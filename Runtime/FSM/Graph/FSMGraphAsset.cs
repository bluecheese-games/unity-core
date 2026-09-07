using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core.FSM.Graph
{
    [CreateAssetMenu(fileName = "FSMGraph", menuName = "BlueCheese/FSM Graph")]
    public class FSMGraphAsset : ScriptableObject
    {
        public List<GraphState> States = new();
        public List<GraphTransition> Transitions = new();
        public List<GraphParameter> Blackboard = new();
        public GraphViewState ViewState = new();

        [Serializable]
        public class GraphViewState
        {
            public Vector3 ViewPosition = Vector3.zero;
            public Vector3 ViewScale = Vector3.one;
            public Vector2 AnyStatePosition = new Vector2(50f, 200f);
        }

        [Serializable]
        public class GraphState
        {
            public string Name;
            public bool IsDefault;
            public Vector2 Position;
            public List<string> HandlerTypeNames = new();
        }

        [Serializable]
        public class GraphTransition
        {
            public string FromState; // null or empty = Any State
            public string ToState;
            public bool UseExitTime;
            [Min(0f)]
            public float ExitTime;
            public List<GraphCondition> Conditions = new();
        }

        [Serializable]
        public class GraphParameter
        {
            public string Name;
            public Condition.Type Type;
            public bool DefaultBoolValue;
            public int DefaultIntValue;
            public float DefaultFloatValue;
            /// <summary>Assembly-qualified type name of an IPredicate implementation.
            /// Only used when Type == Condition.Type.Predicate.</summary>
            public string PredicateTypeName;
            /// <summary>JSON-serialized public fields of the IPredicate instance (via JsonUtility).</summary>
            public string PredicateJson;
        }

        [Serializable]
        public class GraphCondition
        {
            public string ParameterName;
            public Condition.Operator Operator;
            public bool TargetBoolValue;
            public int TargetIntValue;
            public float TargetFloatValue;
            // Note: PredicateTypeName lives on GraphParameter, not here.

            /// <summary>
            /// Convert to a runtime ICondition.
            /// For Predicate conditions, <paramref name="predicateInstances"/> provides the
            /// pre-instantiated IPredicate objects, and <paramref name="ctxBox"/> is a shared
            /// mutable holder that will receive a DynamicContext after Build().
            /// </summary>
            public ICondition ToCondition(
                Dictionary<string, GraphParameter> parameters,
                Dictionary<string, IPredicate> predicateInstances = null,
                ContextBox ctxBox = null)
            {
                if (string.IsNullOrEmpty(ParameterName) || !parameters.TryGetValue(ParameterName, out var param))
                    return null;

                if (param.Type == Condition.Type.Predicate)
                {
                    if (predicateInstances == null || !predicateInstances.TryGetValue(ParameterName, out var pred))
                        return null;
                    var box = ctxBox; // capture for closure
                    return Condition.CreatePredicateCondition(() => pred.Evaluate(box?.Value));
                }

                return param.Type switch
                {
                    Condition.Type.Trigger => Condition.CreateTriggerCondition(ParameterName),
                    Condition.Type.Bool    => Condition.CreateBoolCondition(ParameterName, TargetBoolValue),
                    Condition.Type.Int     => Condition.CreateIntCondition(ParameterName, Operator, TargetIntValue),
                    Condition.Type.Float   => Condition.CreateFloatCondition(ParameterName, Operator, TargetFloatValue),
                    _                      => null,
                };
            }
        }

        /// <summary>Mutable holder so lambdas created before Build() can be updated after.</summary>
        public sealed class ContextBox { public IStateContext Value; }

        public StateMachine ToStateMachine()
        {
            var builder = new StateMachine.Builder();

            var parameterDict = Blackboard.ToDictionary(p => p.Name);

            // Instantiate one IPredicate per Predicate blackboard parameter.
            var predicateInstances = new Dictionary<string, IPredicate>();
            foreach (var param in Blackboard)
            {
                if (param.Type != Condition.Type.Predicate) continue;
                if (string.IsNullOrEmpty(param.PredicateTypeName)) continue;
                var t = Type.GetType(param.PredicateTypeName);
                if (t == null || !typeof(IPredicate).IsAssignableFrom(t)) continue;
                var pred = (IPredicate)Activator.CreateInstance(t);
                // Restore serialized field values (e.g. KeyCode, float threshold…)
                if (!string.IsNullOrEmpty(param.PredicateJson))
                    UnityEngine.JsonUtility.FromJsonOverwrite(param.PredicateJson, pred);
                predicateInstances[param.Name] = pred;
            }

            // Single shared ContextBox — DynamicContext is set after Build()
            var ctxBox = new ContextBox();

            foreach (var state in States)
            {
                IStateHandler handler = null;
                if (state.HandlerTypeNames != null && state.HandlerTypeNames.Count > 0)
                {
                    var handlers = state.HandlerTypeNames
                        .Select(typeName =>
                        {
                            var type = Type.GetType(typeName);
                            if (type == null || !typeof(IStateHandler).IsAssignableFrom(type)) return null;
                            return Activator.CreateInstance(type) as IStateHandler;
                        })
                        .Where(h => h != null)
                        .ToArray();

                    handler = handlers.Length switch
                    {
                        0 => null,
                        1 => handlers[0],
                        _ => new CompositeStateHandler(handlers)
                    };
                }

                builder.AddState(state.Name, handler, state.IsDefault);
            }

            foreach (var trans in Transitions)
            {
                // Skip transitions whose state references are missing or empty
                if (string.IsNullOrEmpty(trans.ToState)) continue;
                if (!string.IsNullOrEmpty(trans.FromState) &&
                    States.All(s => s.Name != trans.FromState)) continue;
                if (States.All(s => s.Name != trans.ToState)) continue;

                var conditions = (trans.Conditions ?? new List<GraphCondition>())
                    .Select(c => c.ToCondition(parameterDict, predicateInstances, ctxBox))
                    .Where(c => c != null)
                    .ToArray();

                float exitTime = trans.UseExitTime ? trans.ExitTime : 0f;
                if (string.IsNullOrEmpty(trans.FromState))
                    builder.AddTransitionFromAnyState(trans.ToState, conditions);
                else
                    builder.AddTransition(trans.FromState, trans.ToState, exitTime, conditions);
            }

            foreach (var param in Blackboard)
            {
                switch (param.Type)
                {
                    case Condition.Type.Bool:  builder.AddBoolParameter(param.Name, param.DefaultBoolValue); break;
                    case Condition.Type.Int:   builder.AddIntParameter(param.Name, param.DefaultIntValue);   break;
                    case Condition.Type.Float: builder.AddFloatParameter(param.Name, param.DefaultFloatValue); break;
                }
            }

            var machine = builder.Build();

            // Wire the dynamic context so all predicate lambdas see the live state.
            ctxBox.Value = new DynamicContext(machine);

            return machine;
        }

        /// <summary>
        /// Live context: StateName reflects the machine's CurrentState at evaluation time.
        /// </summary>
        private sealed class DynamicContext : IStateContext
        {
            private readonly StateMachine _machine;
            public DynamicContext(StateMachine machine) => _machine = machine;
            public string StateName    => _machine.CurrentState ?? string.Empty;
            public IBlackboard Blackboard => _machine.Blackboard;
        }
    }
}
