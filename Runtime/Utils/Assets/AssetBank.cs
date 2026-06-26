//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("BlueCheese.Core.Tests")]

namespace BlueCheese.Core.Utils
{
	[CreateAssetMenu(menuName = "AssetBank", fileName = "AssetBank")]
	public class AssetBank : ScriptableObject, IAssetBank
	{
		public const string AssetBankResourcePath = "AssetBank";
		public const string AssetsResourcePath = "_Assets";

		private static AssetBank _instance;

		[SerializeField] private List<AssetBaseRef> _assets;

		private readonly Dictionary<string, AssetBaseRef> _assetsByName = new();
		private readonly Dictionary<string, AssetBaseRef> _assetsByGuid = new();
		private readonly Dictionary<string, List<AssetBaseRef>> _assetsByTags = new();
		private readonly Dictionary<Type, List<AssetBaseRef>> _assetsByType = new();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ReloadDomain() => _instance = null;

		private static AssetBank Instance
		{
			get
			{
				if (_instance == null) Initialize();
				return _instance;
			}
		}

		#region Static API

		/// <summary> Returns all asset references registered in the bank. </summary>
		public static IEnumerable<AssetBaseRef> GetAllAssets() => Instance._assets;

		/// <summary> Returns the asset with the given GUID, or null if not found. </summary>
		public static T GetAssetByGuid<T>(string guid) where T : AssetBase
		{
			if (string.IsNullOrEmpty(guid)) return null;
			if (Instance._assetsByGuid.TryGetValue(guid, out var assetRef) && assetRef.TryLoad(out T asset))
			{
				return asset;
			}

			return null;
		}

		/// <summary> Tries to get the asset with the given GUID. Returns true on success. </summary>
		public static bool TryGetAssetByGuid<T>(string guid, out T asset) where T : AssetBase
		{
			asset = GetAssetByGuid<T>(guid);
			return asset != null;
		}

		/// <summary> Returns the asset with the given GUID asynchronously, or null if not found. </summary>
		public static async UniTask<T> GetAssetByGuidAsync<T>(string guid) where T : AssetBase
		{
			if (string.IsNullOrEmpty(guid)) return null;
			if (Instance._assetsByGuid.TryGetValue(guid, out var assetRef))
			{
				return await assetRef.TryLoadAsync<T>();
			}

			return null;
		}

		/// <summary> Returns the first asset registered with the given name, or null if not found. </summary>
		public static T GetAssetByName<T>(string name) where T : AssetBase
		{
			if (string.IsNullOrEmpty(name)) return null;
			if (Instance._assetsByName.TryGetValue(name, out var assetRef) && assetRef.TryLoad(out T asset))
			{
				return asset;
			}

			return null;
		}

		/// <summary> Tries to get the first asset registered with the given name. Returns true on success. </summary>
		public static bool TryGetAssetByName<T>(string name, out T asset) where T : AssetBase
		{
			asset = GetAssetByName<T>(name);
			return asset != null;
		}

		/// <summary> Returns the first asset registered with the given name asynchronously, or null if not found. </summary>
		public static async UniTask<T> GetAssetByNameAsync<T>(string name) where T : AssetBase
		{
			if (string.IsNullOrEmpty(name)) return null;
			if (Instance._assetsByName.TryGetValue(name, out var assetRef))
			{
				return await assetRef.TryLoadAsync<T>();
			}
			return null;
		}

		/// <summary> Returns the first registered asset of the given type, or null if none. </summary>
		public static T GetAssetOfType<T>() where T : AssetBase
		{
			if (Instance._assetsByType.TryGetValue(typeof(T), out var assetRefs))
			{
				foreach (var assetRef in assetRefs)
				{
					if (assetRef.TryLoad(out T asset))
					{
						return asset;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Returns the first registered asset of the given type asynchronously.
		/// Tries each ref in order and returns the first that loads successfully.
		/// </summary>
		public static async UniTask<T> GetAssetOfTypeAsync<T>() where T : AssetBase
		{
			if (Instance._assetsByType.TryGetValue(typeof(T), out var assetRefs))
			{
				foreach (var assetRef in assetRefs)
				{
					var asset = await assetRef.TryLoadAsync<T>();
					if (asset != null) return asset;
				}
			}

			return null;
		}

		/// <summary> Returns all registered assets of the given type. </summary>
		public static IEnumerable<T> GetAssetsOfType<T>() where T : AssetBase
		{
			if (Instance._assetsByType.TryGetValue(typeof(T), out var assetRefs))
			{
				foreach (var assetRef in assetRefs)
				{
					if (assetRef.TryLoad(out T asset)) yield return asset;
				}
			}
		}

		/// <summary> Returns all registered assets of the given type asynchronously. </summary>
		public static async UniTask<T[]> GetAssetsOfTypeAsync<T>() where T : AssetBase
		{
			if (Instance._assetsByType.TryGetValue(typeof(T), out var assetRefs))
			{
				// Filter out refs that failed to load so the async result matches the sync contract.
				var loaded = await UniTask.WhenAll(assetRefs.Select(r => r.TryLoadAsync<T>()));
				return loaded.Where(asset => asset != null).ToArray();
			}
			return Array.Empty<T>();
		}

		/// <summary> Returns all registered assets carrying the given tag. </summary>
		public static IEnumerable<T> GetAssetsByTag<T>(string tag) where T : AssetBase
		{
			if (string.IsNullOrEmpty(tag)) yield break;
			if (Instance._assetsByTags.TryGetValue(tag, out var assetRefs))
			{
				foreach (var assetRef in assetRefs)
				{
					if (assetRef.TryLoad(out T asset)) yield return asset;
				}
			}
		}

		/// <summary> Returns all registered assets carrying the given tag asynchronously. </summary>
		public static async UniTask<T[]> GetAssetsByTagAsync<T>(string tag) where T : AssetBase
		{
			if (!string.IsNullOrEmpty(tag) && Instance._assetsByTags.TryGetValue(tag, out var assetRefs))
			{
				// Filter out refs that failed to load so the async result matches the sync contract.
				var loaded = await UniTask.WhenAll(assetRefs.Select(r => r.TryLoadAsync<T>()));
				return loaded.Where(asset => asset != null).ToArray();
			}
			return Array.Empty<T>();
		}

		/// <summary>
		/// Unloads all cached assets from memory and clears their internal caches.
		/// Only call when you are certain no other system holds references to these assets.
		/// </summary>
		public static void UnloadAll()
		{
			if (_instance?._assets == null) return;
			foreach (var assetRef in _instance._assets)
			{
				assetRef.Unload();
			}
		}

		/// <summary>
		/// Unloads all cached assets carrying the given tag and clears their internal caches.
		/// Symmetric with <see cref="GetAssetsByTag{T}"/>. Only call when no other system holds
		/// references to these assets.
		/// </summary>
		public static void UnloadAssetsByTag(string tag)
		{
			if (string.IsNullOrEmpty(tag)) return;
			if (Instance._assetsByTags.TryGetValue(tag, out var assetRefs))
			{
				foreach (var assetRef in assetRefs)
				{
					assetRef.Unload();
				}
			}
		}

		#endregion

		#region Lifecycle

		/// <summary>
		/// Loads the AssetBank from Resources and rebuilds all lookup dictionaries.
		/// Called automatically on first access. Can be called manually to force a reload,
		/// but note that <see cref="AssetBaseRef"/> instances already in memory retain their own cache.
		/// </summary>
		public static void Initialize()
		{
			_instance = Resources.Load<AssetBank>(AssetBankResourcePath);
			if (_instance == null)
			{
				Debug.LogWarning("AssetBank not found in Resources. Creating a new instance in memory.");
				_instance = CreateInstance<AssetBank>();
			}

			// A bank created in memory (or with no serialized list) has a null _assets, so guard before indexing.
			_instance._assets ??= new List<AssetBaseRef>();
			_instance.RebuildIndex();
		}

		// Rebuilds all lookup dictionaries from the current _assets list.
		private void RebuildIndex()
		{
			_assetsByName.Clear();
			_assetsByGuid.Clear();
			_assetsByTags.Clear();
			_assetsByType.Clear();

			foreach (var asset in _assets)
			{
				if (!asset.IsValid)
				{
					Debug.LogWarning($"AssetBank: Invalid asset reference. GUID: '{asset.Guid}', Type: '{asset.TypeName}'");
					continue;
				}

				if (!string.IsNullOrWhiteSpace(asset.Name))
				{
					if (_assetsByName.ContainsKey(asset.Name))
					{
						Debug.LogWarning($"AssetBank: Duplicate name '{asset.Name}' for GUID '{asset.Guid}'. Previous entry will be overwritten.");
					}

					_assetsByName[asset.Name] = asset;
				}

				if (_assetsByGuid.ContainsKey(asset.Guid))
				{
					Debug.LogWarning($"AssetBank: Duplicate GUID '{asset.Guid}' for asset '{asset.Name}'. Previous entry will be overwritten.");
				}

				_assetsByGuid[asset.Guid] = asset;

				foreach (string tag in (string[])asset.Tags)
				{
					if (!_assetsByTags.TryGetValue(tag, out var tagList))
					{
						_assetsByTags[tag] = tagList = new List<AssetBaseRef>();
					}

					tagList.Add(asset);
				}

				// Type may be unresolvable if the class was renamed or moved to another assembly.
				// The asset stays reachable by GUID/name/tag, but cannot be indexed by type.
				var type = asset.Type;
				if (type == null)
				{
					Debug.LogWarning($"AssetBank: Could not resolve type '{asset.TypeName}' for asset '{asset.Name}' (GUID '{asset.Guid}'). Skipping type index.");
					continue;
				}

				if (!_assetsByType.TryGetValue(type, out var typeList))
				{
					_assetsByType[type] = typeList = new List<AssetBaseRef>();
				}

				typeList.Add(asset);
			}
		}

		#endregion

		#region IAssetBank (instance bridge for DI)

		IEnumerable<AssetBaseRef> IAssetBank.GetAllAssets() => GetAllAssets();
		T IAssetBank.GetAssetByGuid<T>(string guid) => GetAssetByGuid<T>(guid);
		bool IAssetBank.TryGetAssetByGuid<T>(string guid, out T asset) => TryGetAssetByGuid<T>(guid, out asset);
		UniTask<T> IAssetBank.GetAssetByGuidAsync<T>(string guid) => GetAssetByGuidAsync<T>(guid);
		T IAssetBank.GetAssetByName<T>(string name) => GetAssetByName<T>(name);
		bool IAssetBank.TryGetAssetByName<T>(string name, out T asset) => TryGetAssetByName<T>(name, out asset);
		UniTask<T> IAssetBank.GetAssetByNameAsync<T>(string name) => GetAssetByNameAsync<T>(name);
		T IAssetBank.GetAssetOfType<T>() => GetAssetOfType<T>();
		UniTask<T> IAssetBank.GetAssetOfTypeAsync<T>() => GetAssetOfTypeAsync<T>();
		IEnumerable<T> IAssetBank.GetAssetsOfType<T>() => GetAssetsOfType<T>();
		UniTask<T[]> IAssetBank.GetAssetsOfTypeAsync<T>() => GetAssetsOfTypeAsync<T>();
		IEnumerable<T> IAssetBank.GetAssetsByTag<T>(string tag) => GetAssetsByTag<T>(tag);
		UniTask<T[]> IAssetBank.GetAssetsByTagAsync<T>(string tag) => GetAssetsByTagAsync<T>(tag);

		#endregion

		#region Editor

#if UNITY_EDITOR
		public void Feed(IEnumerable<AssetBase> assets)
		{
			_assets = assets.Select(AssetBaseRef.FromAsset).ToList();
			foreach (var asset in assets)
				asset.OnRegister();
			UnityEditor.EditorUtility.SetDirty(this);
		}

		public static void SelectInProject() => UnityEditor.Selection.activeObject = Instance;

		public static string GetPath() => UnityEditor.AssetDatabase.GetAssetPath(Instance);

		// Test seam: replaces the singleton with an in-memory bank built from the given refs,
		// bypassing Resources so the lookup logic can be exercised in isolation.
		internal static void InitializeForTests(IEnumerable<AssetBaseRef> assets)
		{
			_instance = CreateInstance<AssetBank>();
			_instance._assets = assets?.ToList() ?? new List<AssetBaseRef>();
			_instance.RebuildIndex();
		}

		// Test seam: clears the cached singleton so the next access re-initializes from Resources.
		internal static void ResetForTests() => _instance = null;
#endif

		#endregion
	}
}
