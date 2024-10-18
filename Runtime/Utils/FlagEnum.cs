//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core
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

		public readonly bool HasFlag(T flag)
		{
			return (_value & (GetFlagValue(flag))) != 0;
		}

		public void AddFlag(T flag)
		{
			_value |= GetFlagValue(flag);
		}

		public void RemoveFlag(T flag)
		{
			_value &= ~(GetFlagValue(flag));
		}

		public static implicit operator FlagEnum<T>(T value) => new(GetFlagValue(value));

		private static long GetFlagValue(T flag) => 1L << Convert.ToInt32(flag);

		public override string ToString()
		{
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
	}
}
