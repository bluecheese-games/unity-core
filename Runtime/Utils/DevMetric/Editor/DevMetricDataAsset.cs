//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	/// <summary>
	/// ScriptableObject asset that holds recorded development metrics.
	/// </summary>
	[CreateAssetMenu(fileName = "DevMetricData", menuName = "DevMetric/Data Asset", order = 0)]
	public class DevMetricDataAsset : ScriptableObject
	{
		[Header("Project / User metadata")]
		public string projectName;   // e.g., Application.productName
		public string userName;      // Unity Cloud display name if logged in, else Environment.UserName

		[Serializable]
		public class MetricAggregate
		{
			public string name;
			public double sumSeconds;
			public int count;
			public double AverageSeconds => count > 0 ? sumSeconds / count : 0.0;
		}

		[Serializable]
		public class DayRecord
		{
			public string isoDate; // "yyyy-MM-dd" (UTC)
			public List<MetricAggregate> metrics = new List<MetricAggregate>();

			public MetricAggregate GetOrCreate(string metricName)
			{
				for (int i = 0; i < metrics.Count; i++)
					if (metrics[i].name == metricName) return metrics[i];

				var m = new MetricAggregate { name = metricName, sumSeconds = 0, count = 0 };
				metrics.Add(m);
				return m;
			}
		}

		[SerializeField] private int maxDaysToKeep = 180;
		[SerializeField] private List<DayRecord> days = new List<DayRecord>();

		string TodayIsoUtc => DateTime.UtcNow.ToString("yyyy-MM-dd");

		public void AddSample(string metricName, double valueSeconds)
		{
			var day = GetOrCreateDay(TodayIsoUtc);
			var agg = day.GetOrCreate(metricName);
			agg.sumSeconds += valueSeconds;
			agg.count += 1;
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
			TrimOldDays();
		}

		public IReadOnlyList<DayRecord> Days => days;

		DayRecord GetOrCreateDay(string isoDate)
		{
			for (int i = 0; i < days.Count; i++)
				if (days[i].isoDate == isoDate) return days[i];

			var d = new DayRecord { isoDate = isoDate };
			days.Add(d);
			return d;
		}

		void TrimOldDays()
		{
			if (maxDaysToKeep <= 0 || days.Count <= maxDaysToKeep) return;
			days.Sort((a, b) => string.CompareOrdinal(a.isoDate, b.isoDate));
			while (days.Count > maxDaysToKeep) days.RemoveAt(0);
		}
	}
}