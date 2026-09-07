//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System;

namespace BlueCheese.Core.Utils
{
	[Serializable]
	public struct AssetRef<T> where T : AssetBase
	{
		public string Guid;

		/// <summary>
		/// Resolves the referenced asset synchronously. The first access loads from the configured
		/// source; subsequent accesses return the cached instance. Returns null if the GUID is empty.
		/// Avoid on heavy assets in performance-critical paths; prefer <see cref="AssetAsync"/> instead.
		/// </summary>
		public readonly T Asset => string.IsNullOrEmpty(Guid) ? null : AssetBank.GetAssetByGuid<T>(Guid);

		/// <summary>
		/// Resolves the referenced asset asynchronously. Returns null if the GUID is empty.
		/// </summary>
		public readonly UniTask<T> AssetAsync => string.IsNullOrEmpty(Guid)
			? UniTask.FromResult<T>(null)
			: AssetBank.GetAssetByGuidAsync<T>(Guid);

		/// <summary>
		/// Loads the referenced asset and adds one reference to it -- the reference-counted
		/// alternative to <see cref="Asset"/>. Call <see cref="Release"/> exactly once per successful
		/// call (typically from OnDisable/OnDestroy) to let it be unloaded once nothing else
		/// references it. Do not mix with <see cref="Asset"/>/<see cref="AssetAsync"/> on the same
		/// field -- see <see cref="AssetBaseRef.Load{T}"/> for why. Typical usage:
		/// <code>
		/// [SerializeField] private AssetRef&lt;MyAsset&gt; _assetRef;
		/// private MyAsset _asset;
		///
		/// private void OnEnable() => _assetRef.Load(out _asset);
		/// private void OnDestroy() => _assetRef.Release();
		/// </code>
		/// </summary>
		public readonly bool Load(out T asset)
		{
			asset = null;
			return !string.IsNullOrEmpty(Guid) && AssetBank.LoadAssetByGuid(Guid, out asset);
		}

		/// <summary> Async counterpart of <see cref="Load"/>. </summary>
		public readonly UniTask<T> LoadAsync() => string.IsNullOrEmpty(Guid)
			? UniTask.FromResult<T>(null)
			: AssetBank.LoadAssetByGuidAsync<T>(Guid);

		/// <summary> Releases one reference acquired via <see cref="Load"/>/<see cref="LoadAsync"/>. </summary>
		public readonly void Release()
		{
			if (!string.IsNullOrEmpty(Guid))
				AssetBank.ReleaseAsset(Guid);
		}

		public static implicit operator T(AssetRef<T> assetRef) => assetRef.Asset;
	}
}
