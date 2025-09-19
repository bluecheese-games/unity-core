//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	[Serializable]
	public struct FlagEnum<T> where T : Enum
	{
		[SerializeField]
		private long _value;

		private long _lastToStringValue;
		private string _stringValue;

		public FlagEnum(long value)
		{
			_value = value;
			_lastToStringValue = 0;
			_stringValue = string.Empty;
		}

		public FlagEnum(params T[] values)
		{
			_value = 0;
			_lastToStringValue = 0;
			_stringValue = string.Empty;
			foreach (var value in values)
			{
				AddFlag(value);
			}
		}

		public readonly bool HasFlag(T flag) => (_value & (GetFlagValue(flag))) != 0;

		public void AddFlag(T flag) => _value |= GetFlagValue(flag);

		public void RemoveFlag(T flag) => _value &= ~(GetFlagValue(flag));

		public static implicit operator FlagEnum<T>(T value) => new(GetFlagValue(value));

		public static FlagEnum<T> operator |(FlagEnum<T> a, FlagEnum<T> b) => new(a._value | b._value);

		public static FlagEnum<T> operator |(FlagEnum<T> a, T b) => new(a._value | GetFlagValue(b));

		public static FlagEnum<T> operator |(T a, FlagEnum<T> b) => new(GetFlagValue(a) | b._value);

		public static FlagEnum<T> operator &(FlagEnum<T> a, FlagEnum<T> b) => new(a._value & b._value);

		public static FlagEnum<T> operator &(FlagEnum<T> a, T b) => new(a._value & GetFlagValue(b));

		public static FlagEnum<T> operator &(T a, FlagEnum<T> b) => new(GetFlagValue(a) & b._value);

		public static FlagEnum<T> operator ^(FlagEnum<T> a, FlagEnum<T> b) => new(a._value ^ b._value);

		public static FlagEnum<T> operator ~(FlagEnum<T> a) => new(~a._value);

		public static bool operator ==(FlagEnum<T> a, FlagEnum<T> b) => a._value == b._value;
		public static bool operator !=(FlagEnum<T> a, FlagEnum<T> b) => a._value != b._value;

		public override readonly bool Equals(object obj) => obj is FlagEnum<T> other && _value == other._value;

		public override readonly int GetHashCode() => _value.GetHashCode();

		public readonly bool IsEmpty => _value == 0;

		public readonly int Count
		{
			get
			{
				long v = _value;
				int count = 0;
				while (v != 0)
				{
					count += (int)(v & 1);
					v >>= 1;
				}
				return count;
			}
		}

		private static long GetFlagValue(T flag)
		{
			long val = Convert.ToInt64(flag);
			if (val < 0 || val > 63)
				throw new ArgumentOutOfRangeException($"{typeof(T)}.{flag} out of bounds for FlagEnum!");
			return 1L << (int)val;
		}

		public override string ToString()
		{
			if (IsEmpty)
			{
				return "None";
			}

			if (_lastToStringValue == _value)
			{
				// Returns the cached value
				return _stringValue;
			}

			_lastToStringValue = _value;
			_stringValue = string.Join(", ", Enum.GetValues(typeof(T))
				.Cast<T>()
				.Where(HasFlag)
				.Select(x => x.ToString()));

			return _stringValue;
		}

		public readonly string ToBinaryString(bool includeLeadingZeros = false)
		{
			string result = Convert.ToString(_value, 2);
			if (!includeLeadingZeros)
			{
				return result;
			}
			return result.PadLeft(sizeof(long) * 8, '0');
		}

		public readonly IEnumerable<T> GetFlags() =>
			Enum.GetValues(typeof(T))
				.Cast<T>()
				.Where(HasFlag);

		public readonly IList<T> ToList() => GetFlags().ToList();

		public readonly T[] ToArray() => GetFlags().ToArray();

		public readonly T FirstOrDefault() => GetFlags().FirstOrDefault();
	}
}
