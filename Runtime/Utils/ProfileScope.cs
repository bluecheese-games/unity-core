//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Diagnostics;
using UnityEngine.Profiling;

namespace BlueCheese.Core.Utils
{
	public readonly struct ProfileScope : IDisposable
	{
		/// <summary>
		/// Begins a new profiling scope with the specified name.
		/// If no name is specified, the name of the calling method will be used.
		/// </summary>
		public ProfileScope(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				var frame = new StackFrame(1);
				name = $"{frame.GetMethod().DeclaringType?.Name}.{frame.GetMethod().Name}";
			}
			Profiler.BeginSample(name);
		}

		public readonly void Dispose() => Profiler.EndSample();
	}
}
