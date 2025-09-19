//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.FSM
{
    public class Transition : ITransition
    {
        private readonly ICondition[] _conditions;
        private readonly float _exitTime = 0f;

        public string NextState { get; private set; }

        public Transition(string nextState, float exitTime, params ICondition[] conditions)
        {
            NextState = nextState;
            _exitTime = exitTime;
            _conditions = conditions;
        }

        public bool Evaluate(float stateTime, IBlackboard blackboard, out string nextstate, out float overTime)
        {
            overTime = 0f;
            nextstate = null;
            if (_exitTime > 0f && stateTime < _exitTime)
            {
                return false;
            }

            if (_conditions != null && _conditions.Length > 0)
            {
                foreach (var condition in _conditions)
                {
                    if (!condition.Evaluate(blackboard))
                    {
                        return false;
                    }
                }
            }

            overTime = stateTime - _exitTime;
            nextstate = NextState;
            return true;
        }
    }
}
