//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// Runtime recorder for development metrics.
	/// Only records metrics in the Unity Editor.
	/// </summary>
	public static class DevMetricRecorder
	{
		public static void Record(string metricName, double value)
		{
#if UNITY_EDITOR
			Editor.DevMetricRecorder.Record(metricName, value);
#endif
		}
	}
}
