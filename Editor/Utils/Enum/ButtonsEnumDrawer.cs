#if UNITY_EDITOR
using BlueCheese.Core.Utils;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ButtonsEnumAttribute))]
public class ButtonsEnumDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		if (property.propertyType == SerializedPropertyType.Enum)
		{
			string[] enumNames = property.enumDisplayNames;
			int selectedIndex = property.enumValueIndex;

			float buttonWidth = position.width / enumNames.Length;
			Rect buttonRect = new(position.x, position.y, buttonWidth, position.height);

			for (int i = 0; i < enumNames.Length; i++)
			{
				GUIStyle buttonStyle;
				if (enumNames.Length == 1) buttonStyle = UnityEditor.EditorStyles.miniButton;
				else if (i == 0) buttonStyle = EditorStyles.miniButtonLeft;
				else if (i == enumNames.Length - 1) buttonStyle = EditorStyles.miniButtonRight;
				else buttonStyle = EditorStyles.miniButtonMid;

				bool isSelected = (i == selectedIndex);

				// Check the NEW state of the toggle
				bool newState = GUI.Toggle(buttonRect, isSelected, enumNames[i], buttonStyle);

				// Only apply the change if it was just turned ON (ignoring the ones that just stayed on)
				if (newState && !isSelected)
				{
					property.enumValueIndex = i;
				}

				buttonRect.x += buttonWidth;
			}
		}
		else
		{
			EditorGUI.LabelField(position, "ButtonsEnum Attribute requires an Enum.");
		}

		EditorGUI.EndProperty();
	}
}
#endif
