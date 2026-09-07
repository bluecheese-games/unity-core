//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomEditor(typeof(AssetBank))]
	public class AssetBankEditor : UnityEditor.Editor
	{
		private SerializedProperty _assetsProperty;

		private string _searchText = string.Empty;
		private int _loadModeFilter = 0;
		private int _tagFilter = 0;
		private int _bundleFilter = 0;

		private void OnEnable()
		{
			_assetsProperty = serializedObject.FindProperty("_assets");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.BeginHorizontal();
			bool regenerateClicked = GUILayout.Button("Regenerate");
			bool browseClicked = GUILayout.Button("Browse Assets");
			EditorGUILayout.EndHorizontal();

			if (browseClicked)
			{
				AssetBankBrowserWindow.Open();
			}

			if (regenerateClicked)
			{
				AssetBankGenerator.Regenerate();
				return;
			}

			var refs = CollectRefs();
			string[] tagOptions = AssetBankFilterUtility.BuildTagOptions(refs);
			string[] bundleOptions = AssetBankFilterUtility.BuildBundleOptions(refs);
			_tagFilter = Mathf.Clamp(_tagFilter, 0, tagOptions.Length - 1);
			_bundleFilter = Mathf.Clamp(_bundleFilter, 0, bundleOptions.Length - 1);

			DrawFilters(tagOptions, bundleOptions);

			AssetLoadMode? loadModeFilter = _loadModeFilter <= 0 ? null : (AssetLoadMode)(_loadModeFilter - 1);
			string tagFilter = _tagFilter <= 0 ? null : tagOptions[_tagFilter];
			string bundleFilter = _bundleFilter <= 0 ? null : bundleOptions[_bundleFilter];
			string search = _searchText?.Trim();

			DrawAssetList(refs, search, loadModeFilter, tagFilter, bundleFilter);

			serializedObject.ApplyModifiedProperties();
		}

		#region Drawing

		private void DrawFilters(string[] tagOptions, string[] bundleOptions)
		{
			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.LabelField("Filter", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			_searchText = EditorGUILayout.TextField("Search (name / GUID / tag)", _searchText);
			if (GUILayout.Button("Clear", GUILayout.Width(50)))
			{
				_searchText = string.Empty;
				_loadModeFilter = 0;
				_tagFilter = 0;
				_bundleFilter = 0;
				GUI.FocusControl(null);
			}
			EditorGUILayout.EndHorizontal();

			_loadModeFilter = EditorGUILayout.Popup("Load Mode", _loadModeFilter, AssetBankFilterUtility.LoadModeOptions);
			_tagFilter = EditorGUILayout.Popup("Tag", _tagFilter, tagOptions);
			_bundleFilter = EditorGUILayout.Popup("Bundle", _bundleFilter, bundleOptions);

			EditorGUILayout.EndVertical();
		}

		private void DrawAssetList(List<AssetBaseRef> refs, string search, AssetLoadMode? loadMode, string tag, string bundle)
		{
			var matchingIndices = new List<int>();
			for (int i = 0; i < refs.Count; i++)
				if (AssetBankFilterUtility.Matches(refs[i], search, loadMode, tag, bundle))
					matchingIndices.Add(i);

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField($"Assets ({matchingIndices.Count}/{refs.Count})", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			if (matchingIndices.Count == 0)
			{
				EditorGUILayout.LabelField("No asset matches the current filters.", EditorStyles.miniLabel);
			}

			foreach (int i in matchingIndices)
			{
				var assetProperty = _assetsProperty.GetArrayElementAtIndex(i);
				DrawAssetRow(assetProperty, refs[i]);
			}

			EditorGUI.indentLevel--;
			EditorGUILayout.EndVertical();
		}

		private static void DrawAssetRow(SerializedProperty assetProperty, AssetBaseRef assetRef)
		{
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = false;
			EditorGUILayout.PropertyField(assetProperty, new GUIContent(assetRef.Name));
			GUI.enabled = true;

			if (GUILayout.Button(EditorIcon.Select, EditorStyles.iconButton, GUILayout.Width(20), GUILayout.Height(20)))
			{
				if (assetRef.TryLoad<AssetBase>(out var asset))
				{
					Selection.activeObject = asset;
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region Filtering

		private List<AssetBaseRef> CollectRefs()
		{
			var refs = new List<AssetBaseRef>(_assetsProperty.arraySize);
			for (int i = 0; i < _assetsProperty.arraySize; i++)
			{
				if (_assetsProperty.GetArrayElementAtIndex(i).boxedValue is AssetBaseRef assetRef)
					refs.Add(assetRef);
			}
			return refs;
		}

		#endregion
	}
}
