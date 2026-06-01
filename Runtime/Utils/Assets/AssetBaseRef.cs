//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

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

		public bool IsValid => !string.IsNullOrWhiteSpace(Guid) && !string.IsNullOrWhiteSpace(TypeName);

		private Type _type;
		private AssetBase _loadedAsset;

#if UNITY_ADDRESSABLES
		// Keeps the Addressables handle alive so the asset is not evicted from memory.
		// Released in Unload().
		private AsyncOperationHandle _addressablesHandle;
#endif

		public Type Type
		{
			get
			{
				if (_type != null) return _type;
				_type = Type.GetType(TypeName);
				return _type;
			}
		}

#if UNITY_EDITOR
		public static AssetBaseRef FromAsset(AssetBase asset)
		{
			return new AssetBaseRef
			{
				Name     = asset.Name,
				Guid     = asset.Guid,
				TypeName = asset.TypeName,
				Tags     = asset.Tags,
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

			// Editor shortcut: load directly from AssetDatabase
			if (TryGetEditorAsset(out asset))
			{
				_loadedAsset = asset;
				return true;
			}

			switch (LoadMode)
			{
				case AssetLoadMode.Resources:
					asset = Resources.Load<T>($"{AssetBank.AssetsResourcePath}/{Guid}");
					return CacheAndValidate(asset);

#if UNITY_ADDRESSABLES
				case AssetLoadMode.Addressables:
					// WaitForCompletion blocks the main thread; acceptable for the synchronous API.
					// Prefer TryLoadAsync for runtime use to avoid frame hitches.
					var handle = Addressables.LoadAssetAsync<T>(Guid);
					handle.WaitForCompletion();
					asset = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
					if (asset != null)
						_addressablesHandle = handle;
					else
						Addressables.Release(handle);
					return CacheAndValidate(asset);
#else
				case AssetLoadMode.Addressables:
					Debug.LogError(
						"[AssetBank] Addressables is not enabled. " +
						"Install com.unity.addressables and add UNITY_ADDRESSABLES to your Scripting Define Symbols.");
					asset = null;
					return false;
#endif

				default:
					asset = null;
					return false;
			}
		}

		public async UniTask<T> TryLoadAsync<T>() where T : AssetBase
		{
			// Return cached asset if already loaded
			if (_loadedAsset is T cachedAsset) return cachedAsset;

			// Editor shortcut: load directly from AssetDatabase
			if (TryGetEditorAsset(out T editorAsset))
			{
				_loadedAsset = editorAsset;
				return editorAsset;
			}

			T asset = null;
			switch (LoadMode)
			{
				case AssetLoadMode.Resources:
					var request = Resources.LoadAsync<T>($"{AssetBank.AssetsResourcePath}/{Guid}");
					await request;
					asset = request.asset as T;
					CacheAndValidate(asset);
					break;

#if UNITY_ADDRESSABLES
				case AssetLoadMode.Addressables:
					var handle = Addressables.LoadAssetAsync<T>(Guid);
					asset = await handle.Task.AsUniTask();
					if (handle.Status == AsyncOperationStatus.Succeeded)
						_addressablesHandle = handle;
					else
						Addressables.Release(handle);
					CacheAndValidate(asset);
					break;
#else
				case AssetLoadMode.Addressables:
					Debug.LogError(
						"[AssetBank] Addressables is not enabled. " +
						"Install com.unity.addressables and add UNITY_ADDRESSABLES to your Scripting Define Symbols.");
					break;
#endif
			}

			return asset;
		}

		/// <summary>
		/// Releases the loaded asset from memory and clears the internal cache.
		/// For Resources assets, calls <see cref="Resources.UnloadAsset"/>.
		/// For Addressables assets, releases the operation handle.
		/// Only call when no other system holds a reference to the asset.
		/// </summary>
		public void Unload()
		{
			if (_loadedAsset == null) return;

			switch (LoadMode)
			{
				case AssetLoadMode.Resources:
					Resources.UnloadAsset(_loadedAsset);
					break;

#if UNITY_ADDRESSABLES
				case AssetLoadMode.Addressables:
					if (_addressablesHandle.IsValid())
						Addressables.Release(_addressablesHandle);
					_addressablesHandle = default;
					break;
#endif
			}

			_loadedAsset = null;
		}

		// Stores the loaded asset and logs an error if loading failed. Returns true when asset is not null.
		private bool CacheAndValidate<T>(T asset) where T : AssetBase
		{
			_loadedAsset = asset;
			if (_loadedAsset == null)
				Debug.LogError($"[AssetBank] Failed to load asset '{Name}' (GUID: {Guid}, Type: {TypeName}, Mode: {LoadMode})");
			return asset != null;
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
