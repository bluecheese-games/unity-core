//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	[CreateAssetMenu(menuName = "AssetBank", fileName = "AssetBank")]
	public class AssetBank : ScriptableObject
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
		private static void ReloadDomain()
		{
			_instance = null;
		}

		private static AssetBank Instance
		{
			get
			{
				if (_instance == null)
				{
					Initialize();
				}

				return _instance;
			}
		}

		static public IEnumerable<AssetBaseRef> GetAllAssets() => Instance._assets;

		static public T GetAssetByGuid<T>(string guid) where T : AssetBase
		{
			if (Instance._assetsByGuid.TryGetValue(guid, out var assetBaseRef))
			{
				if (assetBaseRef.TryLoad(out T asset))
				{
					return asset;
				}
			}
			return null;
		}

		static public bool TryGetAssetByGuid<T>(string guid, out T asset) where T : AssetBase
		{
			asset = GetAssetByGuid<T>(guid);
			return asset != null;
		}

		static public async UniTask<T> TryGetAssetByGuid<T>(string guid) where T : AssetBase
		{
			if (Instance._assetsByGuid.TryGetValue(guid, out var assetBaseRef))
			{
				return await assetBaseRef.TryLoadAsync<T>();
			}
			return null;
		}

		static public IEnumerable<T> GetAssetsByType<T>() where T : AssetBase
		{
			var type = typeof(T);
			if (Instance._assetsByType.TryGetValue(type, out var assetRefs))
			{
				foreach (var assetRef in assetRefs)
				{
					if (assetRef.TryLoad(out T asset))
					{
						yield return asset;
					}
				}
			}
		}

		/// <summary>
		/// Returns all assets of the specified type asynchronously.
		/// Using UniTask.WhenAll
		/// </summary>
		static public async UniTask<T[]> GetAssetsByTypeAsync<T>() where T : AssetBase
		{
			var type = typeof(T);
			if (Instance._assetsByType.TryGetValue(type, out var assetRefs))
			{
				return await UniTask.WhenAll(assetRefs.Select(assetRef => assetRef.TryLoadAsync<T>()));
			}
			return Array.Empty<T>();
		}

		static public void Initialize()
		{
			_instance = Resources.Load<AssetBank>(AssetBankResourcePath);
			if (_instance == null)
			{
				Debug.LogWarning("AssetBank not found in Resources, creating an empty one");
				_instance = CreateInstance<AssetBank>();
				return;
			}

			_instance._assetsByName.Clear();
			_instance._assetsByGuid.Clear();
			_instance._assetsByTags.Clear();
			_instance._assetsByType.Clear();

			foreach (var asset in _instance._assets)
			{
				_instance._assetsByName[asset.Name] = asset;
				_instance._assetsByGuid[asset.Guid] = asset;

				for (int i = 0; i < asset.Tags.Length; i++)
				{
					if (!_instance._assetsByTags.ContainsKey(asset.Tags[i]))
					{
						_instance._assetsByTags[asset.Tags[i]] = new();
					}
					_instance._assetsByTags[asset.Tags[i]].Add(asset);
				}

				if (!_instance._assetsByType.ContainsKey(asset.Type))
				{
					_instance._assetsByType[asset.Type] = new();
				}
				_instance._assetsByType[asset.Type].Add(asset);
			}
		}

#if UNITY_EDITOR
		public void Feed(IEnumerable<AssetBase> assets)
		{
			_assets = assets.Select(AssetBaseRef.FromAsset).ToList();

			foreach (var asset in assets)
			{
				asset.OnRegister();
			}
		}

		static public void SelectInProject()
		{
			UnityEditor.Selection.activeObject = Instance;
		}

		static public string GetPath()
		{
			return UnityEditor.AssetDatabase.GetAssetPath(Instance);
		}
#endif
	}
}
