//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// Provides read access to registered assets. Implemented by <see cref="AssetBank"/>.
	/// Register via the DI container to decouple systems from the static singleton.
	/// </summary>
	public interface IAssetBank
	{
		IEnumerable<AssetBaseRef> GetAllAssets();

		T GetAssetByGuid<T>(string guid) where T : AssetBase;
		bool TryGetAssetByGuid<T>(string guid, out T asset) where T : AssetBase;
		UniTask<T> GetAssetByGuidAsync<T>(string guid) where T : AssetBase;

		T GetAssetByName<T>(string name) where T : AssetBase;
		bool TryGetAssetByName<T>(string name, out T asset) where T : AssetBase;
		UniTask<T> GetAssetByNameAsync<T>(string name) where T : AssetBase;

		T GetAssetOfType<T>() where T : AssetBase;
		UniTask<T> GetAssetOfTypeAsync<T>() where T : AssetBase;
		IEnumerable<T> GetAssetsOfType<T>() where T : AssetBase;
		UniTask<T[]> GetAssetsOfTypeAsync<T>() where T : AssetBase;

		IEnumerable<T> GetAssetsByTag<T>(string tag) where T : AssetBase;
		UniTask<T[]> GetAssetsByTagAsync<T>(string tag) where T : AssetBase;

		/// <summary>
		/// Reference-counted alternative to <see cref="GetAssetByGuid{T}"/>/<see cref="GetAssetByName{T}"/>:
		/// each successful Load must be matched by exactly one Release, which unloads the asset once
		/// nothing else references it. Do not mix with the Get*/TryGet* methods on the same asset --
		/// see <see cref="AssetBaseRef.Load{T}"/> for why.
		/// </summary>
		bool LoadAssetByGuid<T>(string guid, out T asset) where T : AssetBase;
		UniTask<T> LoadAssetByGuidAsync<T>(string guid) where T : AssetBase;
		void ReleaseAsset(string guid);

		bool LoadAssetByName<T>(string name, out T asset) where T : AssetBase;
		UniTask<T> LoadAssetByNameAsync<T>(string name) where T : AssetBase;
		void ReleaseAssetByName(string name);
	}
}
