//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.FSM
{
    public class Transition : ITransition
    {
        private readonly ICondition[] _conditions;
        private readonly float _exitTime = 0f;

        public IState NextState { get; private set; }

        public Transition(IState nextState, float exitTime, ICondition[] conditions)
        {
            NextState = nextState;
            _exitTime = exitTime;
            _conditions = conditions;
        }

        public bool Evaluate(IStateMachine stateMachine, out IState nextstate, out float overTime)
        {
            overTime = 0f;
            nextstate = null;
            if (_exitTime > 0f && stateMachine.StateTime < _exitTime)
            {
                return false;
            }

            if (_conditions != null && _conditions.Length > 0)
            {
                foreach (var condition in _conditions)
                {
                    if (!condition.Evaluate(stateMachine))
                    {
                        return false;
                    }
                }
            }

            overTime = stateMachine.StateTime - _exitTime;
            nextstate = NextState;
            return true;
        }
    }
}
