using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Unity.Core.FSM.Graph
{
    [CreateAssetMenu(fileName = "FSMGraph", menuName = "FSM/Graph")]
    public class FSMGraphAsset : ScriptableObject
    {
        public List<GraphState> States;
        public List<GraphTransition> Transitions;
        public List<GraphParameter> Parameters;

        private void OnValidate()
        {
            if (States != null)
            {
                foreach (GraphState state in States)
                {
                    state.DisplayName = $"{state.Name} {(state.IsDefault ? "[Default]" : "")}";
                }
            }
            if (Transitions != null)
            {
                foreach (GraphTransition transition in Transitions)
                {
                    transition.DisplayName = $"{transition.FromState} => {transition.ToState}";
                }
            }
            if (Parameters != null)
            {
                foreach (GraphParameter parameter in Parameters)
                {
                    parameter.DisplayName = $"[{parameter.Type}] {parameter.Name}";
                }
            }
        }

        [Serializable]
        public class GraphState
        {
            [HideInInspector]
            public string DisplayName;
            public string Name;
            public bool IsDefault;
            [HideInInspector]
            public Vector2 Position;
        }

        [Serializable]
        public class GraphTransition
        {
            [HideInInspector]
            public string DisplayName;
            public string FromState;
            public string ToState;
            public float ExitTime;
            [HideInInspector]
            public Vector2 Position;
            public List<GraphCondition> Conditions;

            public ICondition[] GetConditionArray(Dictionary<string, GraphParameter> parameters)
            {
                if (Conditions == null || Conditions.Count == 0)
                {
                    return default;
                }

                return Conditions
                    .Select(c => c.ToCondition(parameters))
                    .Where(c => c != null)
                    .ToArray();
            }
        }

        [Serializable]
        public class GraphParameter
        {
            [HideInInspector]
            public string DisplayName;
            public string Name;
            public Condition.Type Type;
            public bool DefaultBoolValue;
            public int DefaultIntValue;
            public float DefaultFloatValue;
        }

        [Serializable]
        public class GraphCondition
        {
            public string ParameterName;
            public Condition.Operator Operator;
            public bool TargetBoolValue;
            public int TargetIntValue;
            public float TargetFloatValue;

            public ICondition ToCondition(Dictionary<string, GraphParameter> parameters)
            {
                if (!parameters.ContainsKey(ParameterName))
                {
                    return null;
                }

                var parameter = parameters[ParameterName];
                return parameter.Type switch
                {
                    Condition.Type.Trigger => Condition.CreateTriggerCondition(ParameterName),
                    Condition.Type.Bool => Condition.CreateBoolCondition(ParameterName, TargetBoolValue),
                    Condition.Type.Int => Condition.CreateIntCondition(ParameterName, Operator, TargetIntValue),
                    Condition.Type.Float => Condition.CreateFloatCondition(ParameterName, Operator, TargetFloatValue),
                    _ => null,
                };
            }
        }

        public StateMachine ToStateMachine()
        {
            var builder = new StateMachine.Builder();

            foreach (var state in States)
            {
                builder.AddState(new State(state.Name), state.IsDefault);
            }

            foreach (var transitionData in Transitions)
            {
                var parameterDict = Parameters.ToDictionary(p => p.Name);
                if (string.IsNullOrEmpty(transitionData.FromState))
                {
                    builder.AddTransitionFromAnyState(transitionData.ToState, transitionData.GetConditionArray(parameterDict));
                }
                else
                {
                    builder.AddTransition(transitionData.FromState, transitionData.ToState, transitionData.ExitTime, transitionData.GetConditionArray(parameterDict));
                }
            }

            foreach (var parameter in Parameters)
            {
                switch (parameter.Type)
                {
                    case Condition.Type.Bool:
                        builder.AddBoolParameter(parameter.Name, parameter.DefaultBoolValue);
                        break;
                    case Condition.Type.Int:
                        builder.AddIntParameter(parameter.Name, parameter.DefaultIntValue);
                        break;
                    case Condition.Type.Float:
                        builder.AddFloatParameter(parameter.Name, parameter.DefaultFloatValue);
                        break;
                }
            }

            return builder.Build();
        }
    }
}
