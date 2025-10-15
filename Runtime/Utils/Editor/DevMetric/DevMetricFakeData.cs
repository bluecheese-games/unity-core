//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	public static class DevMetricFakeData
	{
		private const string LocalAssetPath = "Assets/_Local/DevMetricData.asset";
		private const string ImportsFolder = "Assets/_Local/Imports";

		// ---------- MENU ----------

		[MenuItem("Tools/DevMetric/Test/Generate Local Fake Data (30 days, blanks)", priority = 1000)]
		public static void GenerateLocal30Days()
		{
			var asset = EnsureLocalAsset();
			if (asset == null) return;

			// 30 days back, seed, label, weekly bumps, small upward trend, 15% blank days
			GenerateFakeDataIntoAsset(asset, daysBack: 30, seed: 1234, label: "(Local)",
									  weeklyBumps: true, trend: 0.01, blankDayChance: 0.15);
			SaveAsset(asset, LocalAssetPath);
			EditorUtility.DisplayDialog("DevMetric",
				"Local fake data generated (30 days, with blank days). Open Tools ▸ DevMetric ▸ Viewer.",
				"OK");
		}

		[MenuItem("Tools/DevMetric/Test/Generate 2 Imported Fake Datasets (blanks)", priority = 1001)]
		public static void GenerateImportedDatasets()
		{
			EnsureFolders();

			var a = ScriptableObject.CreateInstance<DevMetricDataAsset>();
			a.projectName = "Fake Project A";
			a.userName = "Alice";
			GenerateFakeDataIntoAsset(a, daysBack: 45, seed: 2021, label: "(Alice-A)",
									  weeklyBumps: true, trend: 0.00, blankDayChance: 0.20);
			var pathA = AssetDatabase.GenerateUniqueAssetPath($"{ImportsFolder}/Fake_Alice.asset");
			SaveAsset(a, pathA);

			var b = ScriptableObject.CreateInstance<DevMetricDataAsset>();
			b.projectName = "Fake Project B";
			b.userName = "Bob";
			GenerateFakeDataIntoAsset(b, daysBack: 60, seed: 2022, label: "(Bob-B)",
									  weeklyBumps: false, trend: +0.02, blankDayChance: 0.10);
			var pathB = AssetDatabase.GenerateUniqueAssetPath($"{ImportsFolder}/Fake_Bob.asset");
			SaveAsset(b, pathB);

			EditorUtility.DisplayDialog("DevMetric",
				$"Imported fake datasets created:\n• {pathA}\n• {pathB}\nOpen the viewer and Add Source (Asset).",
				"OK");
		}

		[MenuItem("Tools/DevMetric/Test/Clear Local Data", priority = 1100)]
		public static void ClearLocal()
		{
			var asset = AssetDatabase.LoadAssetAtPath<DevMetricDataAsset>(LocalAssetPath);
			if (asset == null)
			{
				EditorUtility.DisplayDialog("DevMetric", "Local asset not found.", "OK");
				return;
			}

			var dayType = typeof(DevMetricDataAsset).GetNestedType("DayRecord");
			var daysField = typeof(DevMetricDataAsset).GetField("days",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			if (dayType != null && daysField != null)
			{
				// assign a correctly typed empty List<DayRecord>
				daysField.SetValue(asset, CreateListOf(dayType));
			}

			EditorUtility.SetDirty(asset);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("DevMetric", "Local DevMetric data cleared.", "OK");
		}

		// ---------- CORE GENERATOR ----------

		/// <param name="blankDayChance">0..1 probability to SKIP the entire day (no DayRecord written)</param>
		private static void GenerateFakeDataIntoAsset(
			DevMetricDataAsset asset,
			int daysBack,
			int seed,
			string label,
			bool weeklyBumps = false,
			double trend = 0.0,
			double blankDayChance = 0.15)
		{
			var dayType = typeof(DevMetricDataAsset).GetNestedType("DayRecord");
			var metricType = typeof(DevMetricDataAsset).GetNestedType("MetricAggregate");
			var daysField = typeof(DevMetricDataAsset).GetField("days",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			if (dayType == null || metricType == null || daysField == null)
			{
				Debug.LogError("DevMetricFakeData: Could not access internal fields/types. Did DevMetricDataAsset change?");
				return;
			}

			// Get or create a correctly typed List<DayRecord>
			var daysList = daysField.GetValue(asset) as IList;
			if (daysList == null)
			{
				daysList = CreateListOf(dayType); // List<DayRecord>
				daysField.SetValue(asset, daysList);
			}

			// Ensure metadata
			if (string.IsNullOrEmpty(asset.projectName)) asset.projectName = $"Fake Project {label}".Trim();
			if (string.IsNullOrEmpty(asset.userName)) asset.userName = $"Fake User {label}".Trim();

			// Metric keys (match your recorder)
			var M_BOOT = "ProjectBootTime_s";
			var M_COMP = "CompilationTime_s";
			var M_PLAY_D = "EnterPlayMode_DomainReload_s";
			var M_PLAY_N = "EnterPlayMode_NoDomainReload_s";
			var M_CUSTOM = "CustomAssetPipeline_s"; // extra to test tabs & search

			var rnd = new System.Random(seed);

			// Base levels (seconds)
			double bootBase = 9.0 + rnd.NextDouble() * 3.0;     // ~9-12s
			double compBase = 4.0 + rnd.NextDouble() * 2.0;     // ~4-6s
			double playNBase = 1.2 + rnd.NextDouble() * 0.6;     // ~1.2-1.8s
			double playDBase = 3.2 + rnd.NextDouble() * 1.0;     // ~3.2-4.2s
			double customBase = 2.0 + rnd.NextDouble() * 1.0;     // ~2-3s

			// Random-walk helpers
			double boot = bootBase, comp = compBase, playN = playNBase, playD = playDBase, custom = customBase;

			for (int i = daysBack; i >= 0; i--)
			{
				var day = DateTime.UtcNow.Date.AddDays(-i);
				string iso = day.ToString("yyyy-MM-dd");

				// BLANK DAY? skip entirely (no DayRecord at all)
				if (rnd.NextDouble() < Mathf.Clamp01((float)blankDayChance))
					continue;

				// Optional weekly pattern (e.g., slower on Mondays)
				double weekly = 1.0;
				if (weeklyBumps)
				{
					var dow = day.DayOfWeek;
					if (dow == DayOfWeek.Monday) weekly = 1.10;
					else if (dow == DayOfWeek.Friday) weekly = 1.05;
					else if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) weekly = 0.95;
				}

				// Random walk + noise + optional trend
				boot = Jitter(boot * (1.0 + trend * 0.5), 0.20, rnd) * weekly;
				comp = Jitter(comp * (1.0 + trend), 0.15, rnd) * weekly;
				playN = Jitter(playN * (1.0 + trend * 0.2), 0.10, rnd) * weekly;
				playD = Jitter(playD * (1.0 + trend * 0.3), 0.12, rnd) * weekly;
				custom = Jitter(custom * (1.0 + trend * 0.1), 0.18, rnd) * weekly;

				// Vary counts (how many measurements that day)
				int cBoot = rnd.Next(1, 3);
				int cComp = rnd.Next(3, 9);
				int cN = rnd.Next(2, 6);
				int cD = rnd.Next(1, 4);
				int cCus = rnd.Next(1, 5);

				// Build the day record
				var dayObj = Activator.CreateInstance(dayType);
				dayType.GetField("isoDate").SetValue(dayObj, iso);

				// Correctly typed List<MetricAggregate>
				var metricsList = CreateListOf(metricType);
				metricsList.Add(MakeMetric(metricType, M_BOOT, boot * cBoot, cBoot));
				metricsList.Add(MakeMetric(metricType, M_COMP, comp * cComp, cComp));
				metricsList.Add(MakeMetric(metricType, M_PLAY_N, playN * cN, cN));
				metricsList.Add(MakeMetric(metricType, M_PLAY_D, playD * cD, cD));
				metricsList.Add(MakeMetric(metricType, M_CUSTOM, custom * cCus, cCus));

				dayType.GetField("metrics").SetValue(dayObj, metricsList);

				// Replace same date if exists; else add
				ReplaceOrAddDay(daysList, dayObj, iso, dayType);
			}
		}

		private static object MakeMetric(Type metricType, string name, double sum, int count)
		{
			var m = Activator.CreateInstance(metricType);
			metricType.GetField("name").SetValue(m, name);
			metricType.GetField("sumSeconds").SetValue(m, sum);
			metricType.GetField("count").SetValue(m, count);
			return m;
		}

		private static void ReplaceOrAddDay(IList daysList, object dayObj, string iso, Type dayType)
		{
			for (int i = 0; i < daysList.Count; i++)
			{
				var cur = daysList[i];
				string curIso = (string)dayType.GetField("isoDate").GetValue(cur);
				if (string.Equals(curIso, iso, StringComparison.Ordinal))
				{
					daysList[i] = dayObj;
					return;
				}
			}
			daysList.Add(dayObj);
		}

		private static double Jitter(double baseline, double amplitude, System.Random rnd)
		{
			// baseline * (1 + random(-amp..+amp)) clamped >= tiny
			double f = 1.0 + (rnd.NextDouble() * 2.0 - 1.0) * amplitude;
			return Math.Max(0.01, baseline * f);
		}

		// ---------- UTIL ----------

		// Creates a strongly-typed List<T> (returned as IList) at runtime.
		private static IList CreateListOf(Type elementType)
		{
			var listType = typeof(List<>).MakeGenericType(elementType);
			return (IList)Activator.CreateInstance(listType);
		}

		private static DevMetricDataAsset EnsureLocalAsset()
		{
			EnsureFolders();

			var asset = AssetDatabase.LoadAssetAtPath<DevMetricDataAsset>(LocalAssetPath);
			if (asset == null)
			{
				asset = ScriptableObject.CreateInstance<DevMetricDataAsset>();
				asset.projectName = string.IsNullOrEmpty(asset.projectName) ? Application.productName : asset.projectName;
				asset.userName = string.IsNullOrEmpty(asset.userName) ? Environment.UserName : asset.userName;
				AssetDatabase.CreateAsset(asset, LocalAssetPath);
				AssetDatabase.SaveAssets();
			}
			return asset;
		}

		private static void EnsureFolders()
		{
			if (!AssetDatabase.IsValidFolder("Assets/_Local"))
				AssetDatabase.CreateFolder("Assets", "_Local");
			if (!AssetDatabase.IsValidFolder(ImportsFolder))
				AssetDatabase.CreateFolder("Assets/_Local", "Imports");
		}

		private static void SaveAsset(DevMetricDataAsset asset, string path)
		{
			if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
			{
				AssetDatabase.CreateAsset(asset, path);
			}
			EditorUtility.SetDirty(asset);
			AssetDatabase.SaveAssets();
			AssetDatabase.ImportAsset(path);
			AssetDatabase.Refresh();
		}
	}
}