//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	[CustomEditor(typeof(DevMetricDataAsset))]
	public class DevMetricDataAssetEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			// Draw default fields (project/user names etc.)
			DrawDefaultInspector();

			EditorGUILayout.Space();

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Open in DevMetric Viewer", GUILayout.Height(24)))
				{
					var asset = (DevMetricDataAsset)target;
					DevMetricsViewerWindow.OpenAndShow(asset);
				}
				GUILayout.FlexibleSpace();
			}
		}
	}

	public static class DevMetricAssetOpener
	{
		// Handles double-click on the asset in Project window
		[OnOpenAsset]
		public static bool OnOpenAsset(int instanceID, int line)
		{
			var obj = EditorUtility.InstanceIDToObject(instanceID);
			var asset = obj as DevMetricDataAsset;
			if (asset == null) return false;

			DevMetricsViewerWindow.OpenAndShow(asset);
			return true; // we handled it
		}
	}
}
