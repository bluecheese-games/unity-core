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

		public readonly T Asset => AssetBank.GetAssetByGuid<T>(Guid);

		public static implicit operator T (AssetRef<T> assetRef) => assetRef.Asset;
	}
}
