using BlueCheese.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	// A DecoratorDrawer renders above the field, independently of whatever PropertyDrawer
	// handles the field's value — so InfoBox composes freely with ShowIf, ReadOnly, etc.
	// The trade-off is that DecoratorDrawer.OnGUI has no SerializedProperty/target access,
	// so this can't support a visibility condition; it's a purely static message.
	[CustomPropertyDrawer(typeof(InfoBoxAttribute))]
	public class InfoBoxDrawer : DecoratorDrawer
	{
		private const float Spacing = 2f;

		public override float GetHeight()
		{
			var infoBox = (InfoBoxAttribute)attribute;
			float width = EditorGUIUtility.currentViewWidth - 40f;
			return EditorStyles.helpBox.CalcHeight(new GUIContent(infoBox.Text), width) + Spacing;
		}

		public override void OnGUI(Rect position)
		{
			var infoBox = (InfoBoxAttribute)attribute;
			MessageType messageType = infoBox.Type switch
			{
				InfoBoxType.Warning => MessageType.Warning,
				InfoBoxType.Error => MessageType.Error,
				_ => MessageType.Info
			};

			position.height -= Spacing;
			EditorGUI.HelpBox(position, infoBox.Text, messageType);
		}
	}
}
