using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	[Serializable]
	public struct Tags
	{
		[SerializeField] private string[] _values;

		public readonly int Count => _values?.Length ?? 0;

		public readonly bool Contains(string value) =>
			_values != null && Array.IndexOf(_values, value) >= 0;

		public void Combine(Tags tags)
		{
			HashSet<string> values = _values != null ? new(_values) : new();
			if (tags._values != null)
				values.UnionWith(tags._values);
			_values = values.ToArray();
		}

		public static implicit operator Tags(string[] values) => new() { _values = values };

		public static implicit operator string[](Tags tags) => tags._values ?? Array.Empty<string>();

		public readonly string this[int index] => _values[index];

		public override readonly string ToString()
		{
			if (_values == null || _values.Length == 0)
			{
				return "None";
			}
			return string.Join(", ", _values);
		}
	}
}
