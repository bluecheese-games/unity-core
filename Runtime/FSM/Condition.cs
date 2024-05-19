//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using static BlueCheese.Core.FSM.Condition;

namespace BlueCheese.Core.FSM
{
    public class Condition : ICondition
    {
        [Serializable]
        public enum Operator
        {
            Equals,
            NotEquals,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual,
        }

        [Serializable]
        public enum Type
        {
            Trigger,
            Bool,
            Int,
            Float,
            Predicate
        }

        private string _parameterName;
        private bool _boolValue;
        private int _intValue;
        private float _floatValue;
        private Func<bool> _predicate;
        private Type _type;
        private Operator _operator;

        private Condition() { }

        static public ICondition CreateTriggerCondition(string triggerName)
            => new Condition() { _parameterName = triggerName, _type = Type.Trigger };

        static public ICondition CreateBoolCondition(string parameterName, bool targetValue)
            => new Condition() { _parameterName = parameterName, _type = Type.Bool, _boolValue = targetValue };

        static public ICondition CreateIntCondition(string parameterName, Operator op, int targetValue)
            => new Condition() { _parameterName = parameterName, _type = Type.Int, _intValue = targetValue, _operator = op };

        static public ICondition CreateFloatCondition(string parameterName, Operator op, float targetValue)
            => new Condition() { _parameterName = parameterName, _type = Type.Float, _floatValue = targetValue, _operator = op };

        static public ICondition CreatePredicateCondition(Func<bool> predicate)
            => new Condition() { _type = Type.Predicate, _predicate = predicate };

        public bool Evaluate(IStateMachine stateMachine) => _type switch
        {
            Type.Trigger => stateMachine.GetTriggerState(_parameterName),
            Type.Bool => stateMachine.GetBoolValue(_parameterName) == _boolValue,
            Type.Int => EvaluateInt(stateMachine.GetIntValue(_parameterName), _intValue, _operator),
            Type.Float => EvaluateFloat(stateMachine.GetFloatValue(_parameterName), _floatValue, _operator),
            Type.Predicate => _predicate(),
            _ => false,
        };

        private bool EvaluateInt(int currentValue, int targetValue, Operator op) => op switch
        {
            Operator.Equals => currentValue == targetValue,
            Operator.NotEquals => currentValue != targetValue,
            Operator.Greater => currentValue > targetValue,
            Operator.GreaterOrEqual => currentValue >= targetValue,
            Operator.Less => currentValue < targetValue,
            Operator.LessOrEqual => currentValue <= targetValue,
            _ => false,
        };

        private bool EvaluateFloat(float currentValue, float targetValue, Operator op) => op switch
        {
            Operator.Equals => currentValue == targetValue,
            Operator.NotEquals => currentValue != targetValue,
            Operator.Greater => currentValue > targetValue,
            Operator.GreaterOrEqual => currentValue >= targetValue,
            Operator.Less => currentValue < targetValue,
            Operator.LessOrEqual => currentValue <= targetValue,
            _ => false,
        };

        
    }

    public static class OperatorExtensions
    {
        public static string ToFriendlyString(this Operator op) => op switch
        {
            Operator.Equals => "==",
            Operator.NotEquals => "!=",
            Operator.Greater => ">",
            Operator.GreaterOrEqual => ">=",
            Operator.Less => "<",
            Operator.LessOrEqual => "<=",
            _ => op.ToString(),
        };
    }
}
