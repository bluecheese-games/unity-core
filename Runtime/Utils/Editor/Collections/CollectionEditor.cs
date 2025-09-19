//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	[CustomEditor(typeof(Collection<>))]
	public class CollectionEditor : AssetBaseEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();

			var itemsProperty = serializedObject.FindProperty("_items");
			EditorGUILayout.PropertyField(itemsProperty);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
