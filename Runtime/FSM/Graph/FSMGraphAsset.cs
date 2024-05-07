//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;

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
                    state.OnValidate();
                }
            }
            if (Parameters != null)
            {
                foreach (GraphParameter parameter in Parameters)
                {
                    parameter.OnValidate();
                }
            }
            if (Transitions != null)
            {
                foreach (GraphTransition transition in Transitions)
                {
                    transition.OnValidate(this);
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

            public void OnValidate()
            {
                DisplayName = $"{Name} {(IsDefault ? "[Default]" : "")}";
            }
        }

        [Serializable]
        public class GraphTransition
        {
            [HideInInspector]
            public string DisplayName;

            [HideInInspector]
            public Vector2 Position;

            [Dropdown(nameof(_states))]
            public string FromState;

            [Dropdown(nameof(_states))]
            public string ToState;

            [Min(0f)]
            public float ExitTime;

            public List<GraphCondition> Conditions;

            private string[] _states;

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

            public void OnValidate(FSMGraphAsset graph)
            {
                DisplayName = $"{FromState} => {ToState}";

                _states = graph.States.Select(s => s.Name).ToArray();

                var parameters = graph.Parameters.ToDictionary(p => p.Name);
                foreach (var condition in Conditions)
                {
                    condition.OnValidate(parameters);
                }
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

            public void OnValidate()
            {
                DisplayName = $"[{Type}] {Name}";
            }
        }

        [Serializable]
        public class GraphCondition
        {
            [HideInInspector]
            public string DisplayName;

            [Dropdown(nameof(_parametersList))]
            [OnValueChanged(nameof(Refresh))]
            public string ParameterName;

            [ShowIf(nameof(IsNotTrigger))]
            [AllowNesting]
            public Condition.Operator Operator;

            [ShowIf(nameof(_parameterType), Condition.Type.Bool)]
            [AllowNesting]
            public bool TargetBoolValue;

            [ShowIf(nameof(_parameterType), Condition.Type.Int)]
            [AllowNesting]
            public int TargetIntValue;

            [ShowIf(nameof(_parameterType), Condition.Type.Float)]
            [AllowNesting]
            public float TargetFloatValue;

            private Dictionary<string, GraphParameter> _parameters;
            private string[] _parametersList => _parameters.Keys.ToArray();
            private Condition.Type _parameterType => _parameters.ContainsKey(ParameterName) ? _parameters[ParameterName].Type : Condition.Type.Predicate;
            private bool IsNotTrigger => _parameterType != Condition.Type.Trigger;

            private void Refresh() => OnValidate(_parameters);

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

            public void OnValidate(Dictionary<string, GraphParameter> parameters)
            {
                if (!parameters.ContainsKey(ParameterName))
                {
                    DisplayName = ParameterName;
                    return;
                }

                _parameters = parameters;

                var parameter = parameters[ParameterName];
                DisplayName = parameter.Type switch
                {
                    Condition.Type.Trigger => $"-- {parameter.Name} --",
                    Condition.Type.Bool => $"{parameter.Name} {Operator.ToFriendlyString()} {TargetBoolValue}",
                    Condition.Type.Int => $"{parameter.Name} {Operator.ToFriendlyString()} {TargetIntValue}",
                    Condition.Type.Float => $"{parameter.Name} {Operator.ToFriendlyString()} {TargetFloatValue}f",
                    _ => parameter.DisplayName,
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
