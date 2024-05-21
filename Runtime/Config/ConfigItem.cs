//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Core.Config
{
    [Serializable]
    public class ConfigItem
    {
        public string Key;
        public ValueType Type = ValueType.String;

        public string StringValue;
        public int IntValue;
        public float FloatValue;
        public bool BoolValue;
        public UnityEngine.Object ObjectValue;

        public ConfigItem(string key, ValueType type)
        {
            this.Key = key;
            this.Type = type;
        }

        public void Cleanup()
        {
            StringValue = Type == ValueType.String ? StringValue : default;
            IntValue = Type == ValueType.Int ? IntValue : default;
            FloatValue = Type == ValueType.Float ? FloatValue : default;
            BoolValue = Type == ValueType.Boolean ? BoolValue : default;
            ObjectValue = Type == ValueType.Object ? ObjectValue : default;
        }

        public enum ValueType
        {
            String,
            Int,
            Float,
            Boolean,
            Object,
        }
    }
}
