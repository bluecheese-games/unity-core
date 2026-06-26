//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomEditor(typeof(AssetBank))]
	public class AssetBankEditor : UnityEditor.Editor
	{
		// First option is the "no filter" entry; the rest map to AssetLoadMode values.
		private static readonly string[] _loadModeOptions =
			new[] { "All" }.Concat(Enum.GetNames(typeof(AssetLoadMode))).ToArray();

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

			if (GUILayout.Button("Regenerate"))
			{
				AssetBankGenerator.Regenerate();
				return;
			}

			var refs = CollectRefs();
			string[] tagOptions = BuildTagOptions(refs);
			string[] bundleOptions = BuildBundleOptions(refs);
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

			_loadModeFilter = EditorGUILayout.Popup("Load Mode", _loadModeFilter, _loadModeOptions);
			_tagFilter = EditorGUILayout.Popup("Tag", _tagFilter, tagOptions);
			_bundleFilter = EditorGUILayout.Popup("Bundle", _bundleFilter, bundleOptions);

			EditorGUILayout.EndVertical();
		}

		private void DrawAssetList(List<AssetBaseRef> refs, string search, AssetLoadMode? loadMode, string tag, string bundle)
		{
			var matchingIndices = new List<int>();
			for (int i = 0; i < refs.Count; i++)
				if (Matches(refs[i], search, loadMode, tag, bundle))
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

		private static string[] BuildTagOptions(List<AssetBaseRef> refs)
		{
			var tags = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var assetRef in refs)
				foreach (string tag in (string[])assetRef.Tags)
					if (!string.IsNullOrEmpty(tag)) tags.Add(tag);

			return new[] { "All" }.Concat(tags).ToArray();
		}

		private static string[] BuildBundleOptions(List<AssetBaseRef> refs)
		{
			var bundles = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var assetRef in refs)
				if (!string.IsNullOrEmpty(assetRef.BundleKey)) bundles.Add(assetRef.BundleKey);

			return new[] { "All" }.Concat(bundles).ToArray();
		}

		private static bool Matches(AssetBaseRef assetRef, string search, AssetLoadMode? loadMode, string tag, string bundle)
		{
			if (loadMode.HasValue && assetRef.LoadMode != loadMode.Value)
				return false;

			if (!string.IsNullOrEmpty(tag) && !assetRef.Tags.Contains(tag))
				return false;

			if (!string.IsNullOrEmpty(bundle) && assetRef.BundleKey != bundle)
				return false;

			if (!string.IsNullOrEmpty(search))
			{
				bool hit =
					ContainsIgnoreCase(assetRef.Name, search) ||
					ContainsIgnoreCase(assetRef.Guid, search) ||
					((string[])assetRef.Tags).Any(t => ContainsIgnoreCase(t, search));
				if (!hit) return false;
			}

			return true;
		}

		private static bool ContainsIgnoreCase(string value, string search) =>
			!string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

		#endregion
	}
}
