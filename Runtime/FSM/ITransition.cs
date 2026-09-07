namespace BlueCheese.Core.FSM
{
    public interface ITransition
    {
		string NextState { get; }

        bool Evaluate(float stateTime, IBlackboard blackboard, out string nextstate, out float overTime);
    }
}
