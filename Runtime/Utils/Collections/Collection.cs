//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	public class Collection<T> : AssetBase
	{
		[SerializeField, HideInInspector] protected T[] _items;

		public ReadOnlySpan<T> Items => _items;

		public int Size => _items != null ? _items.Length : 0;

		public T this[int index] => _items != null ? _items[index] : default;

		public T GetRandom()
		{
			if (_items == null || _items.Length == 0)
				return default;

			return _items[UnityEngine.Random.Range(0, _items.Length)];
		}
	}
}
