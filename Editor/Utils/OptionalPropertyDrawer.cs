using BlueCheese.Core.Utils;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// Custom Inspector drawer for <see cref="Optional{T}"/>. Renders as a single checkbox line (label +
	/// "Enabled" toggle); the wrapped value only appears, indented, once the checkbox is ticked.
	/// </summary>
	[CustomPropertyDrawer(typeof(Optional<>))]
	public class OptionalPropertyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var enabledProperty = property.FindPropertyRelative("_enabled");
			float height = EditorGUIUtility.singleLineHeight;

			if (enabledProperty.boolValue)
			{
				var valueProperty = property.FindPropertyRelative("_value");
				height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(valueProperty, true);
			}

			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var enabledProperty = property.FindPropertyRelative("_enabled");
			var valueProperty = property.FindPropertyRelative("_value");

			EditorGUI.BeginProperty(position, label, property);

			float line = EditorGUIUtility.singleLineHeight;
			var toggleRect = new Rect(position.x, position.y, position.width, line);
			EditorGUI.PropertyField(toggleRect, enabledProperty, label);

			if (enabledProperty.boolValue)
			{
				var valueRect = new Rect(position.x, toggleRect.yMax + EditorGUIUtility.standardVerticalSpacing,
					position.width, EditorGUI.GetPropertyHeight(valueProperty, true));

				EditorGUI.indentLevel++;
				EditorGUI.PropertyField(valueRect, valueProperty, new GUIContent("Value"), true);
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}
	}
}
