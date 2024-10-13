//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlueCheese.Core
{
	public class ProcessQueue
	{
		private readonly Queue<Item> _items = new();
		private bool _isProcessing = false;
		private Item _processingItem;
		private int _processedCount;

		public int Count => _items.Count + _processedCount;

		public float Progress => _isProcessing ? (float)_processedCount / Count : 0f;

		public string ProcessingAction => _processingItem.Name;

		public event Action<float> Progressed;
		public event Action Complete;

		public void Enqueue(Action action, string name = null)
		{
			if (_isProcessing)
			{
				throw new Exception("ProcessQueue is already processing");
			}
			name ??= action.Method.Name;
			_items.Enqueue(new Item(action: action, name: name));
		}

		public void Enqueue(Func<Task> asyncAction, string name = null)
		{
			name ??= asyncAction.Method.Name;
			_items.Enqueue(new Item(asyncAction: asyncAction, name: name));
		}

		public async Task ProcessAsync()
		{
			if (_isProcessing)
			{
				return;
			}
			if (_items.Count == 0)
			{
				throw new Exception("The process queue is empty");
			}

			_isProcessing = true;
			_processedCount = 0;

			while (_items.Count > 0)
			{
				_processingItem = _items.Dequeue();
				await _processingItem.InvokeAsync();
				_processedCount++;
				Progressed?.Invoke(Progress);
			}

			_isProcessing = false;
			Complete?.Invoke();
		}

		private readonly struct Item
		{
			public readonly string Name { get; }
			private readonly Action _action;
			private readonly Func<Task> _asyncAction;

			public Item(string name = null, Action action = null, Func<Task> asyncAction = null)
			{
				Name = name;
				_action = action;
				_asyncAction = asyncAction;
			}

			public async Task InvokeAsync()
			{
				if (_action != null)
				{
					_action.Invoke();
					await Task.Yield();
				}
				else if (_asyncAction != null)
				{
					await _asyncAction.Invoke();
				}
			}
		}
	}
}
