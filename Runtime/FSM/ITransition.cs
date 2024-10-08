//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.FSM
{
    public interface ITransition
    {
        IState NextState { get; }

        bool Evaluate(float stateTime, IBlackboard blackboard, out IState nextstate, out float overTime);
    }
}