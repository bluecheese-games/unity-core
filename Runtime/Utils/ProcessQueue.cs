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
		private Queue<Item> _items = new();
		private bool _isProcessing = false;

		public int Count => _items.Count;

		public void Enqueue(Action action, string name = null)
		{
			name ??= action.Method.Name;
			_items.Enqueue(Item.Create(name, action));
		}

		public void Enqueue(Func<Task> asyncAction, string name = null)
		{
			name ??= asyncAction.Method.Name;
			_items.Enqueue(Item.CreateASync(name, asyncAction));
		}

		public async Task ProcessAsync()
		{
			if (_isProcessing)
			{
				return;
			}

			_isProcessing = true;

			while (_items.Count > 0)
			{
				Item item = _items.Dequeue();
				await item.InvokeAsync();
			}

			_isProcessing = false;
		}

		private readonly struct Item
		{
			private readonly string _name;
			private readonly Action _action;
			private readonly Func<Task> _asyncAction;

			public string Name => _name;

			public static Item Create(string name, Action action) => new(name, action);

			public static Item CreateASync(string name, Func<Task> asyncAction) => new(name, asyncAction);

			private Item(string name, Action action)
			{
				_name = name;
				_action = action;
				_asyncAction = null;
			}

			private Item(string name, Func<Task> asyncAction)
			{
				_name = name;
				_asyncAction = asyncAction;
				_action = null;
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
