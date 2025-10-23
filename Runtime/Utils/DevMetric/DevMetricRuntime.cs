//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// Runtime recorder for development metrics.
	/// Only records metrics in the Unity Editor.
	/// </summary>
	public static class DevMetricRuntime
	{
		public static event Action<string, double> OnMetricRecorded;

		public static void Record(string metricName, double value)
		{
#if UNITY_EDITOR
			OnMetricRecorded?.Invoke(metricName, value);
#endif
		}
	}
}
