//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(FlagEnum<>))]
	public class FlagEnumPropertyDrawer : PropertyDrawer
	{
		private const int MaxFlagValues = 64;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var valueProperty = property.FindPropertyRelative("_bitValue");
			var enumType = fieldInfo.FieldType.GetGenericArguments()[0];
			var enumValues = Enum.GetValues(enumType);

			if (enumValues.Length > MaxFlagValues)
			{
				EditorGUI.HelpBox(position, $"Enum with more than {MaxFlagValues} values not supported ({enumType.Name})", MessageType.Error);
				return;
			}

			// Draw prefix label and adjust position for the field
			position = EditorGUI.PrefixLabel(position, label);

			long value = valueProperty.longValue;
			string display = BuildDisplayString(enumType, value);

			if (EditorGUI.DropdownButton(position, new GUIContent(display), FocusType.Keyboard))
			{
				GenericMenu menu = new();
				menu.AddItem(new GUIContent("(None)"), value == 0, () =>
				{
					valueProperty.serializedObject.Update();
					valueProperty.longValue = 0;
					valueProperty.serializedObject.ApplyModifiedProperties();
				});
				menu.AddItem(new GUIContent("(All)"), value == (1L << enumValues.Length) - 1, () =>
				{
					valueProperty.serializedObject.Update();
					valueProperty.longValue = (1L << enumValues.Length) - 1;
					valueProperty.serializedObject.ApplyModifiedProperties();
				});
				menu.AddItem(new GUIContent("----------------"), false, null);
				for (int i = 0; i < enumValues.Length; i++)
				{
					object enumValObj = enumValues.GetValue(i);
					string enumName = enumValObj.ToString();
					long flagValue = 1L << i; // Use Convert.ToInt64(enumValObj) for custom enums
					bool isSet = (value & flagValue) != 0;

					menu.AddItem(new GUIContent(enumName), isSet, () =>
					{
						long newValue = value;
						if (isSet)
							newValue &= ~flagValue;
						else
							newValue |= flagValue;

						valueProperty.serializedObject.Update();
						valueProperty.longValue = newValue;
						valueProperty.serializedObject.ApplyModifiedProperties();
					});
				}
				menu.DropDown(position);
			}
		}

		private string BuildDisplayString(Type enumType, long value)
		{
			var enumValues = Enum.GetValues(enumType);
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			bool first = true;
			for (int i = 0; i < enumValues.Length; i++)
			{
				long flagValue = 1L << i; // Use Convert.ToInt64(enumValues.GetValue(i)) for custom enums
				if ((value & flagValue) != 0)
				{
					if (!first) sb.Append(", ");
					sb.Append(enumValues.GetValue(i).ToString());
					first = false;
				}
			}
			if (sb.Length == 0) return "(None)";
			return sb.ToString();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}
