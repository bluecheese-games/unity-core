//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	[Serializable]
	public struct Tags
	{
		[SerializeField] private string[] _values;

		public readonly bool Contains(string value) => Array.IndexOf(_values, value) >= 0;

		public static implicit operator Tags(string[] values) => new() { _values = values };

		public static implicit operator string[](Tags tags) => tags._values;

		public readonly string this[int index]
		{
			get => _values[index];
			set => _values[index] = value;
		}
	}
}
