//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.Core.FSM
{
	public interface IStateMachine : IDisposable
    {
        string CurrentState { get; }
		string DefaultState { get; }
        bool IsStarted { get; }
		ICollection<string> States { get; }
        float StateTime { get; }
		IBlackboard Blackboard { get; }

		void Start();
        void Update(float deltaTime);

		IStateHandler GetStateHandler(string name);
		T GetStateHandler<T>(string name) where T : IStateHandler;
		void SetState(string stateName, float stateTime = 0);
	}
}