using System;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// How an <see cref="AssetBase"/> is registered in and resolved from the <see cref="AssetBank"/>.
	/// None is deliberately the enum's default (0) value: an uninitialized/unmigrated AssetLoadMode
	/// fails safe by not registering the asset, rather than silently registering it.
	/// </summary>
	[Serializable]
	public enum AssetLoadMode
	{
		/// <summary> Not registered in the AssetBank -- excluded from bank generation and, unless
		/// referenced directly elsewhere, from the build. </summary>
		None,

		/// <summary> Referenced directly by the AssetBank asset (no Resources/Addressables indirection).
		/// Always resident once the bank itself is loaded; suits small, always-needed assets. </summary>
		Local,

		/// <summary> Loaded on demand via Addressables. Requires the Addressables package. </summary>
		Remote,
	}
}
