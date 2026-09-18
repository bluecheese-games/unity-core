using BlueCheese.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(EnableIfAttribute))]
	public class EnableIfDrawer : ConditionDrawerBase
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			bool wasEnabled = GUI.enabled;
			GUI.enabled = wasEnabled && EvaluateCondition(property);
			EditorGUI.PropertyField(position, property, label, true);
			GUI.enabled = wasEnabled;
		}
	}
}
