using BlueCheese.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class RequiredDrawer : PropertyDrawer
	{
		private const float WarningHeight = 20f;
		private const float Spacing = 2f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUI.GetPropertyHeight(property, label, true);
			if (IsMissing(property))
			{
				height += WarningHeight + Spacing;
			}
			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			float fieldHeight = EditorGUI.GetPropertyHeight(property, label, true);
			var fieldRect = new Rect(position.x, position.y, position.width, fieldHeight);
			EditorGUI.PropertyField(fieldRect, property, label, true);

			if (IsMissing(property))
			{
				var required = (RequiredAttribute)attribute;
				string message = string.IsNullOrEmpty(required.Message)
					? $"{ObjectNames.NicifyVariableName(fieldInfo.Name)} is required."
					: required.Message;

				var warningRect = new Rect(position.x, fieldRect.yMax + Spacing, position.width, WarningHeight);
				EditorGUI.HelpBox(warningRect, message, MessageType.Warning);
			}
		}

		private static bool IsMissing(SerializedProperty property)
		{
			return property.propertyType switch
			{
				SerializedPropertyType.ObjectReference => property.objectReferenceValue == null,
				SerializedPropertyType.String => string.IsNullOrWhiteSpace(property.stringValue),
				_ => false
			};
		}
	}
}
