//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Unity.Core.FSM
{
    public class State : IState
    {
        public string Name { get; private set; }

        public event Action<IState> OnEnterState;
        public event Action<IState, float> OnUpdateState;
        public event Action<IState> OnExitState;

        public State(string name)
        {
            Name = name;
        }

        virtual public void OnEnter()
        {
            OnEnterState?.Invoke(this);
        }

        virtual public void OnExit()
        {
            OnExitState?.Invoke(this);
        }

        virtual public void OnUpdate(float deltaTime)
        {
            OnUpdateState?.Invoke(this, deltaTime);
        }

        virtual public void Dispose()
        {
            OnEnterState = null;
            OnUpdateState = null;
            OnExitState = null;
        }
    }
}
