//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	public static class DevMetricIO
	{
		[Serializable]
		public class ExportMetricAggregate { public string name; public double sumSeconds; public int count; }

		[Serializable]
		public class ExportDayRecord { public string isoDate; public List<ExportMetricAggregate> metrics = new List<ExportMetricAggregate>(); }

		[Serializable]
		public class DevMetricExport
		{
			public string projectName;
			public string userName;
			public List<ExportDayRecord> days = new List<ExportDayRecord>();
			public int maxDaysToKeep = 180;
			public string exportedAtIsoUtc;
			public string sourceAssetPath;
		}

		private const string DefaultExportName = "DevMetricExport.json";

		public static void ExportToJson(DevMetricDataAsset asset, string suggestedFileName = null)
		{
			if (asset == null)
			{
				EditorUtility.DisplayDialog("DevMetric Export", "No asset selected to export.", "OK");
				return;
			}

			var export = new DevMetricExport
			{
				projectName = asset.projectName,
				userName = asset.userName,
				days = new List<ExportDayRecord>(),
				maxDaysToKeep = 180,
				exportedAtIsoUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				sourceAssetPath = AssetDatabase.GetAssetPath(asset)
			};

			var daysField = asset.GetType().GetField("days", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var daysList = daysField != null ? daysField.GetValue(asset) as System.Collections.IList : null;
			if (daysList != null)
			{
				foreach (var dObj in daysList)
				{
					var dType = dObj.GetType();
					var isoField = dType.GetField("isoDate");
					var metricsField = dType.GetField("metrics");

					var d = new ExportDayRecord
					{
						isoDate = (string)isoField.GetValue(dObj),
						metrics = new List<ExportMetricAggregate>()
					};

					var list = metricsField.GetValue(dObj) as System.Collections.IList;
					if (list != null)
					{
						foreach (var mObj in list)
						{
							var mType = mObj.GetType();
							d.metrics.Add(new ExportMetricAggregate
							{
								name = (string)mType.GetField("name").GetValue(mObj),
								sumSeconds = (double)mType.GetField("sumSeconds").GetValue(mObj),
								count = (int)mType.GetField("count").GetValue(mObj)
							});
						}
					}
					export.days.Add(d);
				}
			}

			string defaultName = !string.IsNullOrEmpty(suggestedFileName) ? suggestedFileName : DefaultExportName;
			string path = EditorUtility.SaveFilePanel("Export DevMetric Data", "", defaultName, "json");
			if (string.IsNullOrEmpty(path)) return;

			string json = JsonUtility.ToJson(export, true);
			File.WriteAllText(path, json);
			EditorUtility.RevealInFinder(path);
		}

		public static DevMetricDataAsset ImportFromJsonToAsset(string jsonPath, string targetFolder = "Assets/_Local/Imports")
		{
			if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
			{
				EditorUtility.DisplayDialog("DevMetric Import", "Invalid JSON file.", "OK");
				return null;
			}

			string json = File.ReadAllText(jsonPath);
			DevMetricExport data;
			try { data = JsonUtility.FromJson<DevMetricExport>(json); }
			catch (Exception e)
			{
				Debug.LogError($"DevMetric Import failed: {e.Message}");
				EditorUtility.DisplayDialog("DevMetric Import", "JSON parsing failed. See Console for details.", "OK");
				return null;
			}

			if (!AssetDatabase.IsValidFolder("Assets/_Local"))
				AssetDatabase.CreateFolder("Assets", "_Local");
			if (!AssetDatabase.IsValidFolder(targetFolder))
				AssetDatabase.CreateFolder("Assets/_Local", "Imports");

			var asset = ScriptableObject.CreateInstance<DevMetricDataAsset>();
			asset.projectName = data.projectName;
			asset.userName = data.userName;

			var dayType = typeof(DevMetricDataAsset).GetNestedType("DayRecord");
			var metricType = typeof(DevMetricDataAsset).GetNestedType("MetricAggregate");
			var daysField = typeof(DevMetricDataAsset).GetField("days", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dayType));

			foreach (var d in data.days)
			{
				var day = Activator.CreateInstance(dayType);
				dayType.GetField("isoDate").SetValue(day, d.isoDate);

				var metricsList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(metricType));
				foreach (var m in d.metrics)
				{
					var mi = Activator.CreateInstance(metricType);
					metricType.GetField("name").SetValue(mi, m.name);
					metricType.GetField("sumSeconds").SetValue(mi, m.sumSeconds);
					metricType.GetField("count").SetValue(mi, m.count);
					metricsList.Add(mi);
				}
				dayType.GetField("metrics").SetValue(day, metricsList);
				list.Add(day);
			}
			daysField.SetValue(asset, list);

			string safeProj = string.IsNullOrEmpty(asset.projectName) ? "UnknownProject" : Sanitize(asset.projectName);
			string safeUser = string.IsNullOrEmpty(asset.userName) ? "UnknownUser" : Sanitize(asset.userName);
			string fileName = $"{safeProj}__{safeUser}__Imported.asset";
			string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{fileName}");

			AssetDatabase.CreateAsset(asset, uniquePath);
			AssetDatabase.SaveAssets();
			AssetDatabase.ImportAsset(uniquePath);

			EditorUtility.DisplayDialog("DevMetric Import", $"Imported as asset:\n{uniquePath}", "OK");
			return asset;
		}

		private static string Sanitize(string s)
		{
			foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
			return s;
		}
	}
}
