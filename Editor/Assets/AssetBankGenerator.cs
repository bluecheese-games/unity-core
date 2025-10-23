//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[InitializeOnLoad]
	public static class AssetBankGenerator
	{
		private static float _lastGenTime = 0;

		static AssetBankGenerator()
		{
			EditorApplication.delayCall += () =>
			{
				Regenerate();
			};
		}

		public static void Regenerate()
		{
			if (Application.isPlaying)
			{
				return;
			}

			// Prevent multiple regenerations in a short time
			if (_lastGenTime > 0 && Time.realtimeSinceStartup - _lastGenTime < 1)
			{
				return;
			}
			_lastGenTime = Time.realtimeSinceStartup;

			// Load the AssetBank from Resources
			var bank = Resources.Load<AssetBank>("AssetBank");
			if (bank == null)
			{
				return;
			}

			// Regenerate the assets in the bank
			var sw = System.Diagnostics.Stopwatch.StartNew();
			var assets = FindAssets();
			bank.Feed(assets);
			Debug.Log($"Regenerated AssetBank in {sw.ElapsedMilliseconds}ms");

			DevMetricRecorder.Record("AssetBank Regen", sw.Elapsed.TotalSeconds);
		}

		public static IEnumerable<AssetBase> FindAssets() =>
			AssetDatabase.FindAssets($"t:{nameof(AssetBase)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<AssetBase>)
				.Where(asset => asset.RegisterInAssetBank)
				.OrderBy(asset => asset.Name);
	}
}
