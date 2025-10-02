//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

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
	}
}
