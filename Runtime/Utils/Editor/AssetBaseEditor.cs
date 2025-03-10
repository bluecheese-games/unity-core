//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEditor;

namespace BlueCheese.Core.Utils.Editor
{

	[CustomEditor(typeof(AssetBase), editorForChildClasses: true)]
	public class AssetBaseEditor : UnityEditor.Editor
	{
		static bool _foldout = true;

		private SerializedProperty _nameProperty;
		private SerializedProperty _tagsProperty;
		private SerializedProperty _registerProperty;

		private void OnEnable()
		{
			_nameProperty = serializedObject.FindProperty("Name");
			_tagsProperty = serializedObject.FindProperty("Tags");
			_registerProperty = serializedObject.FindProperty("RegisterInAssetBank");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.BeginVertical("box");
			_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout, "Asset Bank");
			EditorGUILayout.EndFoldoutHeaderGroup();
			if (_foldout)
			{
				EditorGUILayout.PropertyField(_registerProperty);
				EditorGUILayout.PropertyField(_nameProperty);
				EditorGUILayout.PropertyField(_tagsProperty, true);
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
