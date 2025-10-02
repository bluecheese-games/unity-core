//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	[CustomEditor(typeof(AssetBank))]
	public class AssetBankEditor : UnityEditor.Editor
	{
		private SerializedProperty _assetsProperty;

		private void OnEnable()
		{
			_assetsProperty = serializedObject.FindProperty("_assets");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			if (GUILayout.Button("Regenerate"))
			{
				AssetBankGenerator.Regenerate();
				return;
			}

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);
			GUI.enabled = false;
			for (int i = 0; i < _assetsProperty.arraySize; i++)
			{
				var assetProperty = _assetsProperty.GetArrayElementAtIndex(i);
				string name = (assetProperty.objectReferenceValue as AssetBase).Name;
				EditorGUILayout.PropertyField(assetProperty, new GUIContent(name));
			}
			GUI.enabled = true;
			EditorGUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
