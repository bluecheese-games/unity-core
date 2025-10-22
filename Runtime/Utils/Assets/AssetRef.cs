//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Core.Utils
{
	[Serializable]
	public struct AssetRef<T> where T : AssetBase
	{
        public string Guid;

		public readonly T Asset
		{
			get
			{
				AssetBank.TryGetAssetByGuid<T>(Guid, out var asset);
				return asset;
			}
		}

		public static implicit operator T (AssetRef<T> assetRef) => assetRef.Asset;
	}
}
