using System;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// Serializable reference to a single item picked from a <see cref="Collection{T}"/> asset.
	/// Both the source collection and the picked item are stored as direct references (no
	/// Addressables/AssetBank indirection) -- edited via a custom inspector picker.
	/// See <see cref="CollectionItemsRef{T}"/> for the multi-item counterpart.
	/// </summary>
	[Serializable]
	public struct CollectionItemRef<T> where T : UnityEngine.Object
	{
		[SerializeField] private Collection<T> _collection;
		[SerializeField] private T _item;

		/// <summary> The collection the item was picked from. </summary>
		public readonly Collection<T> Source => _collection;

		/// <summary> The picked item, or null if none was picked. </summary>
		public readonly T Item => _item;

		/// <summary> Whether an item has been picked. </summary>
		public readonly bool HasItem => _item != null;

		public static implicit operator T(CollectionItemRef<T> itemRef) => itemRef._item;
	}
}
