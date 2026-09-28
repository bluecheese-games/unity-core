using System;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// A value that may or may not override some external default. Intended for per-instance override
	/// fields sitting alongside a centralized settings asset (e.g. <c>UIButton</c>'s punch/grayscale/SFX
	/// overrides next to <c>ButtonSettings</c>): when <see cref="Enabled"/> is false, callers should fall
	/// back to their own default via <see cref="Resolve"/> instead of using <see cref="Value"/> directly.
	/// </summary>
	[Serializable]
	public struct Optional<T>
	{
		[SerializeField] private bool _enabled;
		[SerializeField] private T _value;

		public Optional(T value, bool enabled = true)
		{
			_enabled = enabled;
			_value = value;
		}

		public bool Enabled { readonly get => _enabled; set => _enabled = value; }

		public T Value { readonly get => _value; set => _value = value; }

		/// <summary>Returns <see cref="Value"/> when <see cref="Enabled"/>, otherwise <paramref name="fallback"/>.</summary>
		public readonly T Resolve(T fallback) => _enabled ? _value : fallback;

		public static implicit operator Optional<T>(T value) => new(value);
	}
}
