//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

namespace BlueCheese.Core.Editor
{
	[InitializeOnLoad]
	public static class AssetBankGenerator
	{
		private const float MinIntervalSeconds = 1f;

		private static float _lastGenTime = 0;
		private static bool _regenPending = false;

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

			// Debounce: if a regeneration just ran, defer this one to the next editor tick
			// instead of dropping it, so the final project state is always reflected.
			if (_lastGenTime > 0 && Time.realtimeSinceStartup - _lastGenTime < MinIntervalSeconds)
			{
				if (_regenPending)
				{
					return;
				}
				_regenPending = true;
				EditorApplication.delayCall += () =>
				{
					_regenPending = false;
					Regenerate();
				};
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
			// Feed() only marks the bank dirty (EditorUtility.SetDirty); without an explicit save the
			// regenerated data stays in memory and the .asset file on disk never reflects it (e.g. a
			// stale TypeName after a rename, or a deleted asset still listed).
			AssetDatabase.SaveAssets();
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

			bool changed = false;
			foreach (var asset in assets)
			{
				if (asset.LoadMode != BlueCheese.Core.Utils.AssetLoadMode.Addressables) continue;

				string guid = asset.Guid;

				// Route the asset to the group matching its bundle key (one bundle per key).
				var group = GetOrCreateGroup(settings, GetGroupName(asset.BundleKey), ref changed);

				var entry = settings.FindAssetEntry(guid);
				if (entry == null || entry.parentGroup != group)
				{
					entry = settings.CreateOrMoveEntry(guid, group, postEvent: false);
					changed = true;
				}

				// Use the GUID as the address so AssetBaseRef can load by GUID at runtime.
				if (entry.address != guid)
				{
					entry.address = guid;
					changed = true;
				}

				// Mirror the asset tags onto Addressables labels so assets are queryable by tag.
				// An asset can carry several labels (one per tag).
				changed |= SyncLabels(entry, (string[])asset.Tags);
			}

			// Drop managed groups left empty (e.g. after a bundle key changed) to avoid clutter.
			RemoveEmptyManagedGroups(settings, ref changed);

			// Avoid dirtying and saving the whole project on every regeneration when nothing changed.
			if (changed)
			{
				UnityEditor.EditorUtility.SetDirty(settings);
				UnityEditor.AssetDatabase.SaveAssets();
			}
#endif
		}

#if UNITY_ADDRESSABLES
		internal const string AddressableGroupPrefix = "AssetBank";
		internal const string DefaultAddressableGroup = AddressableGroupPrefix + "_Default";

		// Assets without a bundle key share the default group; keyed assets get their own group.
		private static string GetGroupName(string bundleKey) =>
			string.IsNullOrWhiteSpace(bundleKey) ? DefaultAddressableGroup : $"{AddressableGroupPrefix}_{bundleKey}";

		// Finds or creates the group and forces it to pack into a single bundle (Pack Together).
		private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string name, ref bool changed)
		{
			var group = settings.FindGroup(name);
			if (group == null)
			{
				group = settings.CreateGroup(name, setAsDefaultGroup: false, readOnly: false, postEvent: false,
					schemasToCopy: null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
				changed = true;
			}

			var schema = group.GetSchema<BundledAssetGroupSchema>();
			if (schema == null)
			{
				schema = group.AddSchema<BundledAssetGroupSchema>();
				changed = true;
			}

			if (schema.BundleMode != BundledAssetGroupSchema.BundlePackingMode.PackTogether)
			{
				schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
				changed = true;
			}

			return group;
		}

		// Removes empty groups previously created by this generator, keeping the project tidy.
		private static void RemoveEmptyManagedGroups(AddressableAssetSettings settings, ref bool changed)
		{
			foreach (var group in settings.groups.ToArray())
			{
				if (group == null || group == settings.DefaultGroup) continue;
				if (!group.Name.StartsWith(AddressableGroupPrefix)) continue;
				if (group.entries.Count > 0) continue;

				settings.RemoveGroup(group);
				changed = true;
			}
		}

		// Makes the entry's labels match exactly the asset's tags. The label is created in the
		// Addressables settings on demand (force: true). Returns true if anything changed.
		private static bool SyncLabels(AddressableAssetEntry entry, string[] tags)
		{
			bool changed = false;
			var desired = new HashSet<string>(tags ?? System.Array.Empty<string>());

			foreach (string tag in desired)
			{
				if (string.IsNullOrEmpty(tag) || entry.labels.Contains(tag)) continue;
				entry.SetLabel(tag, enable: true, force: true, postEvent: false);
				changed = true;
			}

			// Remove labels that are no longer tags (copy first: SetLabel mutates entry.labels).
			foreach (string label in entry.labels.ToArray())
			{
				if (desired.Contains(label)) continue;
				entry.SetLabel(label, enable: false, force: false, postEvent: false);
				changed = true;
			}

			return changed;
		}
#endif
	}
}
