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
		public string BundleKey;

		public bool IsValid => !string.IsNullOrWhiteSpace(Guid) && !string.IsNullOrWhiteSpace(TypeName);

		/// <summary> Whether the referenced asset is currently loaded and cached in memory. </summary>
		public bool IsLoaded => _loadedAsset != null;

		/// <summary>
		/// Number of outstanding references acquired via <see cref="Load{T}"/>/<see cref="LoadAsync{T}"/>
		/// and not yet released via <see cref="Release"/>. Always 0 for assets only ever accessed
		/// through <see cref="TryLoad{T}"/>/<see cref="TryLoadAsync{T}"/> (the uncounted convenience API).
		/// </summary>
		public int RefCount => _refCount;

		private Type _type;
		private AssetBase _loadedAsset;
		private int _refCount;

#if UNITY_ADDRESSABLES
		// Keeps the Addressables handle alive so the asset is not evicted from memory.
		// Released in UnloadPhysical().
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
				Name      = asset.Name,
				Guid      = asset.Guid,
				TypeName  = asset.TypeName,
				Tags      = asset.Tags,
				LoadMode  = asset.LoadMode,
				BundleKey = asset.BundleKey,
			};
		}
#endif

		#region Uncounted API (TryLoad) -- convenience getters, never unloaded automatically

		/// <summary>
		/// Resolves the asset, loading it if necessary. Not reference-counted: the asset stays cached
		/// forever once loaded (nothing here ever unloads it). This backs <see cref="AssetBank"/>'s
		/// Get* convenience methods. Do not mix with <see cref="Load{T}"/>/<see cref="Release"/> on the
		/// same asset -- both share the same underlying cached instance, so releasing the last counted
		/// reference can unload an asset a TryLoad caller still expects to be resident.
		/// </summary>
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

			return LoadFromConfiguredSource(out asset);
		}

		/// <summary> Async counterpart of <see cref="TryLoad{T}"/>. See its remarks for caveats. </summary>
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

			return await LoadFromConfiguredSourceAsync<T>();
		}

		#endregion

		#region Counted API (Load/Release) -- unloaded once every reference has been released

		/// <summary>
		/// Loads the asset and adds one reference. Call <see cref="Release"/> exactly once per
		/// successful <see cref="Load{T}"/>/<see cref="LoadAsync{T}"/> call to let it be unloaded once
		/// nothing else references it. Unlike <see cref="TryLoad{T}"/>, this always goes through the
		/// asset's configured Resources/Addressables path -- even in the Editor -- so it also exercises
		/// (and can be used to verify) the real runtime loading behavior while testing in Play Mode.
		/// Do not mix with <see cref="TryLoad{T}"/> on the same asset -- see its remarks.
		/// </summary>
		public bool Load<T>(out T asset) where T : AssetBase
		{
			if (_loadedAsset is T cachedAsset)
			{
				_refCount++;
				asset = cachedAsset;
				return true;
			}

			// Only count the reference once loading actually succeeds -- otherwise a failed Load would
			// leak a phantom reference that nothing will ever Release (nothing was returned to the
			// caller, so they have nothing to call Release on).
			if (LoadFromConfiguredSource(out asset))
			{
				_refCount++;
				return true;
			}

			return false;
		}

		/// <summary> Async counterpart of <see cref="Load{T}"/>. See its remarks for caveats. </summary>
		public async UniTask<T> LoadAsync<T>() where T : AssetBase
		{
			if (_loadedAsset is T cachedAsset)
			{
				_refCount++;
				return cachedAsset;
			}

			T asset = await LoadFromConfiguredSourceAsync<T>();
			if (asset != null)
			{
				_refCount++;
			}
			return asset;
		}

		/// <summary>
		/// Releases one reference acquired via <see cref="Load{T}"/>/<see cref="LoadAsync{T}"/>. Once
		/// the reference count reaches 0 the asset is unloaded (see <see cref="TryLoad{T}"/>'s remarks
		/// for why this is unsafe to call for references that were only ever obtained via TryLoad).
		/// Calling this more times than Load/LoadAsync succeeded logs a warning and is otherwise a no-op.
		/// </summary>
		public void Release()
		{
			if (_refCount <= 0)
			{
				Debug.LogWarning($"[AssetBank] Release() called on '{Name}' (GUID: {Guid}) with no active reference. Ignoring.");
				return;
			}

			_refCount--;
			if (_refCount == 0)
			{
				UnloadPhysical();
			}
		}

		#endregion

		#region Loading / unloading internals

		// Resources/Addressables loading shared by the uncounted and counted APIs (after the Editor
		// shortcut has already been tried/skipped by the caller).
		private bool LoadFromConfiguredSource<T>(out T asset) where T : AssetBase
		{
			switch (LoadMode)
			{
				case AssetLoadMode.Resources:
					asset = Resources.Load<T>($"{AssetBank.AssetsResourcePath}/{Guid}");
					return CacheAndValidate(asset);

#if UNITY_ADDRESSABLES
				case AssetLoadMode.Addressables:
					// WaitForCompletion blocks the main thread; acceptable for the synchronous API.
					// Prefer the async variant for runtime use to avoid frame hitches.
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

		// Async counterpart of LoadFromConfiguredSource.
		private async UniTask<T> LoadFromConfiguredSourceAsync<T>() where T : AssetBase
		{
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

		// Physically unloads the cached asset and clears the internal cache.
		// For Resources assets, calls Resources.UnloadAsset. For Addressables assets, releases the handle.
		private void UnloadPhysical()
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

		#endregion
	}
}
