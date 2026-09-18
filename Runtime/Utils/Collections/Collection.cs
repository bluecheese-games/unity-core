using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	public class Collection<T> : AssetBase, IEnumerable<T>
	{
		[SerializeField, HideInInspector] protected List<T> _items;

		// Lazily initialized: assets created via [CreateAssetMenu] may never have had _items
		// serialized before the first access (e.g. Items on a freshly created, empty asset).
		private List<T> ItemsList => _items ??= new List<T>();

		public ReadOnlyCollection<T> Items => ItemsList.AsReadOnly();

		public int Size => ItemsList.Count;

		public T this[int index] => ItemsList[index];

		/// <summary>
		/// Returns a random item from the collection, or default if empty.
		/// </summary>
		public T GetRandom()
		{
			if (ItemsList.Count == 0)
				return default;

			return ItemsList[UnityEngine.Random.Range(0, ItemsList.Count)];
		}

		/// <summary>
		/// Returns a random item from the collection using the given <see cref="System.Random"/>
		/// instead of <see cref="UnityEngine.Random"/>. Useful for deterministic/testable selection.
		/// </summary>
		public T GetRandom(System.Random random)
		{
			if (ItemsList.Count == 0)
				return default;

			return ItemsList[random.Next(ItemsList.Count)];
		}

		public IEnumerator<T> GetEnumerator() => ItemsList.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#if UNITY_EDITOR
		/// <summary>
		/// Whether items can be added, removed, or reordered in the inspector.
		/// Override to false for collections whose content is managed programmatically.
		/// </summary>
		public virtual bool IsEditable => true;

		/// <summary>
		/// Whether the given item matches the search filter typed in the inspector.
		/// Override to search other fields (e.g. a ScriptableObject's own data) instead of ToString().
		/// </summary>
		public virtual bool SearchFilter(T item, string filter)
		{
			if (string.IsNullOrEmpty(filter))
				return true;

			return (object)item != null && item.ToString().IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
		}
#endif
	}
}
