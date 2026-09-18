using BlueCheese.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(HideIfAttribute))]
	public class HideIfDrawer : ConditionDrawerBase
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return !EvaluateCondition(property) ? EditorGUI.GetPropertyHeight(property, label, true) : 0f;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!EvaluateCondition(property))
			{
				EditorGUI.PropertyField(position, property, label, true);
			}
		}
	}
}
