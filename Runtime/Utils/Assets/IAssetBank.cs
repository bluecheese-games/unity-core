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

		T GetAssetOfType<T>() where T : AssetBase;
		UniTask<T> GetAssetOfTypeAsync<T>() where T : AssetBase;
		IEnumerable<T> GetAssetsOfType<T>() where T : AssetBase;
		UniTask<T[]> GetAssetsOfTypeAsync<T>() where T : AssetBase;

		IEnumerable<T> GetAssetsByTag<T>(string tag) where T : AssetBase;
		UniTask<T[]> GetAssetsByTagAsync<T>(string tag) where T : AssetBase;
	}
}
