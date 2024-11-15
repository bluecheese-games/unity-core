//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	[CustomEditor(typeof(Collection))]
	public class CollectionEditor : AssetBaseEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();

			var collectionTypeProperty = serializedObject.FindProperty("_collectionType");
			EditorGUILayout.PropertyField(collectionTypeProperty);

			EditorGUILayout.Separator();

			Collection.Type collectionType = (Collection.Type)collectionTypeProperty.enumValueIndex;
			var valuesProperty = GetValuesProperty(collectionType);
			EditorGUILayout.PropertyField(valuesProperty, new GUIContent("Items"), true, GUILayout.ExpandHeight(true));

			serializedObject.ApplyModifiedProperties();
		}

		private SerializedProperty GetValuesProperty(Collection.Type collectionType) => collectionType switch
		{
			Collection.Type.Int => serializedObject.FindProperty("_ints"),
			Collection.Type.Float => serializedObject.FindProperty("_floats"),
			Collection.Type.String => serializedObject.FindProperty("_strings"),
			Collection.Type.Object => serializedObject.FindProperty("_objects"),
			_ => null,
		};
	}
}
