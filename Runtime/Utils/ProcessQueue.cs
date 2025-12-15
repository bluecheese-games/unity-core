//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BlueCheese.Core.Utils
{
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

		#region Enqueue overloads

		/// <summary>
		/// Enqueue a synchronous action (fluent).
		/// </summary>
		public ProcessQueue Enqueue(Action action, string name = null)
		{
			if (_isProcessing)
				throw new InvalidOperationException("ProcessQueue is already processing.");

			if (action == null)
				throw new ArgumentNullException(nameof(action));

			name ??= action.Method.Name;
			_items.Enqueue(new Item(name: name, action: action));
			return this;
		}

		/// <summary>
		/// Enqueue an async action that takes a CancellationToken.
		/// </summary>
		public ProcessQueue Enqueue(Func<CancellationToken, UniTask> asyncAction, string name = null)
		{
			if (_isProcessing)
				throw new InvalidOperationException("ProcessQueue is already processing.");

			if (asyncAction == null)
				throw new ArgumentNullException(nameof(asyncAction));

			name ??= asyncAction.Method.Name;
			_items.Enqueue(new Item(name: name, asyncAction: asyncAction));
			return this;
		}

		/// <summary>
		/// Enqueue an async action with no token (fluent).
		/// The queue will still honor cancellation between steps.
		/// </summary>
		public ProcessQueue Enqueue(Func<UniTask> asyncAction, string name = null)
		{
			if (asyncAction == null)
				throw new ArgumentNullException(nameof(asyncAction));

			// Adapt to token-aware version.
			Func<CancellationToken, UniTask> wrapped = _ => asyncAction();
			return Enqueue(wrapped, name ?? asyncAction.Method.Name);
		}

		/// <summary>
		/// Enqueue a token-aware synchronous action (fluent).
		/// </summary>
		public ProcessQueue Enqueue(Action<CancellationToken> action, string name = null)
		{
			if (_isProcessing)
				throw new InvalidOperationException("ProcessQueue is already processing.");

			if (action == null)
				throw new ArgumentNullException(nameof(action));

			Func<CancellationToken, UniTask> wrapped = ct =>
			{
				action(ct);
				return UniTask.Yield(ct);
			};

			name ??= action.Method.Name;
			_items.Enqueue(new Item(name: name, asyncAction: wrapped));
			return this;
		}

		/// <summary>
		/// Enqueue a coroutine factory (Func&lt;IEnumerator&gt;).
		/// Requires a non-null ICoroutineRunner passed in the constructor.
		/// </summary>
		public ProcessQueue Enqueue(Func<IEnumerator> coroutineFactory, string name = null)
		{
			if (_isProcessing)
				throw new InvalidOperationException("ProcessQueue is already processing.");

			if (coroutineFactory == null)
				throw new ArgumentNullException(nameof(coroutineFactory));

			if (_coroutineRunner == null)
			{
				throw new InvalidOperationException(
					"No ICoroutineRunner provided. Pass one to the ProcessQueue constructor to use coroutine enqueueing."
				);
			}

			Func<CancellationToken, UniTask> asyncWrapper = async _ =>
			{
				var coroutine = coroutineFactory();
				await _coroutineRunner.RunCoroutineAsync(coroutine);
			};

			name ??= coroutineFactory.Method.Name;
			_items.Enqueue(new Item(name: name, asyncAction: asyncWrapper));
			return this;
		}

		#endregion

		#region Helpers

		public ProcessQueue AddDelay(float seconds, string name = null)
		{
			if (_isProcessing)
				throw new InvalidOperationException("ProcessQueue is already processing.");

			if (seconds < 0f)
				throw new ArgumentOutOfRangeException(nameof(seconds), "Delay must be non-negative.");

			string resolved = name ?? $"Delay {seconds:0.###}s";

			return Enqueue(async (CancellationToken ct) =>
			{
				int ms = (int)(seconds * 1000f);
				await UniTask.Delay(ms, cancellationToken: ct);
			}, resolved);
		}

		public ProcessQueue AddFrame(string name = "WaitForNextFrame")
		{
			if (_isProcessing)
				throw new InvalidOperationException("ProcessQueue is already processing.");

			return Enqueue(async (CancellationToken ct) =>
			{
				await UniTask.Yield(ct);
			}, name);
		}

		/// <summary>
		/// Returns a list of the names of items currently in the queue.
		/// Useful for UI visualization before or during processing.
		/// </summary>
		public IEnumerable<string> GetPendingStepNames()
		{
			foreach (var item in _items)
			{
				yield return item.Name;
			}
		}

		/// <summary>
		/// Clear all queued items and reset internal counters.
		/// </summary>
		public void Clear()
		{
			if (_isProcessing)
				throw new InvalidOperationException("Cannot clear the queue while it is processing.");

			_items.Clear();
			_processedCount = 0;
			_totalCountForRun = 0;
			_processingItem = default;
		}

		#endregion

		#region Processing

		public async UniTask ProcessAsync(CancellationToken ct = default)
		{
			if (_isProcessing)
				throw new InvalidOperationException("ProcessQueue is already processing.");

			if (_items.Count == 0)
				throw new InvalidOperationException("The process queue is empty.");

			_isProcessing = true;
			_processedCount = 0;
			_totalCountForRun = _items.Count;

			try
			{
				while (_items.Count > 0)
				{
					ct.ThrowIfCancellationRequested();

					_processingItem = _items.Dequeue();
					await _processingItem.InvokeAsync(ct);

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
			// Assumes you added a UniTask.ToCoroutine() extension somewhere else, 
			// or are calling this from a context that understands UniTask.ToCoroutine().
			// Standard UniTask usage: return ProcessAsync(ct).ToCoroutine();
			return ProcessAsync(ct).ToCoroutine();
		}

		#endregion

		#region Item struct

		private readonly struct Item
		{
			public readonly string Name;
			private readonly Action _action;
			private readonly Func<CancellationToken, UniTask> _asyncAction;

			public Item(string name, Action action = null, Func<CancellationToken, UniTask> asyncAction = null)
			{
				Name = name;
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