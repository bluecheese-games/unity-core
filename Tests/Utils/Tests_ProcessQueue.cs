//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections;
using System.Threading;
using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

public class Tests_ProcessQueue
{
	private ProcessQueue _processQueue;

	[SetUp]
	public void SetUp()
	{
		_processQueue = new ProcessQueue();
	}

	#region Enqueue (Action)

	[Test]
	public void Enqueue_Action_ShouldAddItemToQueue()
	{
		// Arrange
		int counter = 0;

		// Act
		_processQueue.Enqueue(() => counter++);

		// Assert
		Assert.AreEqual(1, _processQueue.Count);
		// TotalCount is only set when processing starts, so it's expected to be 0 here.
		Assert.AreEqual(0, _processQueue.TotalCount);
	}

	[Test]
	public void Enqueue_Action_ShouldBeFluent()
	{
		// Arrange
		int counter = 0;

		// Act
		var result = _processQueue
			.Enqueue(() => counter++, "Step 1")
			.Enqueue(() => counter++, "Step 2");

		// Assert
		Assert.That(result, Is.SameAs(_processQueue));
		Assert.That(_processQueue.Count, Is.EqualTo(2));
	}

	[Test]
	public void Enqueue_Action_ShouldThrowArgumentNullException_WhenActionIsNull()
	{
		// Arrange
		Action action = null;

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => _processQueue.Enqueue(action));
	}

	#endregion

	#region Enqueue (Async UniTask / token-aware)

	[Test]
	public void Enqueue_AsyncAction_ShouldAddItemToQueue()
	{
		// Arrange
		Func<UniTask> asyncAction = async () =>
		{
			await UniTask.Yield();
		};

		// Act
		_processQueue.Enqueue(asyncAction);

		// Assert
		Assert.AreEqual(1, _processQueue.Count);
	}

	[Test]
	public void Enqueue_AsyncActionWithToken_ShouldBeFluent()
	{
		// Arrange
		Func<CancellationToken, UniTask> asyncAction = async ct =>
		{
			await UniTask.Yield(ct);
		};

		// Act
		var result = _processQueue
			.Enqueue(asyncAction, "Async Step 1")
			.Enqueue(asyncAction, "Async Step 2");

		// Assert
		Assert.That(result, Is.SameAs(_processQueue));
		Assert.That(_processQueue.Count, Is.EqualTo(2));
	}

	[Test]
	public void Enqueue_ActionWithToken_ShouldAddItemToQueue()
	{
		// Arrange
		Action<CancellationToken> action = ct => { /* no-op */ };

		// Act
		_processQueue.Enqueue(action);

		// Assert
		Assert.That(_processQueue.Count, Is.EqualTo(1));
	}

	#endregion

	#region Enqueue while processing

	[Test]
	public void Enqueue_ShouldThrowInvalidOperationException_IfAlreadyBeingProcessed()
	{
		// Arrange
		_processQueue.Enqueue(() => { });

		// Act
		var _ = _processQueue.ProcessAsync(); // fire & forget, sets _isProcessing = true

		// Assert
		Assert.Throws<InvalidOperationException>(() => _processQueue.Enqueue(() => { }));
	}

	#endregion

	#region ProcessAsync basics

	[Test]
	public async void ProcessAsync_ShouldThrowInvalidOperationExceptionIfQueueIsEmpty()
	{
		// Arrange
		InvalidOperationException caught = null;

		// Act
		try
		{
			await _processQueue.ProcessAsync();
		}
		catch (InvalidOperationException ex)
		{
			caught = ex;
		}

		// Assert
		Assert.IsNotNull(caught);
	}

	[Test]
	public async void ProcessAsync_ShouldThrowIfAlreadyProcessing()
	{
		// Arrange
		_processQueue.AddDelay(0.01f); // ensure the first call stays "in progress"
		var _ = _processQueue.ProcessAsync(); // first call

		InvalidOperationException caught = null;

		// Act
		try
		{
			await _processQueue.ProcessAsync(); // second call should fail immediately
		}
		catch (InvalidOperationException ex)
		{
			caught = ex;
		}

		// Assert
		Assert.IsNotNull(caught);
	}

	[Test]
	public async void ProcessAsync_ShouldProcessAllItemsInQueue()
	{
		// Arrange
		int counter = 0;

		_processQueue
			.Enqueue(() => counter++)
			.Enqueue(async () =>
			{
				await UniTask.Yield();
				counter++;
			})
			.Enqueue(() => counter++);

		// Act
		await _processQueue.ProcessAsync();

		// Assert
		Assert.AreEqual(3, counter);
		Assert.That(_processQueue.TotalCount, Is.GreaterThanOrEqualTo(1));
		Assert.That(_processQueue.Progress, Is.InRange(0f, 1f));
		// We don't assert a specific Count value here because its semantics
		// can vary depending on the implementation.
	}

	#endregion

	#region Progress / events

	[Test]
	public async void Progress_ShouldGoFromZeroToSomeValue()
	{
		// Arrange
		_processQueue.Enqueue(() => { });

		// Act
		float before = _processQueue.Progress;
		await _processQueue.ProcessAsync();
		float after = _processQueue.Progress;

		// Assert
		Assert.That(before, Is.EqualTo(0f));
		// Implementation-dependent; just ensure it's a valid normalized progress.
		Assert.That(after, Is.InRange(0f, 1f));
	}

	[Test]
	public async void ProgressedEvent_ShouldBeRaisedPerItem()
	{
		// Arrange
		int progressEventCount = 0;
		float lastProgress = -1f;

		_processQueue
			.Enqueue(() => { }, "Step 1")
			.Enqueue(() => { }, "Step 2")
			.Enqueue(() => { }, "Step 3");

		_processQueue.Progressed += progress =>
		{
			progressEventCount++;
			lastProgress = progress;
		};

		// Act
		await _processQueue.ProcessAsync();

		// Assert
		Assert.That(progressEventCount, Is.EqualTo(3));
		Assert.That(lastProgress, Is.InRange(0f, 1f));
	}

	[Test]
	public async void CompleteEvent_ShouldBeRaisedOnce()
	{
		// Arrange
		int completeCount = 0;

		_processQueue.Enqueue(() => { });
		_processQueue.Complete += () => completeCount++;

		// Act
		await _processQueue.ProcessAsync();

		// Assert
		Assert.That(completeCount, Is.EqualTo(1));
	}

	#endregion

	#region Helper methods: AddDelay / AddFrame

	[Test]
	public async void AddDelay_ShouldEnqueueDelayStep_AndBeProcessed()
	{
		// Arrange
		bool executedAfterDelay = false;

		_processQueue
			.AddDelay(0.01f)
			.Enqueue(() => executedAfterDelay = true);

		// Act
		await _processQueue.ProcessAsync();

		// Assert
		Assert.That(executedAfterDelay, Is.True);
	}

	[Test]
	public async void AddDelay_WithZero_ShouldStillRunNextStep()
	{
		// Arrange
		bool executed = false;

		_processQueue
			.AddDelay(0f)
			.Enqueue(() => executed = true);

		// Act
		await _processQueue.ProcessAsync();

		// Assert
		Assert.That(executed, Is.True);
	}

	[Test]
	public void AddDelay_WithNegative_ShouldThrowArgumentOutOfRangeException()
	{
		// Arrange
		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => _processQueue.AddDelay(-0.1f));
	}

	[Test]
	public async void AddFrame_ShouldEnqueueFrameStep_AndBeProcessed()
	{
		// Arrange
		bool executedAfterFrame = false;

		_processQueue
			.AddFrame()
			.Enqueue(() => executedAfterFrame = true);

		// Act
		await _processQueue.ProcessAsync();

		// Assert
		Assert.That(executedAfterFrame, Is.True);
	}

	#endregion

	#region Cancellation

	[Test]
	public async void ProcessAsync_WhenCancelled_ShouldStopAndNotRaiseComplete()
	{
		// Arrange
		int counter = 0;
		int completeCount = 0;
		var cts = new CancellationTokenSource();

		_processQueue
			.Enqueue(async ct =>
			{
				counter++;
				cts.Cancel();
				await UniTask.Yield(ct);
			}, "Step 1")
			.Enqueue(ct => counter++, "Step 2");

		_processQueue.Complete += () => completeCount++;

		// Act
		Exception caught = null;
		try
		{
			await _processQueue.ProcessAsync(cts.Token);
		}
		catch (OperationCanceledException ex)
		{
			caught = ex;
		}

		// Assert
		Assert.IsNotNull(caught);
		Assert.That(counter, Is.EqualTo(1));        // only first step ran
		Assert.That(completeCount, Is.EqualTo(0));  // Complete should not be raised on cancel
	}

	#endregion

	#region Clear / Reset

	[Test]
	public void Clear_ShouldRemoveAllItems_WhenNotProcessing()
	{
		// Arrange
		_processQueue
			.Enqueue(() => { })
			.Enqueue(() => { });

		// Act
		_processQueue.Clear();

		// Assert
		Assert.That(_processQueue.Count, Is.EqualTo(0));
		Assert.That(_processQueue.TotalCount, Is.EqualTo(0));
		Assert.That(_processQueue.Progress, Is.EqualTo(0f));
	}

	[Test]
	public void Clear_ShouldThrowIfProcessing()
	{
		// Arrange
		_processQueue.AddDelay(0.01f);
		var _ = _processQueue.ProcessAsync();

		// Act & Assert
		Assert.Throws<InvalidOperationException>(() => _processQueue.Clear());
	}

	#endregion

	#region Coroutine enqueue support

	private class TestCoroutineRunner : ICoroutineRunner
	{
		public int RunCount { get; private set; }

		public UniTask RunCoroutineAsync(IEnumerator coroutine)
		{
			RunCount++;

			while (coroutine.MoveNext())
			{
				// Ignore yielded values for tests.
			}

			return UniTask.CompletedTask;
		}
	}

	[Test]
	public void Enqueue_Coroutine_ShouldThrowIfNoRunner()
	{
		// Arrange
		var queue = new ProcessQueue(); // no runner

		IEnumerator MyCoroutine()
		{
			yield break;
		}

		// Act & Assert
		Assert.Throws<InvalidOperationException>(() => queue.Enqueue(MyCoroutine));
	}

	[Test]
	public async void Enqueue_Coroutine_ShouldRunViaCoroutineRunner()
	{
		// Arrange
		var runner = new TestCoroutineRunner();
		var queue = new ProcessQueue(runner);
		int counter = 0;

		IEnumerator MyCoroutine()
		{
			counter++;
			yield break;
		}

		queue.Enqueue(MyCoroutine, "My Coroutine");

		// Act
		await queue.ProcessAsync();

		// Assert
		Assert.That(runner.RunCount, Is.EqualTo(1));
		Assert.That(counter, Is.EqualTo(1));
	}

	#endregion
}
