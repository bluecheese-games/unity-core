//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{

	[CustomEditor(typeof(AssetBase), editorForChildClasses: true)]
	public class AssetBaseEditor : UnityEditor.Editor
	{
		bool _foldout = true;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.BeginVertical("box");
			_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout, "Asset identifiers");
			EditorGUILayout.EndFoldoutHeaderGroup();
			if (_foldout)
			{
				var idProperty = serializedObject.FindProperty("_id");
				EditorGUILayout.PropertyField(idProperty);

				var tagsProperty = serializedObject.FindProperty("_tags");
				EditorGUILayout.PropertyField(tagsProperty, true);
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
