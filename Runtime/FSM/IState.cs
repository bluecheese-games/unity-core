//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.FSM
{
    public interface IState
    {
        public string Name { get; }

        public void OnEnter();

        public void OnUpdate(float deltaTime);

        public void OnExit();
    }
}
