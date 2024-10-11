//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(FlagEnum<>))]
	public class FlagEnumPropertyDrawer : PropertyDrawer
	{
		private const int MaxFlagValues = 32; // 32 bits in an int

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var valueProperty = property.FindPropertyRelative("_value");
			var enumType = fieldInfo.FieldType.GetGenericArguments()[0];
			var enumValues = Enum.GetValues(enumType);

			if (enumValues.Length > MaxFlagValues)
			{
				EditorGUI.HelpBox(position, $"Enum with more than {MaxFlagValues} values not supported ({enumType.Name})", MessageType.Error);
				return;
			}

			EditorGUI.BeginProperty(position, label, property);

			var labels = new string[enumValues.Length];
			for (int i = 0; i < enumValues.Length; i++)
			{
				labels[i] = enumValues.GetValue(i).ToString();
			}

			var selectedValue = 0;
			for (int i = 0; i < enumValues.Length; i++)
			{
				if ((valueProperty.intValue & (1 << i)) != 0)
				{
					selectedValue |= 1 << i;
				}
			}

			var newSelectedValue = EditorGUI.MaskField(position, label, selectedValue, labels);

			valueProperty.intValue = newSelectedValue;

			EditorGUI.EndProperty();
		}
	}
}
