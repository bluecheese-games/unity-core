using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// Serializable reference to zero or more items picked from a <see cref="Collection{T}"/> asset.
	/// Both the source collection and the picked items are stored as direct references (no
	/// Addressables/AssetBank indirection) -- edited via a custom inspector picker.
	/// See <see cref="CollectionItemRef{T}"/> for the single-item counterpart.
	/// </summary>
	[Serializable]
	public struct CollectionItemsRef<T> : IEnumerable<T> where T : UnityEngine.Object
	{
		[SerializeField] private Collection<T> _collection;
		[SerializeField] private T[] _items;

		/// <summary> The collection the items were picked from. </summary>
		public readonly Collection<T> Source => _collection;

		/// <summary> The picked items. </summary>
		public readonly ReadOnlyCollection<T> Items => new(_items ?? Array.Empty<T>());

		/// <summary> Number of picked items. </summary>
		public readonly int Size => _items?.Length ?? 0;

		public readonly T this[int index] => _items[index];

		public readonly bool Contains(T item) => _items != null && Array.IndexOf(_items, item) >= 0;

		/// <summary> Returns a random item among the picked ones, or default if none were picked. </summary>
		public readonly T GetRandom() => Size > 0 ? _items[UnityEngine.Random.Range(0, Size)] : default;

		public readonly IEnumerator<T> GetEnumerator() =>
			((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

		readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
