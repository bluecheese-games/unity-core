//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.FSM
{
    public interface IStateHandler
    {
        /// <summary>
        /// Called once when the state machine starts, before any state is entered.
        /// Override to receive the state name and blackboard.
        /// Default implementation does nothing — existing handlers need no changes.
        /// </summary>
        void Initialize(IStateContext context) { }

        void OnEnter();

        void OnUpdate(float deltaTime);

        void OnExit();
    }
}
