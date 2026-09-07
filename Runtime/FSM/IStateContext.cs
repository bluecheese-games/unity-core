namespace BlueCheese.Core.FSM
{
    /// <summary>
    /// Context passed to state handlers when the state machine starts.
    /// Gives handlers read-only access to their state name and the shared blackboard.
    /// </summary>
    public interface IStateContext
    {
        /// <summary>The name of the state this handler is attached to.</summary>
        string StateName { get; }

        /// <summary>The shared blackboard for reading/writing parameters.</summary>
        IBlackboard Blackboard { get; }
    }
}
