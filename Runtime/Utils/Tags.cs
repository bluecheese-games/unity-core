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
	}
}
