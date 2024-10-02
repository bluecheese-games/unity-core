using System;
using UnityEngine;

namespace BlueCheese.Core
{
	[Serializable]
	public struct FlagEnum<T> where T : Enum
	{
		[SerializeField]
		private int _value;

		public FlagEnum(int value)
		{
			_value = value;
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

		private static int GetFlagValue(T flag) => 1 << Convert.ToInt32(flag);
	}
}
