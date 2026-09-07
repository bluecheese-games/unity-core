namespace BlueCheese.Core.FSM
{
    public interface ICondition
    {
        bool Evaluate(IBlackboard blackboard);
    }
}
