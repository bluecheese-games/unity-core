//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.FSM
{
    /// <summary>
    /// A serializable predicate that can be referenced in a FSMGraphAsset
    /// and evaluated at runtime as a transition condition.
    /// The context is injected at evaluation time so the predicate always
    /// sees the current state and blackboard values.
    /// </summary>
    public interface IPredicate
    {
        bool Evaluate(IStateContext context);
    }
}
