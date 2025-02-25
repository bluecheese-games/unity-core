//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Linq;

namespace BlueCheese.Core.FSM
{
	public class CompositeStateHandler : IStateHandler
	{
		protected readonly List<IStateHandler> _handlers = new();

		public IReadOnlyList<IStateHandler> Handlers => _handlers;

		public CompositeStateHandler(params IStateHandler[] handlers)
		{
			_handlers.AddRange(handlers.Where(h => h != null));
		}

		public void AddHandler(IStateHandler handler)
		{
			if (handler != null)
			{
				_handlers.Add(handler);
			}
		}

		public void RemoveHandler(IStateHandler handler)
		{
			_handlers.Remove(handler);
		}

		public void OnEnter()
		{
			foreach (var handler in _handlers)
			{
				handler.OnEnter();
			}
		}

		public void OnExit()
		{
			foreach (var handler in _handlers)
			{
				handler.OnExit();
			}
		}

		public void OnUpdate(float deltaTime)
		{
			foreach (var handler in _handlers)
			{
				handler.OnUpdate(deltaTime);
			}
		}
	}
}
