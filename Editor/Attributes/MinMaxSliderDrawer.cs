using BlueCheese.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
	public class MinMaxSliderDrawer : PropertyDrawer
	{
		private const float FieldWidth = 52f; // +30% over the original 40f
		private const float Spacing = 4f;
		private const int DisplayedDecimals = 3;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// This drawer always renders min field + slider + max field on a single row.
			// Without this override, Vector2's default height kicks in (2 lines when not in wide mode),
			// which starves OnGUI's single-line layout and squishes/clips the controls.
			return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var range = (MinMaxSliderAttribute)attribute;

			if (property.propertyType != SerializedPropertyType.Vector2)
			{
				EditorGUI.PropertyField(position, property, label);
				return;
			}

			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, label);

			// PrefixLabel already accounted for the indent when carving out the content rect.
			// Leaving indentLevel untouched would make every raw-Rect control below (FloatField,
			// MinMaxSlider) apply the same indent inset a second time, pushing everything right
			// and shrinking the fields — very noticeable on nested fields like SoundOptions.Pitch.
			int previousIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			Vector2 value = property.vector2Value;
			float min = RoundToDecimals(value.x, DisplayedDecimals);
			float max = RoundToDecimals(value.y, DisplayedDecimals);

			var minFieldRect = new Rect(position.x, position.y, FieldWidth, position.height);
			var sliderRect = new Rect(minFieldRect.xMax + Spacing, position.y, position.width - 2f * (FieldWidth + Spacing), position.height);
			var maxFieldRect = new Rect(sliderRect.xMax + Spacing, position.y, FieldWidth, position.height);

			min = RoundToDecimals(EditorGUI.FloatField(minFieldRect, min), DisplayedDecimals);
			EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, range.MinLimit, range.MaxLimit);
			max = RoundToDecimals(EditorGUI.FloatField(maxFieldRect, max), DisplayedDecimals);

			min = Mathf.Clamp(RoundToDecimals(min, DisplayedDecimals), range.MinLimit, max);
			max = Mathf.Clamp(RoundToDecimals(max, DisplayedDecimals), min, range.MaxLimit);

			property.vector2Value = new Vector2(min, max);

			EditorGUI.indentLevel = previousIndentLevel;
			EditorGUI.EndProperty();
		}

		private static float RoundToDecimals(float value, int decimals)
		{
			float factor = Mathf.Pow(10f, decimals);
			return Mathf.Round(value * factor) / factor;
		}
	}
}
