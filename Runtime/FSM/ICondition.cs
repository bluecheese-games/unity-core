//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.FSM
{
    public interface ICondition
    {
        bool Evaluate(IBlackboard blackboard);
    }
}