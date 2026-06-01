//
// Copyright (c) 2026 BlueCheese Games All rights reserved
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
			var assets = FindAssets().ToList();
			bank.Feed(assets);
			ConfigureAddressables(assets);
			Debug.Log($"Regenerated AssetBank in {sw.ElapsedMilliseconds}ms");

			DevMetricRecorder.Record("AssetBank Regen", sw.Elapsed.TotalSeconds);
		}

		public static IEnumerable<AssetBase> FindAssets() =>
			AssetDatabase.FindAssets($"t:{nameof(AssetBase)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<AssetBase>)
				.Where(asset => asset != null && asset.RegisterInAssetBank)
				.OrderBy(asset => asset.Name);

		// Ensures every Addressables asset is registered in the Addressables system with its GUID as address.
		// No-op when the UNITY_ADDRESSABLES symbol is not defined.
		private static void ConfigureAddressables(IEnumerable<AssetBase> assets)
		{
#if UNITY_ADDRESSABLES
			var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null) return;

			foreach (var asset in assets)
			{
				if (asset.LoadMode != BlueCheese.Core.Utils.AssetLoadMode.Addressables) continue;

				string guid = asset.Guid;
				var entry = settings.FindAssetEntry(guid);
				if (entry == null)
					entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, postEvent: false);

				// Use the GUID as the address so AssetBaseRef can load by GUID at runtime.
				if (entry.address != guid)
				{
					entry.address = guid;
					UnityEditor.EditorUtility.SetDirty(settings);
				}
			}

			UnityEditor.AssetDatabase.SaveAssets();
#endif
		}
	}
}
