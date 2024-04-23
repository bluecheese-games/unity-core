namespace BlueCheese.Unity.Core.FSM
{
    public interface ITransition
    {
        IState NextState { get; }

        bool Evaluate(IStateMachine stateMachine, out IState nextstate, out float overTime);
    }
}