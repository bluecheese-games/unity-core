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
			EditorGUI.indentLevel++;
			for (int i = 0; i < _assetsProperty.arraySize; i++)
			{
				var assetProperty = _assetsProperty.GetArrayElementAtIndex(i);
				var name = assetProperty.FindPropertyRelative("Name").stringValue;
				EditorGUILayout.BeginHorizontal();
				GUI.enabled = false;
				EditorGUILayout.PropertyField(assetProperty, new GUIContent(name));
				GUI.enabled = true;
				if (GUILayout.Button(EditorIcon.Select, EditorStyles.iconButton, GUILayout.Width(20), GUILayout.Height(20)))
				{
					if (assetProperty.boxedValue is AssetBaseRef assetBaseRef &&
						assetBaseRef.TryLoad<AssetBase>(out var asset))
					{
						Selection.activeObject = asset;
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
