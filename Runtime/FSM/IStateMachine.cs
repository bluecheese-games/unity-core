//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.Core.FSM
{
	public interface IStateMachine : IDisposable
    {
        IState CurrentState { get; }
        IState DefaultState { get; }
        bool IsStarted { get; }
		IEnumerable<IState> States { get; }
        float StateTime { get; }
		IBlackboard Blackboard { get; }

		void Start();
        void Update(float deltaTime);

		IState GetState(string name);
		T GetState<T>(string name) where T : IState;
		void SetState(string stateName, float stateTime = 0);
	}
}