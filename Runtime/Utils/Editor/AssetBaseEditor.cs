//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{

	[CustomEditor(typeof(AssetBase), editorForChildClasses: true)]
	public class AssetBaseEditor : UnityEditor.Editor
	{
		static bool _foldout = true;

		private SerializedProperty _nameProperty;
		private SerializedProperty _tagsProperty;
		private SerializedProperty _registerProperty;

		private AssetBase _asset => target as AssetBase;

		virtual protected void OnEnable()
		{
			_nameProperty = serializedObject.FindProperty(nameof(_asset.Name));
			_tagsProperty = serializedObject.FindProperty(nameof(_asset.Tags));
			_registerProperty = serializedObject.FindProperty(nameof(_asset.RegisterInAssetBank));
		}

		protected override void OnHeaderGUI()
		{
			serializedObject.Update();

			EditorGUILayout.BeginVertical("box");
			_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout, $"Asset #{_asset.Guid}");
			EditorGUILayout.EndFoldoutHeaderGroup();
			if (_foldout)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(_registerProperty);
				if (GUILayout.Button("Open Asset Bank", GUILayout.Width(150)))
				{
					AssetBank.SelectInProject();
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.PropertyField(_nameProperty);
				EditorGUILayout.PropertyField(_tagsProperty, true);
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
