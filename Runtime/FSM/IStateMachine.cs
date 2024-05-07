//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.Unity.Core.FSM
{
    public interface IStateMachine : IDisposable
    {
        IState CurrentState { get; }
        IState DefaultState { get; }
        bool IsStarted { get; }
        IReadOnlyList<IState> States { get; }
        float StateTime { get; }

        bool GetBoolValue(string name);
        float GetFloatValue(string name);
        int GetIntValue(string name);
        IState GetState(string name);
        T GetState<T>(string name) where T : IState;
        bool GetTriggerState(string name);
        void SetBool(string name, bool value);
        void SetFloat(string name, float value);
        void SetInt(string name, int value);
        void SetState(string stateName, float stateTime = 0);
        void SetTrigger(string name);

        void Start();
        void Update(float deltaTime);
    }
}