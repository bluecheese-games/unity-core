//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Unity.Core.FSM
{
    public interface ICondition
    {
        bool Evaluate(IStateMachine stateMachine);
    }
}