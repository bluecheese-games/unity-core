//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	[CreateAssetMenu(menuName = "AssetBank", fileName = "AssetBank")]
	public class AssetBank : ScriptableObject
	{
		private static AssetBank _instance;

		[SerializeField] private List<AssetBase> _assets;

		private readonly Dictionary<string, AssetBase> _assetsByName = new();
		private readonly Dictionary<int, AssetBase> _assetsById = new();
		private readonly Dictionary<string, List<AssetBase>> _assetsByTags = new();
		private readonly Dictionary<Type, List<AssetBase>> _assetsByType = new();

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

		static public T GetAssetByName<T>(string name) where T : AssetBase
		{
			if (Instance._assetsByName.TryGetValue(name, out var baseAsset) && baseAsset is T asset)
			{
				return asset;
			}
			return null;
		}

		static public bool TryGetAssetByName<T>(string name, out T asset) where T : AssetBase
		{
			if (Instance._assetsByName.TryGetValue(name, out var baseAsset) && baseAsset is T tAsset)
			{
				asset = tAsset;
				return true;
			}

			asset = null;
			return false;
		}

		static public T GetAssetById<T>(int id) where T : AssetBase
		{
			if (Instance._assetsById.TryGetValue(id, out var baseAsset) && baseAsset is T asset)
			{
				return asset;
			}
			return null;
		}

		static public bool TryGetAssetById<T>(int id, out T asset) where T : AssetBase
		{
			if (Instance._assetsById.TryGetValue(id, out var baseAsset) && baseAsset is T tAsset)
			{
				asset = tAsset;
				return true;
			}

			asset = null;
			return false;
		}

		static public IEnumerable<T> GetAssetsByTag<T>(string tag) where T : AssetBase
		{
			if (Instance._assetsByTags.TryGetValue(tag, out var assets))
			{
				return assets.OfType<T>();
			}

			return Enumerable.Empty<T>();
		}

		static public IEnumerable<T> GetAssetsByType<T>() where T : AssetBase
		{
			if (Instance._assetsByType.TryGetValue(typeof(T), out var assets))
			{
				return assets.OfType<T>();
			}

			return Enumerable.Empty<T>();
		}

		static public void Initialize()
		{
			_instance = Resources.Load<AssetBank>("AssetBank");
			if (_instance == null)
			{
				Debug.LogWarning("AssetBank not found in Resources, creating an empty one");
				_instance = CreateInstance<AssetBank>();
				return;
			}

			foreach (var asset in _instance._assets)
			{
				_instance._assetsByName[asset.Name] = asset;
				_instance._assetsById[asset.Id] = asset;

				for (int i = 0; i < asset.Tags.Length; i++)
				{
					if (!_instance._assetsByTags.ContainsKey(asset.Tags[i]))
					{
						_instance._assetsByTags[asset.Tags[i]] = new();
					}

					_instance._assetsByTags[asset.Tags[i]].Add(asset);
				}

				if (!_instance._assetsByType.ContainsKey(asset.GetType()))
				{
					_instance._assetsByType[asset.GetType()] = new();
				}
			}
		}

#if UNITY_EDITOR
		class AssetBankAssetPostprocessor : UnityEditor.AssetPostprocessor
		{
			private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
			{
				var bank = Resources.Load<AssetBank>("AssetBank");
				if (bank != null)
				{
					bank.Regenerate();
				}
			}
		}

		public void Regenerate()
		{
			if (Application.isPlaying)
			{
				return;
			}

			var sw = System.Diagnostics.Stopwatch.StartNew();
			_assets.Clear();

			_assets.AddRange(UnityEditor.AssetDatabase.FindAssets($"t:{nameof(AssetBase)}")
				.Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
				.Select(UnityEditor.AssetDatabase.LoadAssetAtPath<AssetBase>)
				.Where(asset => asset.RegisterInAssetBank)
				.OrderBy(asset => asset.Name));

			foreach (var asset in _assets)
			{
				asset.OnRegister();
			}

			Debug.Log($"Regenerated AssetBank in {sw.ElapsedMilliseconds}ms");
		}

		public static void Refresh()
		{
			var bank = Resources.Load<AssetBank>("AssetBank");
			if (bank != null)
			{
				bank.Regenerate();
			}
		}
#endif
	}
}
