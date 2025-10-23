//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	[Serializable]
	public class AssetBaseRef
	{
		public string Name;
		public string Guid;
		public string TypeName;
		public Tags Tags;
		public AssetLoadMode LoadMode = AssetLoadMode.Resources;

		private Type _type;
		private AssetBase _loadedAsset;

		public Type Type
		{
			get
			{
				if (_type != null)
				{
					return _type;
				}
				_type = Type.GetType(TypeName);
				return _type;
			}
		}

#if UNITY_EDITOR
		public static AssetBaseRef FromAsset(AssetBase asset)
		{
			return new AssetBaseRef
			{
				Name = asset.Name,
				Guid = asset.Guid,
				TypeName = asset.TypeName,
				Tags = asset.Tags,
				LoadMode = asset.LoadMode,
			};
		}
#endif

		public bool TryLoad<T>(out T asset) where T : AssetBase
		{
			// Return cached asset if already loaded
			if (_loadedAsset is T cachedAsset)
			{
				asset = cachedAsset;
				return true;
			}

			// Editor shortcut to load directly from AssetDatabase
			if (TryGetEditorAsset(out asset))
			{
				_loadedAsset = asset;
				return true;
			}

			// Load asset based on LoadMode
			switch (LoadMode)
			{
				case AssetLoadMode.Resources:
					var fullPath = $"{AssetBank.AssetsResourcePath}/{Guid}";
					asset = Resources.Load<T>(fullPath);
					break;
				case AssetLoadMode.Addressables:
					Debug.LogError("Addressables loading not implemented yet.");
					asset = null;
					break;
			}

			// Cache loaded asset
			_loadedAsset = asset;
			if (_loadedAsset == null)
			{
				Debug.LogError($"[AssetBank] Failed to load asset '{Name}' (GUID: {Guid}, Type: {TypeName}, Mode: {LoadMode})");
			}
			return asset != null;
		}

		public async UniTask<T> TryLoadAsync<T>() where T : AssetBase
		{
			// Return cached asset if already loaded
			if (_loadedAsset is T cachedAsset)
			{
				return cachedAsset;
			}

			// Editor shortcut to load directly from AssetDatabase
			if (TryGetEditorAsset(out T editorAsset))
			{
				_loadedAsset = editorAsset;
				return editorAsset;
			}

			// Load asset based on LoadMode
			T asset = null;
			switch (LoadMode)
			{
				case AssetLoadMode.Resources:
					var fullPath = $"{AssetBank.AssetsResourcePath}/{Guid}";
					var resourceRequest = Resources.LoadAsync<T>(fullPath);
					await resourceRequest;
					asset = resourceRequest.asset as T;
					break;
				case AssetLoadMode.Addressables:
					Debug.LogError("Addressables loading not implemented yet.");
					break;
			}

			// Cache loaded asset
			_loadedAsset = asset;
			if (_loadedAsset == null)
			{
				Debug.LogError($"[AssetBank] Failed to load asset '{Name}' (GUID: {Guid}, Type: {TypeName}, Mode: {LoadMode})");
			}
			return asset;
		}

		private bool TryGetEditorAsset<T>(out T asset) where T : AssetBase
		{
#if UNITY_EDITOR
			string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(Guid);
			asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
			return asset != null;
#else
			asset = null;
			return false;
#endif
		}
	}
}
