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

		public static implicit operator T(AssetRef<T> assetRef) => assetRef.Asset;
	}
}
