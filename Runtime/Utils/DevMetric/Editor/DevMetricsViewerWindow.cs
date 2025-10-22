//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	/// <summary>
	/// Editor window to visualize DevMetric data from one or more DevMetricDataAsset assets.
	/// </summary>
	public class DevMetricsViewerWindow : EditorWindow
	{
		private enum Timeframe { Daily, Weekly, Monthly }

		private class Dataset
		{
			public DevMetricDataAsset asset;
			public string label;
			public bool visible = true;
			public Color color;
			public string assetPath;
			public bool isLocal;
		}

		private class AggPoint { public DateTime bucketStart; public double sum; public int count; public double avgSeconds; }
		private class Series { public List<AggPoint> points = new List<AggPoint>(); public Color color; public string label; }

		private const string LocalAssetPath = "Assets/_Local/DevMetricData.asset";
		private const string MenuPath = "Window/Dev Metric Viewer";
		private const string PrefKeyWhitelist = "DevMetricViewer_MetricWhitelist_v1"; // EditorPrefs key

		private static readonly Color[] kPalette = new[]
		{
		new Color(0.33f, 0.73f, 0.98f),
		new Color(0.94f, 0.76f, 0.20f),
		new Color(0.98f, 0.41f, 0.35f),
		new Color(0.48f, 0.80f, 0.65f),
		new Color(0.78f, 0.57f, 0.96f),
		new Color(0.99f, 0.59f, 0.07f),
		new Color(0.57f, 0.64f, 0.69f),
		new Color(0.36f, 0.84f, 0.85f),
	};

		private readonly List<Dataset> _datasets = new List<Dataset>();
		private Vector2 _scroll;

		// Tabs (raw + nice)
		private int _metricTabIndex = 0;
		private string[] _metricTabsRaw = Array.Empty<string>();
		private string[] _metricTabsNice = Array.Empty<string>();
		private string _metricSearch = "";

		// timeframe selector
		private Timeframe _timeframe = Timeframe.Daily;

		// metric whitelist (raw keys). Empty/null => show all
		private HashSet<string> _metricWhitelist;

		// Object picker control ID
		private int _pickerControlID = -1;

		// Y headroom ratio (10% by default)
		private const double Y_HEADROOM = 0.10;

		[MenuItem(MenuPath)]
		public static void Open()
		{
			var w = GetWindow<DevMetricsViewerWindow>(utility: false, title: "DevMetric");
			w.minSize = new Vector2(700, 400);
			w.Focus();
			w.LoadPrefs();
			w.LoadDefaultLocal();
		}

		public static void OpenAndShow(DevMetricDataAsset asset)
		{
			var w = GetWindow<DevMetricsViewerWindow>(utility: false, title: "DevMetric");
			w.minSize = new Vector2(700, 400);
			w.Focus();
			w.LoadPrefs();
			if (asset != null) { w.AddDataset(asset, isLocal: false); }
			w.RefreshMetricTabs();
			w.Repaint();
		}

		private void LoadPrefs() => _metricWhitelist = LoadWhitelistFromPrefs();
		private void SavePrefs() => SaveWhitelistToPrefs(_metricWhitelist);

		private void LoadDefaultLocal()
		{
			if (_datasets.Any(ds => ds.isLocal)) return;
			var asset = AssetDatabase.LoadAssetAtPath<DevMetricDataAsset>(LocalAssetPath);
			if (asset != null) AddDataset(asset, isLocal: true);
			RefreshMetricTabs();
		}

		private void AddDataset(DevMetricDataAsset asset, bool isLocal)
		{
			if (asset == null) return;
			string label = BuildLabel(asset);
			string path = AssetDatabase.GetAssetPath(asset);
			if (_datasets.Any(d => d.assetPath == path)) return;

			var ds = new Dataset
			{
				asset = asset,
				label = label,
				visible = true,
				color = kPalette[_datasets.Count % kPalette.Length],
				assetPath = path,
				isLocal = isLocal
			};
			_datasets.Add(ds);
		}

		private static string BuildLabel(DevMetricDataAsset asset)
		{
			string proj = string.IsNullOrEmpty(asset.projectName) ? "(Project?)" : asset.projectName;
			string user = string.IsNullOrEmpty(asset.userName) ? "(User?)" : asset.userName;
			return $"{proj} ({user})";
		}

		private void OnGUI()
		{
			TopBar();
			using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
			{
				_scroll = scroll.scrollPosition;

				DataSourcesUI();
				EditorGUILayout.Space(6);

				MetricTabsUI();

				var currentMetricRaw = GetCurrentMetricRaw();
				if (string.IsNullOrEmpty(currentMetricRaw))
				{
					EditorGUILayout.HelpBox("No metrics found. Generate data or add/import datasets.", MessageType.Info);
					return;
				}

				GraphUI(currentMetricRaw);
			}
		}

		private void TopBar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			if (GUILayout.Button("Add Source (Asset)", EditorStyles.toolbarButton))
			{
				_pickerControlID = EditorGUIUtility.GetControlID(FocusType.Passive);
				EditorGUIUtility.ShowObjectPicker<DevMetricDataAsset>(null, false, "", _pickerControlID);
			}

			if (GUILayout.Button("Import JSON", EditorStyles.toolbarButton))
			{
				var path = EditorUtility.OpenFilePanel("Import DevMetric JSON", "", "json");
				if (!string.IsNullOrEmpty(path))
				{
					var imported = DevMetricIO.ImportFromJsonToAsset(path);
					if (imported != null) AddDataset(imported, isLocal: false);
					RefreshMetricTabs();
				}
			}

			if (GUILayout.Button("Export Local JSON", EditorStyles.toolbarButton))
			{
				var local = _datasets.FirstOrDefault(d => d.isLocal)?.asset;
				if (local == null)
				{
					EditorUtility.DisplayDialog("DevMetric Export", "Local asset not found.", "OK");
				}
				else
				{
					string suggest = $"DevMetric_{Sanitize(local.projectName)}_{Sanitize(local.userName)}.json";
					DevMetricIO.ExportToJson(local, suggest);
				}
			}

			GUILayout.FlexibleSpace();

			// ▼ Metrics checkable dropdown
			if (EditorGUILayout.DropdownButton(new GUIContent("Metrics"), FocusType.Passive, EditorStyles.toolbarDropDown))
			{
				ShowMetricsDropdown();
			}

			// Timeframe selector
			GUILayout.Label("Timeframe", EditorStyles.label, GUILayout.Width(70));
			_timeframe = (Timeframe)EditorGUILayout.EnumPopup(_timeframe, GUILayout.Width(110));

			// Search (matches nice or raw)
			_metricSearch = GUILayout.TextField(_metricSearch, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField, GUILayout.Width(200));

			EditorGUILayout.EndHorizontal();

			// Object picker result with control ID gate
			if (Event.current.commandName == "ObjectSelectorClosed" &&
				EditorGUIUtility.GetObjectPickerControlID() == _pickerControlID)
			{
				var picked = EditorGUIUtility.GetObjectPickerObject() as DevMetricDataAsset;
				if (picked != null)
				{
					AddDataset(picked, isLocal: false);
					RefreshMetricTabs();
					Repaint();
				}
				_pickerControlID = -1;
			}
		}

		// === Metrics Dropdown ===

		private void ShowMetricsDropdown()
		{
			var menu = new GenericMenu();
			BuildMetricsMenu(menu);
			menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
		}

		private void BuildMetricsMenu(GenericMenu menu)
		{
			var allRaw = GetAllMetricKeysSorted(); // sorted raw keys
												   // Build map from raw->nice and also a nice-sorted list
			var items = allRaw
				.Select(r => (raw: r, nice: GetNiceName(r)))
				.OrderBy(t => t.nice, StringComparer.CurrentCultureIgnoreCase)
				.ToList();

			bool hasWhitelist = _metricWhitelist != null && _metricWhitelist.Count > 0;

			// Header actions
			menu.AddItem(new GUIContent("Select All"), false, () => SelectAllMetrics(allRaw));
			menu.AddSeparator("");

			// Checkable items
			foreach (var it in items)
			{
				bool on = hasWhitelist ? _metricWhitelist.Contains(it.raw) : true; // empty whitelist => show all => checked
				var content = new GUIContent($"{it.nice}", it.raw); // raw in tooltip
				menu.AddItem(content, on, () => ToggleMetric(it.raw));
			}
		}

		private void ToggleMetric(string raw)
		{
			_metricWhitelist ??= new HashSet<string>(StringComparer.Ordinal);

			// If currently "show all" (empty whitelist), initialize whitelist to "all" then toggle raw off
			if (_metricWhitelist.Count == 0)
			{
				foreach (var k in GetAllMetricKeysSorted())
					_metricWhitelist.Add(k);
			}

			if (_metricWhitelist.Contains(raw)) _metricWhitelist.Remove(raw);
			else _metricWhitelist.Add(raw);

			SavePrefs();
			RefreshMetricTabs();
			Repaint();
		}

		private void SelectAllMetrics(List<string> allRaw)
		{
			_metricWhitelist = new HashSet<string>(allRaw, StringComparer.Ordinal);
			SavePrefs();
			RefreshMetricTabs();
			Repaint();
		}

		private void ClearSelectionShowAll()
		{
			// Empty whitelist means "show everything"
			_metricWhitelist = new HashSet<string>(StringComparer.Ordinal);
			SavePrefs();
			RefreshMetricTabs();
			Repaint();
		}

		private void DataSourcesUI()
		{
			if (_datasets.Count == 0)
			{
				EditorGUILayout.HelpBox("No data sources. The local asset will appear automatically once created. You can also add an existing asset or import JSON.", MessageType.None);
				return;
			}

			EditorGUILayout.LabelField("Data Sources", EditorStyles.boldLabel);
			for (int i = 0; i < _datasets.Count; i++)
			{
				var ds = _datasets[i];
				EditorGUILayout.BeginHorizontal();

				ds.visible = EditorGUILayout.Toggle(ds.visible, GUILayout.Width(18));
				ds.color = EditorGUILayout.ColorField(GUIContent.none, ds.color, true, true, false, GUILayout.Width(40));
				EditorGUILayout.LabelField(new GUIContent(ds.label, ds.assetPath));

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Export", GUILayout.Width(70)))
				{
					string suggest = $"DevMetric_{Sanitize(ds.asset.projectName)}_{Sanitize(ds.asset.userName)}.json";
					DevMetricIO.ExportToJson(ds.asset, suggest);
				}

				if (GUILayout.Button("Remove", GUILayout.Width(70)))
				{
					_datasets.RemoveAt(i);
					i--;
					RefreshMetricTabs();
					continue;
				}

				var deleteText = ds.isLocal ? "Delete Local Asset" : "Delete Asset";
				if (GUILayout.Button(deleteText, GUILayout.Width(120)))
				{
					if (EditorUtility.DisplayDialog("Delete Asset?",
						$"This will permanently delete:\n{ds.assetPath}\n\nProceed?",
						"Delete", "Cancel"))
					{
						if (AssetDatabase.DeleteAsset(ds.assetPath))
						{
							_datasets.RemoveAt(i);
							i--;
							RefreshMetricTabs();
							continue;
						}
						else
						{
							EditorUtility.DisplayDialog("Delete Asset", "Failed to delete asset. Check Console for details.", "OK");
						}
					}
				}

				EditorGUILayout.EndHorizontal();
			}
		}

		private void MetricTabsUI()
		{
			RefreshMetricTabs();

			if (_metricTabsRaw.Length == 0)
			{
				EditorGUILayout.HelpBox("No metrics in the loaded data sources.", MessageType.Info);
				return;
			}

			// Filter tabs using both nice + raw for matching; display nice labels
			var filtered = new List<(int index, string nice)>();
			for (int i = 0; i < _metricTabsRaw.Length; i++)
			{
				string raw = _metricTabsRaw[i];
				string nice = _metricTabsNice[i];
				if (string.IsNullOrEmpty(_metricSearch) ||
					raw.IndexOf(_metricSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
					nice.IndexOf(_metricSearch, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					filtered.Add((i, nice));
				}
			}

			if (filtered.Count == 0)
			{
				EditorGUILayout.HelpBox("No metrics match your search.", MessageType.Info);
				return;
			}

			int filteredSel = Mathf.Max(0, filtered.FindIndex(f => f.index == _metricTabIndex));
			int newFilteredSel = GUILayout.Toolbar(filteredSel, filtered.Select(f => f.nice).ToArray(), GUILayout.Height(24));
			_metricTabIndex = filtered[newFilteredSel].index;

			EditorGUILayout.Space(6);
		}

		private string GetCurrentMetricRaw()
		{
			if (_metricTabsRaw.Length == 0) return null;
			int idx = Mathf.Clamp(_metricTabIndex, 0, _metricTabsRaw.Length - 1);
			return _metricTabsRaw[idx];
		}

		private void GraphUI(string metricRaw)
		{
			var rect = GUILayoutUtility.GetRect(10, 420, GUILayout.ExpandWidth(true));
			GUI.Box(rect, GUIContent.none);

			var series = BuildSeries(metricRaw);
			if (series.Count == 0)
			{
				EditorGUI.LabelField(rect, "No data for this metric.", CenterStyle());
				return;
			}

			// Union of bucket keys (global X)
			var allBuckets = new SortedSet<DateTime>();
			foreach (var s in series)
				foreach (var p in s.points)
					allBuckets.Add(p.bucketStart);
			var buckets = allBuckets.ToList();
			if (buckets.Count == 0)
			{
				EditorGUI.LabelField(rect, "No points.", CenterStyle());
				return;
			}

			// Global Y range with headroom
			double minY = 0.0, maxY = 0.0;
			foreach (var s in series)
				foreach (var p in s.points)
					maxY = Math.Max(maxY, p.avgSeconds);
			if (Math.Abs(maxY - minY) < 1e-6) maxY = 1.0;
			else maxY = maxY * (1.0 + Y_HEADROOM);

			// Plot rect
			float leftPad = 60f;
			float bottomPad = 34f;
			Rect plot = new Rect(rect.x + leftPad, rect.y + 8, rect.width - leftPad - 8, rect.height - bottomPad - 12);

			DrawGrid(plot, buckets.Count, minY, maxY);

			Handles.BeginGUI();
			DrawYAxisLabels(rect, plot, minY, maxY);
			DrawXAxisLabels(rect, plot, buckets);
			Handles.EndGUI();

			foreach (var s in series)
				DrawCurveCarryForward(plot, buckets, s.points, s.color, minY, maxY);

			string nice = GetNiceName(metricRaw);
			EditorGUI.LabelField(new Rect(plot.x, rect.y + 2, plot.width, 18),
				$"Metric: {nice}  (avg seconds/{_timeframe})", EditorStyles.boldLabel);

			HoverUI(plot, buckets, series, minY, maxY);
		}

		private void HoverUI(Rect plot, List<DateTime> buckets, List<Series> series, double minY, double maxY)
		{
			var e = Event.current;
			if (e == null) return;
			if (!plot.Contains(e.mousePosition)) return;

			float t = Mathf.InverseLerp(plot.xMin, plot.xMax, e.mousePosition.x);
			int idx = Mathf.Clamp(Mathf.RoundToInt(t * (buckets.Count - 1)), 0, buckets.Count - 1);
			float x = Mathf.Lerp(plot.xMin, plot.xMax, (buckets.Count == 1) ? 0f : idx / (float)(buckets.Count - 1));
			var bucketDate = buckets[idx];

			var lines = new List<(Color col, string label, string valueStr, float yPix, bool hasValue)>();
			foreach (var s in series)
			{
				var last = s.points.LastOrDefault(p => p.bucketStart <= bucketDate);
				bool hasVal = last != null;
				double v = hasVal ? last.avgSeconds : double.NaN;

				float y = hasVal
					? Mathf.Lerp(plot.yMax, plot.yMin, (float)((v - minY) / Math.Max(1e-6, (maxY - minY))))
					: float.NaN;

				lines.Add((s.color, s.label, hasVal ? $"{v:0.###} s" : "—", y, hasVal));
			}

			Handles.BeginGUI();
			Handles.color = new Color(1, 1, 1, 0.15f);
			Handles.DrawLine(new Vector3(x, plot.yMin), new Vector3(x, plot.yMax));
			Handles.EndGUI();

			foreach (var L in lines)
			{
				if (!L.hasValue) continue;
				var r = new Rect(x - 2.5f, L.yPix - 2.5f, 5f, 5f);
				EditorGUI.DrawRect(r, L.col);
			}

			string dateStr = _timeframe switch
			{
				Timeframe.Daily => bucketDate.ToString("yyyy-MM-dd"),
				Timeframe.Weekly => $"Week {ISOWeekOf(bucketDate)} (start {bucketDate:yyyy-MM-dd})",
				Timeframe.Monthly => bucketDate.ToString("yyyy-MM"),
				_ => bucketDate.ToString("yyyy-MM-dd"),
			};

			var sb = new System.Text.StringBuilder();
			sb.AppendLine(dateStr);
			foreach (var L in lines)
				sb.AppendLine($"{L.label}: {L.valueStr}");
			string tip = sb.ToString().TrimEnd();

			var style = new GUIStyle(EditorStyles.helpBox)
			{
				fontSize = 11,
				richText = false,
				alignment = TextAnchor.UpperLeft,
				padding = new RectOffset(8, 8, 6, 6)
			};

			Vector2 size = style.CalcSize(new GUIContent(tip));
			float textHeight = style.CalcHeight(new GUIContent(tip), Mathf.Min(320f, size.x));
			size.y = Mathf.Max(size.y, textHeight);

			float pad = 8f;
			float tx = e.mousePosition.x + 12f;
			float ty = e.mousePosition.y + 12f;
			if (tx + size.x + pad > position.width) tx = position.width - size.x - pad;
			if (ty + size.y + pad > position.height) ty = position.height - size.y - pad;

			var tipRect = new Rect(tx, ty, size.x, size.y);
			GUI.Box(tipRect, tip, style);

			Repaint();
		}

		// ----- aggregation, carry-forward drawing, and helpers -----

		private GUIStyle CenterStyle()
		{
			var s = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
			return s;
		}

		private List<Series> BuildSeries(string metricName)
		{
			var list = new List<Series>();
			foreach (var ds in _datasets)
			{
				if (!ds.visible || ds.asset == null) continue;

				var daysField = typeof(DevMetricDataAsset).GetField("days", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				var days = daysField.GetValue(ds.asset) as System.Collections.IList;
				if (days == null) continue;

				var buckets = new Dictionary<DateTime, (double sum, int count)>();

				foreach (var dObj in days)
				{
					var dType = dObj.GetType();
					var iso = (string)dType.GetField("isoDate").GetValue(dObj);
					var metricsList = dType.GetField("metrics").GetValue(dObj) as System.Collections.IList;

					if (!DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
						continue;

					var bucketStart = GetBucketStart(date.Date);
					(double sum, int count) day = (0, 0);

					if (metricsList != null)
					{
						foreach (var mObj in metricsList)
						{
							var mType = mObj.GetType();
							if ((string)mType.GetField("name").GetValue(mObj) == metricName)
							{
								double sum = (double)mType.GetField("sumSeconds").GetValue(mObj);
								int count = (int)mType.GetField("count").GetValue(mObj);
								day = (sum, count);
								break;
							}
						}
					}

					if (day.count > 0)
					{
						if (!buckets.TryGetValue(bucketStart, out var agg))
							agg = (0, 0);
						agg.sum += day.sum;
						agg.count += day.count;
						buckets[bucketStart] = agg;
					}
				}

				var srs = new Series { color = ds.color, label = ds.label };
				foreach (var kv in buckets)
					srs.points.Add(new AggPoint { bucketStart = kv.Key, sum = kv.Value.sum, count = kv.Value.count, avgSeconds = kv.Value.count > 0 ? kv.Value.sum / kv.Value.count : 0.0 });

				srs.points.Sort((a, b) => a.bucketStart.CompareTo(b.bucketStart));
				if (srs.points.Count > 0) list.Add(srs);
			}
			return list;
		}

		private DateTime GetBucketStart(DateTime date)
		{
			switch (_timeframe)
			{
				case Timeframe.Daily: return date.Date;
				case Timeframe.Weekly:
					int offset = ((int)date.DayOfWeek + 6) % 7; // Monday=0
					return date.Date.AddDays(-offset);
				case Timeframe.Monthly: return new DateTime(date.Year, date.Month, 1);
				default: return date.Date;
			}
		}

		private void DrawGrid(Rect plot, int xTicks, double minY, double maxY)
		{
			EditorGUI.DrawRect(plot, EditorGUIUtility.isProSkin ? new Color(0.10f, 0.10f, 0.10f, 1f) : new Color(0.95f, 0.95f, 0.95f, 1f));
			Handles.BeginGUI();
			Handles.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.06f) : new Color(0, 0, 0, 0.06f);

			int yLines = 4;
			for (int i = 0; i <= yLines; i++)
			{
				float y = Mathf.Lerp(plot.yMax, plot.yMin, i / (float)yLines);
				Handles.DrawLine(new Vector3(plot.xMin, y), new Vector3(plot.xMax, y));
			}

			int xLines = Mathf.Clamp(xTicks, 2, 12);
			for (int i = 0; i <= xLines; i++)
			{
				float x = Mathf.Lerp(plot.xMin, plot.xMax, i / (float)xLines);
				Handles.DrawLine(new Vector3(x, plot.yMin), new Vector3(x, plot.yMax));
			}

			Handles.EndGUI();
		}

		private void DrawYAxisLabels(Rect rect, Rect plot, double minY, double maxY)
		{
			int yLines = 4;
			for (int i = 0; i <= yLines; i++)
			{
				double v = Mathf.Lerp((float)minY, (float)maxY, i / (float)yLines);
				float y = Mathf.Lerp(plot.yMax, plot.yMin, i / (float)yLines);
				var r = new Rect(rect.x + 6, y - 8, 52, 16);
				GUI.Label(r, $"{v:0.##} s", EditorStyles.miniLabel);
			}
		}

		private void DrawXAxisLabels(Rect rect, Rect plot, List<DateTime> buckets)
		{
			int labels = Mathf.Clamp(buckets.Count, 2, 8);
			for (int i = 0; i < labels; i++)
			{
				int idx = Mathf.RoundToInt((i / (float)(labels - 1)) * (buckets.Count - 1));
				float t = idx / Mathf.Max(1f, buckets.Count - 1);
				float x = Mathf.Lerp(plot.xMin, plot.xMax, t);

				string s = _timeframe switch
				{
					Timeframe.Daily => buckets[idx].ToString("MM-dd"),
					Timeframe.Weekly => "W " + ISOWeekOf(buckets[idx]),
					Timeframe.Monthly => buckets[idx].ToString("yyyy-MM"),
					_ => buckets[idx].ToString("MM-dd"),
				};

				var r = new Rect(x - 24, plot.yMax + 2, 48, 16);
				GUI.Label(r, s, EditorStyles.miniLabel);
			}
		}

		private static string ISOWeekOf(DateTime mondayStart)
		{
			var cal = CultureInfo.InvariantCulture.Calendar;
			var week = cal.GetWeekOfYear(mondayStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
			return $"{mondayStart.Year}-{week:00}";
		}

		private void DrawCurveCarryForward(Rect plot, List<DateTime> allBuckets, List<AggPoint> points, Color color, double minY, double maxY)
		{
			if (points.Count == 0) return;

			int pIdx = 0;
			double? lastVal = null;

			List<Vector3> currentSeg = new List<Vector3>();
			Action flush = () =>
			{
				if (currentSeg.Count >= 2)
				{
					Handles.BeginGUI();
					Handles.color = new Color(color.r, color.g, color.b, 0.95f);
					Handles.DrawAAPolyLine(2.5f, currentSeg.ToArray());
					Handles.EndGUI();
				}
				currentSeg.Clear();
			};

			for (int i = 0; i < allBuckets.Count; i++)
			{
				var b = allBuckets[i];

				while (pIdx < points.Count && points[pIdx].bucketStart <= b)
				{
					lastVal = points[pIdx].avgSeconds;
					pIdx++;
				}

				if (!lastVal.HasValue)
				{
					flush();
					continue;
				}

				double v = lastVal.Value;

				float t = (allBuckets.Count == 1) ? 0f : i / (float)(allBuckets.Count - 1);
				float x = Mathf.Lerp(plot.xMin, plot.xMax, t);
				float y = Mathf.Lerp(plot.yMax, plot.yMin, (float)((v - minY) / Math.Max(1e-6, (maxY - minY))));
				currentSeg.Add(new Vector3(x, y));
			}

			flush();
		}

		private void RefreshMetricTabs()
		{
			var rawSet = new HashSet<string>();
			foreach (var ds in _datasets)
			{
				if (ds.asset == null) continue;
				var daysField = typeof(DevMetricDataAsset).GetField("days", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				var days = daysField.GetValue(ds.asset) as System.Collections.IList;
				if (days == null) continue;

				foreach (var dObj in days)
				{
					var dType = dObj.GetType();
					var metricsList = dType.GetField("metrics").GetValue(dObj) as System.Collections.IList;
					if (metricsList == null) continue;

					foreach (var mObj in metricsList)
					{
						var mType = mObj.GetType();
						string name = (string)mType.GetField("name").GetValue(mObj);
						if (!string.IsNullOrEmpty(name)) rawSet.Add(name);
					}
				}
			}

			var raw = rawSet.ToList();
			raw.Sort(StringComparer.Ordinal);

			// Apply whitelist (if any)
			if (_metricWhitelist != null && _metricWhitelist.Count > 0)
				raw = raw.Where(r => _metricWhitelist.Contains(r)).ToList();

			// Build nice names; ensure uniqueness
			var nice = new string[raw.Count];
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < raw.Count; i++)
			{
				string candidate = GetNiceName(raw[i]);
				if (!seen.Add(candidate))
					candidate = $"{candidate} ({raw[i]})";
				seen.Add(candidate);
				nice[i] = candidate;
			}

			_metricTabsRaw = raw.ToArray();
			_metricTabsNice = nice;

			_metricTabIndex = Mathf.Clamp(_metricTabIndex, 0, Math.Max(0, _metricTabsRaw.Length - 1));
		}

		// ---------- Metric whitelist prefs ----------

		private static HashSet<string> LoadWhitelistFromPrefs()
		{
			var s = EditorPrefs.GetString(PrefKeyWhitelist, string.Empty);
			if (string.IsNullOrEmpty(s)) return new HashSet<string>(StringComparer.Ordinal);
			var parts = s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			return new HashSet<string>(parts, StringComparer.Ordinal);
		}

		private static void SaveWhitelistToPrefs(HashSet<string> set)
		{
			if (set == null || set.Count == 0)
			{
				EditorPrefs.SetString(PrefKeyWhitelist, string.Empty);
				return;
			}
			var joined = string.Join("|", set);
			EditorPrefs.SetString(PrefKeyWhitelist, joined);
		}

		private List<string> GetAllMetricKeysSorted()
		{
			var all = new HashSet<string>();
			foreach (var ds in _datasets)
			{
				if (ds.asset == null) continue;
				var daysField = typeof(DevMetricDataAsset).GetField("days", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				var days = daysField.GetValue(ds.asset) as System.Collections.IList;
				if (days == null) continue;

				foreach (var dObj in days)
				{
					var dType = dObj.GetType();
					var metricsList = dType.GetField("metrics").GetValue(dObj) as System.Collections.IList;
					if (metricsList == null) continue;

					foreach (var mObj in metricsList)
					{
						var mType = mObj.GetType();
						string name = (string)mType.GetField("name").GetValue(mObj);
						if (!string.IsNullOrEmpty(name)) all.Add(name);
					}
				}
			}
			var list = all.ToList();
			list.Sort(StringComparer.Ordinal);
			return list;
		}

		// ---------- Nice name formatting ----------

		private static readonly Dictionary<string, string> kNiceOverrides = new Dictionary<string, string>
	{
		{ "CompilationTime_s",              "Compilation Time" },
		{ "ProjectBootTime_s",              "Project Boot Time" },
		{ "EnterPlayMode_NoDomainReload_s", "Play Mode (No Domain Reload)" },
		{ "EnterPlayMode_DomainReload_s",   "Play Mode (Domain Reload)" },
	};

		private string GetNiceName(string rawKey)
		{
			if (string.IsNullOrEmpty(rawKey)) return rawKey;

			if (kNiceOverrides.TryGetValue(rawKey, out var nice))
				return nice;

			string key = rawKey.EndsWith("_s", StringComparison.Ordinal) ? rawKey[..^2] : rawKey;
			key = key.Replace('_', ' ');

			string[] words = key.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < words.Length; i++)
				words[i] = NiceWord(words[i]);

			return string.Join(" ", words);
		}

		private static readonly HashSet<string> kAcronyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"CPU","GPU","IL2CPP","HDRP","URP","DX","VULKAN","GLES","API","DOTS"
	};

		private static string NiceWord(string w)
		{
			if (string.IsNullOrEmpty(w)) return w;
			if (kAcronyms.Contains(w)) return w.ToUpperInvariant();

			var parts = SplitCamel(w);
			for (int i = 0; i < parts.Count; i++)
			{
				var p = parts[i];
				if (kAcronyms.Contains(p)) parts[i] = p.ToUpperInvariant();
				else parts[i] = char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..].ToLowerInvariant() : "");
			}
			return string.Join(" ", parts);
		}

		private static List<string> SplitCamel(string s)
		{
			var list = new List<string>();
			if (string.IsNullOrEmpty(s)) { list.Add(s); return list; }

			int start = 0;
			for (int i = 1; i < s.Length; i++)
				if (char.IsUpper(s[i]) && !char.IsUpper(s[i - 1]))
				{
					list.Add(s.Substring(start, i - start));
					start = i;
				}
			list.Add(s.Substring(start));
			return list;
		}

		private static string Sanitize(string s)
		{
			if (string.IsNullOrEmpty(s)) return "Unknown";
			var bad = System.IO.Path.GetInvalidFileNameChars();
			foreach (var c in bad) s = s.Replace(c, '_');
			return s;
		}
	}
}
