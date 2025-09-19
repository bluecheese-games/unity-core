//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.FSM
{
    public interface IStateHandler
    {
        public void OnEnter();

        public void OnUpdate(float deltaTime);

        public void OnExit();
    }
}
