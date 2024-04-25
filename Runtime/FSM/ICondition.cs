namespace BlueCheese.Unity.Core.FSM
{
    public interface ICondition
    {
        bool Evaluate(IStateMachine stateMachine);
    }
}