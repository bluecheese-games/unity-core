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

		private T _loadedAsset;

		public T Asset
		{
			get
			{
				if (_loadedAsset == null)
				{
					AssetBank.TryLoadAssetByGuid(Guid, out _loadedAsset);
				}
				return _loadedAsset;
			}
		}

		public static implicit operator T (AssetRef<T> assetRef) => assetRef.Asset;
	}
}
