//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System;
using System.Text;
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
			SerializedProperty valueProperty = property.FindPropertyRelative("_bitValue");
			if (valueProperty == null)
			{
				EditorGUI.LabelField(position, label.text, "Use FlagEnum<T> with a serializable enum.");
				return;
			}

			Type enumType = fieldInfo.FieldType.GetGenericArguments()[0];
			if (!enumType.IsEnum)
			{
				EditorGUI.LabelField(position, label.text, "T must be an Enum");
				return;
			}

			Array enumValues = Enum.GetValues(enumType);
			string[] enumNames = Enum.GetNames(enumType);
			if (enumValues.Length > MaxFlagValues)
			{
				EditorGUI.LabelField(position, label.text, $"Too many enum values (>{MaxFlagValues})");
				return;
			}

			long value = valueProperty.longValue;

			Rect fieldRect = EditorGUI.PrefixLabel(position, label);
			if (GUI.Button(fieldRect, BuildDisplayString(enumValues, value), EditorStyles.popup))
			{
				GenericMenu menu = new GenericMenu();

				// Compute the "all" mask by OR-ing actual enum values (sparse-safe)
				long allMask = 0;
				for (int i = 0; i < enumValues.Length; i++)
				{
					long idx = Convert.ToInt64(enumValues.GetValue(i));
					if (idx >= 0 && idx <= 63) allMask |= 1L << (int)idx;
				}

				// None / All
				menu.AddItem(new GUIContent("None"), value == 0, () =>
				{
					valueProperty.serializedObject.Update();
					valueProperty.longValue = 0;
					valueProperty.serializedObject.ApplyModifiedProperties();
				});
				menu.AddItem(new GUIContent("All"), value == allMask, () =>
				{
					valueProperty.serializedObject.Update();
					valueProperty.longValue = allMask;
					valueProperty.serializedObject.ApplyModifiedProperties();
				});
				menu.AddItem(new GUIContent("----------------"), false, null);

				// Per-flag items (use underlying values, not the loop index)
				for (int i = 0; i < enumValues.Length; i++)
				{
					object enumValObj = enumValues.GetValue(i);
					string enumName = enumValObj.ToString();
					long idx = Convert.ToInt64(enumValObj);
					if (idx < 0 || idx > 63) continue; // out-of-range flags are skipped in the UI
					long flagValue = 1L << (int)idx;
					bool isSet = (value & flagValue) != 0;

					menu.AddItem(new GUIContent(enumName), isSet, () =>
					{
						valueProperty.serializedObject.Update();
						long v = valueProperty.longValue;
						if ((v & flagValue) != 0) v &= ~flagValue;
						else v |= flagValue;
						valueProperty.longValue = v;
						valueProperty.serializedObject.ApplyModifiedProperties();
					});
				}

				menu.DropDown(fieldRect);
			}
		}

		private static string BuildDisplayString(Array enumValues, long value)
		{
			StringBuilder sb = new();
			bool first = true;
			for (int i = 0; i < enumValues.Length; i++)
			{
				long idx = Convert.ToInt64(enumValues.GetValue(i));
				if (idx < 0 || idx > 63) continue;
				long flagValue = 1L << (int)idx;
				if ((value & flagValue) != 0)
				{
					if (!first) sb.Append(", ");
					sb.Append(enumValues.GetValue(i).ToString());
					first = false;
				}
			}
			if (sb.Length == 0) return "None";
			return sb.ToString();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}
