using BlueCheese.Core.Utils;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// Inspector drawer for <see cref="CollectionItemRef{T}"/>: a single row with the source
	/// collection object field on the left and a dropdown to pick one of its items on the right.
	/// </summary>
	[CustomPropertyDrawer(typeof(CollectionItemRef<>))]
	public class CollectionItemRefPropertyDrawer : PropertyDrawer
	{
		private const float FieldGapX = 4f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			=> EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var collectionProperty = property.FindPropertyRelative("_collection");
			var itemProperty = property.FindPropertyRelative("_item");

			var contentRect = EditorGUI.PrefixLabel(position, label);
			float halfWidth = (contentRect.width - FieldGapX) * 0.5f;
			var collectionRect = new Rect(contentRect.x, contentRect.y, halfWidth, contentRect.height);
			var itemRect = new Rect(collectionRect.xMax + FieldGapX, contentRect.y, contentRect.width - halfWidth - FieldGapX, contentRect.height);

			// EditorGUI.BeginChangeCheck/EndChangeCheck is Unity's own recommended way to detect an
			// object field's value changing -- it correctly covers every assignment path (drag & drop,
			// the object picker window's async callback, "None" via the context menu), unlike a manual
			// before/after reference comparison which can miss the picker's deferred callback.
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(collectionRect, collectionProperty, GUIContent.none);
			bool collectionChanged = EditorGUI.EndChangeCheck();

			var candidateItems = CollectionRefEditorUtility.GetCandidateItems(collectionProperty.objectReferenceValue);

			// Source collection swapped: clear the picked item if it's no longer part of the new one.
			if (collectionChanged && itemProperty.objectReferenceValue != null
				&& Array.IndexOf(candidateItems, itemProperty.objectReferenceValue) < 0)
			{
				itemProperty.objectReferenceValue = null;
			}

			using (new EditorGUI.DisabledScope(collectionProperty.objectReferenceValue == null))
			{
				string[] options = new[] { "None" }
					.Concat(candidateItems.Select(CollectionRefEditorUtility.GetDisplayName))
					.ToArray();

				int currentIndex = itemProperty.objectReferenceValue != null
					? Array.IndexOf(candidateItems, itemProperty.objectReferenceValue) + 1
					: 0;
				if (currentIndex < 0) currentIndex = 0; // picked item no longer present -- show as None

				int newIndex = EditorGUI.Popup(itemRect, currentIndex, options);
				if (newIndex != currentIndex)
					itemProperty.objectReferenceValue = newIndex == 0 ? null : candidateItems[newIndex - 1];
			}

			EditorGUI.EndProperty();
		}
	}
}
