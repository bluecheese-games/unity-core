//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Unity.Core.FSM
{
    public interface IState : IDisposable
    {
        public string Name { get; }

        public void OnEnter();

        public void OnUpdate(float deltaTime);

        public void OnExit();
    }
}
