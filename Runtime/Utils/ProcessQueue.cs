//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// Defines behavior when a task throws an unhandled exception.
	/// </summary>
	public enum ExceptionBehavior
	{
		/// <summary>
		/// Log the exception and continue processing the next item.
		/// </summary>
		Continue,

		/// <summary>
		/// Stop processing and propagate the exception (cancelling the queue).
		/// </summary>
		Cancel
	}

	/// <summary>
	/// Represents a snapshot of a step in the queue for visualization.
	/// </summary>
	public readonly struct QueueStep
	{
		public readonly string Name;
		public readonly bool IsParallel;
		public readonly string[] SubStepNames;

		public QueueStep(string name, bool isParallel, string[] subStepNames = null)
		{
			Name = name;
			IsParallel = isParallel;
			SubStepNames = subStepNames;
		}
	}

	/// <summary>
	/// A simple ordered processing queue for Actions, async steps, and coroutines.
	/// 
	/// NOTE: Not thread-safe. Intended for use from Unity's main thread only.
	/// </summary>
	public interface ICoroutineRunner
	{
		UniTask RunCoroutineAsync(IEnumerator coroutine);
	}

	public class ProcessQueue
	{
		private readonly Queue<Item> _items = new();
		private readonly ICoroutineRunner _coroutineRunner;

		private bool _isProcessing;
		private Item _processingItem;
		private int _processedCount;
		private int _totalCountForRun;

		public ProcessQueue(ICoroutineRunner coroutineRunner = null)
		{
			_coroutineRunner = coroutineRunner;
		}

		/// <summary>
		/// Determines what happens when a task throws an exception.
		/// Default is Continue.
		/// </summary>
		public ExceptionBehavior Behavior { get; set; } = ExceptionBehavior.Continue;

		/// <summary>
		/// Remaining items in the queue (not yet processed).
		/// </summary>
		public int Count => _items.Count;

		/// <summary>
		/// Total number of items in the current/last run (processed + remaining at start).
		/// 0 if no run has started yet.
		/// </summary>
		public int TotalCount => _totalCountForRun;

		/// <summary>
		/// Progress of the current/last run in [0, 1]. 0 if no run has started yet.
		/// </summary>
		public float Progress =>
			_totalCountForRun > 0
				? (float)_processedCount / _totalCountForRun
				: 0f;

		/// <summary>
		/// Name of the currently processing item, or null if idle.
		/// </summary>
		public string ProcessingAction => _isProcessing ? _processingItem.Name : null;

		public bool IsProcessing => _isProcessing;

		public event Action<float> Progressed;
		public event Action Complete;

		/// <summary>
		/// Fired when an individual step fails.
		/// Arguments: Step Index (0-based relative to run), Exception.
		/// </summary>
		public event Action<int, Exception> StepFailed;

		/// <summary>
		/// Fired when a sub-task within a parallel step finishes.
		/// Arguments: Main Step Index, Sub Task Index.
		/// </summary>
		public event Action<int, int> ParallelSubProgress;

		#region Enqueue overloads

		public ProcessQueue EnqueueAction(Action action, string name = null)
		{
			CheckProcessing();
			if (action == null) throw new ArgumentNullException(nameof(action));

			name ??= action.Method.Name;
			_items.Enqueue(new Item(name, action: action));
			return this;
		}

		public ProcessQueue EnqueueAsync(Func<CancellationToken, UniTask> asyncAction, string name = null)
		{
			CheckProcessing();
			if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));

			name ??= asyncAction.Method.Name;
			_items.Enqueue(new Item(name, asyncAction: asyncAction));
			return this;
		}

		public ProcessQueue EnqueueAsync(Func<UniTask> asyncAction, string name = null)
		{
			if (asyncAction == null) throw new ArgumentNullException(nameof(asyncAction));
			return EnqueueAsync(_ => asyncAction(), name ?? asyncAction.Method.Name);
		}

		public ProcessQueue EnqueueAction(Action<CancellationToken> action, string name = null)
		{
			CheckProcessing();
			if (action == null) throw new ArgumentNullException(nameof(action));

			Func<CancellationToken, UniTask> wrapped = ct =>
			{
				action(ct);
				return UniTask.Yield(ct);
			};
			return EnqueueAsync(wrapped, name ?? action.Method.Name);
		}

		public ProcessQueue EnqueueCoroutine(Func<IEnumerator> coroutineFactory, string name = null)
		{
			CheckProcessing();
			if (coroutineFactory == null) throw new ArgumentNullException(nameof(coroutineFactory));
			if (_coroutineRunner == null) throw new InvalidOperationException("No ICoroutineRunner provided.");

			Func<CancellationToken, UniTask> asyncWrapper = async _ =>
			{
				await _coroutineRunner.RunCoroutineAsync(coroutineFactory());
			};

			_items.Enqueue(new Item(name ?? coroutineFactory.Method.Name, asyncAction: asyncWrapper));
			return this;
		}

		/// <summary>
		/// Enqueue multiple named async tasks to run concurrently.
		/// This step completes only when ALL tasks are complete.
		/// </summary>
		public ProcessQueue EnqueueParallel(string name, params (string taskName, Func<CancellationToken, UniTask> task)[] tasks)
		{
			CheckProcessing();
			if (tasks == null || tasks.Length == 0)
				throw new ArgumentException("Parallel tasks list cannot be empty", nameof(tasks));

			string[] subNames = tasks.Select(t => t.taskName).ToArray();

			Func<CancellationToken, UniTask> parallelWrapper = async (ct) =>
			{
				int mainIndex = _processedCount; // Capture current index

				// Create wrappers that fire completion events
				var runningTasks = new List<UniTask>(tasks.Length);
				for (int i = 0; i < tasks.Length; i++)
				{
					int subIndex = i;
					var t = tasks[i].task;

					runningTasks.Add(UniTask.Create(async () =>
					{
						await t(ct);
						ParallelSubProgress?.Invoke(mainIndex, subIndex);
					}));
				}

				await UniTask.WhenAll(runningTasks);
			};

			_items.Enqueue(new Item(name, asyncAction: parallelWrapper, isParallel: true, subStepNames: subNames));
			return this;
		}

		private void CheckProcessing()
		{
			if (_isProcessing)
				throw new InvalidOperationException("ProcessQueue is already processing.");
		}

		#endregion

		#region Helpers

		public ProcessQueue AddDelay(float seconds)
		{
			CheckProcessing();
			if (seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));

			return EnqueueAsync(async (ct) =>
			{
				await UniTask.Delay((int)(seconds * 1000f), cancellationToken: ct);
			}, $"Delay {seconds:0.###}s");
		}

		public ProcessQueue AddFrame()
		{
			CheckProcessing();
			return EnqueueAsync(async (ct) => await UniTask.Yield(ct), "WaitForNextFrame");
		}

		public IEnumerable<QueueStep> GetPendingSteps()
		{
			foreach (var item in _items)
			{
				yield return new QueueStep(item.Name, item.IsParallel, item.SubStepNames);
			}
		}

		public void Clear()
		{
			CheckProcessing();
			_items.Clear();
			_processedCount = 0;
			_totalCountForRun = 0;
			_processingItem = default;
		}

		#endregion

		#region Processing

		public async UniTask ProcessAsync(CancellationToken ct = default)
		{
			CheckProcessing();
			if (_items.Count == 0) throw new InvalidOperationException("The process queue is empty.");

			_isProcessing = true;
			_processedCount = 0;
			_totalCountForRun = _items.Count;

			try
			{
				while (_items.Count > 0)
				{
					ct.ThrowIfCancellationRequested();

					_processingItem = _items.Dequeue();

					try
					{
						await _processingItem.InvokeAsync(ct);
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception ex)
					{
						StepFailed?.Invoke(_processedCount, ex);

						if (Behavior == ExceptionBehavior.Cancel) throw;
					}

					_processedCount++;
					Progressed?.Invoke(Progress);
				}

				if (!ct.IsCancellationRequested)
				{
					Complete?.Invoke();
				}
			}
			finally
			{
				_isProcessing = false;
			}
		}

		public IEnumerator ProcessCoroutine(CancellationToken ct = default)
		{
			return ProcessAsync(ct).ToCoroutine();
		}

		#endregion

		#region Item struct

		private readonly struct Item
		{
			public readonly string Name;
			public readonly bool IsParallel;
			public readonly string[] SubStepNames;
			private readonly Action _action;
			private readonly Func<CancellationToken, UniTask> _asyncAction;

			public Item(string name, Action action = null, Func<CancellationToken, UniTask> asyncAction = null, bool isParallel = false, string[] subStepNames = null)
			{
				Name = name;
				IsParallel = isParallel;
				SubStepNames = subStepNames;
				_action = action;
				_asyncAction = asyncAction;
			}

			public UniTask InvokeAsync(CancellationToken ct)
			{
				if (_action != null)
				{
					_action.Invoke();
					return UniTask.Yield(ct);
				}
				if (_asyncAction != null)
				{
					return _asyncAction(ct);
				}
				throw new InvalidOperationException("Item has no action to invoke.");
			}
		}

		#endregion
	}
}