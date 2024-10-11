//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;
using BlueCheese.Core;
using NUnit.Framework;

public class Tests_ProcessQueue
{
	private ProcessQueue _loadingQueue;

	[SetUp]
	public void SetUp()
	{
		_loadingQueue = new ProcessQueue();
	}

	[Test]
	public void Enqueue_Action_ShouldAddItemToQueue()
	{
		int counter = 0;
		_loadingQueue.Enqueue(() => counter++);

		Assert.AreEqual(1, _loadingQueue.Count);
	}

	[Test]
	public void Enqueue_AsyncAction_ShouldAddItemToQueue()
	{
		_loadingQueue.Enqueue(async () =>
		{
			await Task.Yield();
		});

		Assert.AreEqual(1, _loadingQueue.Count);
	}

	[Test]
	public async void ProcessAsync_ShouldProcessAllItemsInQueue()
	{
		int counter = 0;
		_loadingQueue.Enqueue(() => counter++);
		_loadingQueue.Enqueue(async () =>
		{
			await Task.Yield();
			counter++;
		});
		_loadingQueue.Enqueue(() => counter++);

		await _loadingQueue.ProcessAsync();

		Assert.AreEqual(0, _loadingQueue.Count);
		Assert.AreEqual(3, counter);
	}
}
