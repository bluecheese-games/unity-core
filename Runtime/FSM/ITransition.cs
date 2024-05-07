//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Unity.Core.FSM
{
    public interface ITransition
    {
        IState NextState { get; }

        bool Evaluate(IStateMachine stateMachine, out IState nextstate, out float overTime);
    }
}