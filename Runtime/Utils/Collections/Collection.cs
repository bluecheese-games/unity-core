using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	public class Collection<T> : AssetBase
	{
		[SerializeField, HideInInspector] protected List<T> _items;

		public ReadOnlyCollection<T> Items => _items.AsReadOnly();

		public int Size => _items != null ? _items.Count : 0;

		public T this[int index] => _items != null ? _items[index] : default;

		public T GetRandom()
		{
			if (_items == null || _items.Count == 0)
				return default;

			return _items[UnityEngine.Random.Range(0, _items.Count)];
		}

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
