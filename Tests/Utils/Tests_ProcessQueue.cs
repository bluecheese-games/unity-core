//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
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

	[Test]
	public void Enqueue_Action_ShouldAddItemToQueue()
	{
		int counter = 0;
		_processQueue.Enqueue(() => counter++);

		Assert.AreEqual(1, _processQueue.Count);
	}

	[Test]
	public void Enqueue_ShouldThrowException_IfAlreadyBeingProcessed()
	{
		_processQueue.Enqueue(() => { });
		_ = _processQueue.ProcessAsync();
		Assert.Throws<Exception>(() => _processQueue.Enqueue(() => { }));
	}

	[Test]
	public void Enqueue_AsyncAction_ShouldAddItemToQueue()
	{
		_processQueue.Enqueue(async () =>
		{
			await UniTask.Yield();
		});

		Assert.AreEqual(1, _processQueue.Count);
	}

	[Test]
	public async void ProcessAsync_ShouldThrowExceptionIfQueueIsEmpty()
	{
		bool caught = false;
		try
		{
			await _processQueue.ProcessAsync();
		}
		catch (Exception)
		{
			caught = true;
		}
		Assert.IsTrue(caught);
	}

	[Test]
	public async void ProcessAsync_ShouldProcessAllItemsInQueue()
	{
		int counter = 0;
		_processQueue.Enqueue(() => counter++);
		_processQueue.Enqueue(async () =>
		{
			await UniTask.Yield();
			counter++;
		});
		_processQueue.Enqueue(() => counter++);

		await _processQueue.ProcessAsync();

		Assert.AreEqual(0, _processQueue.Count);
		Assert.AreEqual(3, counter);
	}

	[Test]
	public async void Progress_ShouldUpdate()
	{
		_processQueue.Enqueue(() => { });

		Assert.That(_processQueue.Progress, Is.EqualTo(0));
		await _processQueue.ProcessAsync();
		Assert.That(_processQueue.Progress, Is.EqualTo(1));
	}
}
