//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomEditor(typeof(Collection<>), editorForChildClasses: true)]
	public class CollectionEditor : AssetBaseEditor
	{
		private ReorderableList _itemsList;

		override protected void OnEnable()
		{
			base.OnEnable();

			var itemsProperty = serializedObject.FindProperty("_items");
			if (itemsProperty == null) return;

			// Headerless reorderable list: keeps add/remove/reorder but hides the "Items" foldout.
			_itemsList = new ReorderableList(serializedObject, itemsProperty,
				draggable: true, displayHeader: false, displayAddButton: true, displayRemoveButton: true)
			{
				drawElementCallback = (rect, index, isActive, isFocused) =>
				{
					var element = itemsProperty.GetArrayElementAtIndex(index);
					rect.y += 2;
					rect.height = EditorGUI.GetPropertyHeight(element, includeChildren: true);
					EditorGUI.PropertyField(rect, element, GUIContent.none, includeChildren: true);
				},
				elementHeightCallback = index =>
					EditorGUI.GetPropertyHeight(itemsProperty.GetArrayElementAtIndex(index), includeChildren: true) + 4,
			};
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();
			_itemsList?.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}
