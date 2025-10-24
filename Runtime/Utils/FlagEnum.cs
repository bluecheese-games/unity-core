//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// Bit-flag helper that maps enum values (their underlying integral values) to bit positions in a 64-bit mask.
	/// Supports sparse enums as long as values are in the range [0, 63].
	/// </summary>
	[Serializable]
	public struct FlagEnum<T> : IEquatable<FlagEnum<T>>, IEnumerable<T> where T : Enum
	{
		[SerializeField]
		private long _bitValue;

		// Precompute the mask of all known enum bits for this T
		private static readonly long KnownMask;
		private static readonly T[] AllValues;

		static FlagEnum()
		{
			var values = Enum.GetValues(typeof(T)).Cast<object>().ToArray();
			AllValues = new T[values.Length];
			long mask = 0;
			for (int i = 0; i < values.Length; i++)
			{
				T v = (T)values[i];
				AllValues[i] = v;
				long idx = Convert.ToInt64(v);
				if (idx >= 0 && idx <= 63) mask |= 1L << (int)idx;
			}
			KnownMask = mask;
		}

		public FlagEnum(params T[] flags)
		{
			_bitValue = 0;
			_cachedForValue = 0;
			_cachedString = null;
			if (flags != null)
			{
				foreach (var f in flags) AddFlag(f);
			}
		}

		public FlagEnum(long bitValue)
		{
			_bitValue = bitValue & KnownMask;
			_cachedForValue = 0;
			_cachedString = null;
		}

		/// <summary>The current 64-bit mask (masked to known enum bits).</summary>
		public long Value
		{
			readonly get => _bitValue & KnownMask;
			set => _bitValue = value & KnownMask;
		}

		public static long GetFlagValue(T flag)
		{
			long idx = Convert.ToInt64(flag);
			if (idx < 0 || idx > 63)
				throw new ArgumentOutOfRangeException(nameof(flag), "Enum values used with FlagEnum<T> must be between 0 and 63 inclusive.");
			return 1L << (int)idx;
		}

		public void AddFlag(T flag) => _bitValue |= GetFlagValue(flag);
		public void RemoveFlag(T flag) => _bitValue &= ~GetFlagValue(flag);
		public void ToggleFlag(T flag) => _bitValue ^= GetFlagValue(flag);
		public readonly bool HasFlag(T flag) => (Value & GetFlagValue(flag)) != 0;

		public void Clear() => _bitValue = 0;
		public void SetAll(IEnumerable<T> flags)
		{
			_bitValue = 0;
			if (flags == null) return;
			foreach (var f in flags) AddFlag(f);
		}

		// Bitwise operators
		public static FlagEnum<T> operator ~(FlagEnum<T> a) => new(~a.Value & KnownMask);

		// OR
		public static FlagEnum<T> operator |(FlagEnum<T> a, FlagEnum<T> b) => new(a.Value | b.Value);
		public static FlagEnum<T> operator |(FlagEnum<T> a, T b) => new(a.Value | GetFlagValue(b));
		public static FlagEnum<T> operator |(T a, FlagEnum<T> b) => new(GetFlagValue(a) | b.Value);

		// AND
		public static FlagEnum<T> operator &(FlagEnum<T> a, FlagEnum<T> b) => new(a.Value & b.Value);
		public static FlagEnum<T> operator &(FlagEnum<T> a, T b) => new(a.Value & GetFlagValue(b));
		public static FlagEnum<T> operator &(T a, FlagEnum<T> b) => new(GetFlagValue(a) & b.Value);

		// XOR
		public static FlagEnum<T> operator ^(FlagEnum<T> a, FlagEnum<T> b) => new(a.Value ^ b.Value);
		public static FlagEnum<T> operator ^(FlagEnum<T> a, T b) => new(a.Value ^ GetFlagValue(b));
		public static FlagEnum<T> operator ^(T a, FlagEnum<T> b) => new(GetFlagValue(a) ^ b.Value);

		// Equality
		public static bool operator ==(FlagEnum<T> a, FlagEnum<T> b) => a.Value == b.Value;
		public static bool operator !=(FlagEnum<T> a, FlagEnum<T> b) => a.Value != b.Value;
		public static bool operator ==(FlagEnum<T> a, T b) => a.Value == GetFlagValue(b);
		public static bool operator !=(FlagEnum<T> a, T b) => a.Value != GetFlagValue(b);
		public static bool operator ==(T a, FlagEnum<T> b) => GetFlagValue(a) == b.Value;
		public static bool operator !=(T a, FlagEnum<T> b) => GetFlagValue(a) != b.Value;
		public override bool Equals(object obj) => obj is FlagEnum<T> other && Equals(other);
		public bool Equals(FlagEnum<T> other) => Value == other.Value;
		public override int GetHashCode() => Value.GetHashCode();

		// Conversions
		public static implicit operator FlagEnum<T>(T flag) => new(GetFlagValue(flag)); // T -> FlagEnum<T>
		public static explicit operator long(FlagEnum<T> f) => f.Value;

		private string _cachedString;
		private long _cachedForValue;
		public override string ToString()
		{
			long v = Value;
			if (_cachedString != null && _cachedForValue == v) return _cachedString;

			if (v == 0)
			{
				_cachedForValue = 0;
				_cachedString = "None";
				return _cachedString;
			}

			var names = new List<string>(AllValues.Length);
			foreach (var ev in AllValues)
			{
				if (HasFlag(ev)) names.Add(ev.ToString());
			}

			_cachedForValue = v;
			_cachedString = names.Count == 0 ? "None" : string.Join(", ", names);
			return _cachedString;
		}

		/// <summary>Returns the underlying mask as a 64-bit binary string.</summary>
		public readonly string ToBinaryString()
		{
			ulong u = unchecked((ulong)Value);
			string s = Convert.ToString((long)u, 2); // Convert.ToString(ulong,2) may not exist on older runtimes
			if (s.StartsWith("-"))
			{
				// Fallback: format manually to 64 bits when negative
				return Convert.ToString((long)Value & int.MaxValue, 2).PadLeft(64, '0');
			}
			return s.PadLeft(64, '0');
		}

		public readonly IEnumerable<T> GetFlags() =>
			AllValues.Where(HasFlag);

		public readonly IList<T> ToList() => GetFlags().ToList();

		public readonly T[] ToArray() => GetFlags().ToArray();

		public readonly T FirstOrDefault() => GetFlags().FirstOrDefault();

		// IEnumerable<T> to allow foreach
		public readonly IEnumerator<T> GetEnumerator() => GetFlags().GetEnumerator();
		readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
