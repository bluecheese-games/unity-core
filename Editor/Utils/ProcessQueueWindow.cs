//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;
using BlueCheese.Core.Utils;

namespace BlueCheese.Core.Editor
{
	public class ProcessQueueWindow : EditorWindow
	{
		private ProcessQueue _queue;
		private CancellationTokenSource _cts;
		private Action _onComplete;
		private List<string> _allSteps = new List<string>();
		private List<GroupedStep> _groupedSteps = new List<GroupedStep>();
		private Vector2 _scrollPosition;
		private bool _isCancelled;
		private bool _autoClose;

		// Time tracking
		[SerializeField] private double _startTime;
		[SerializeField] private double _endTime;

		// Per-step duration tracking
		private double[] _stepDurations;
		private double _lastStepFinishTime;

		// Error tracking
		private Dictionary<int, string> _stepErrors = new Dictionary<int, string>();

		// Auto-scroll tracking
		private int _lastActiveGroupIndex = -1;

		// Styles
		private GUIStyle _stepPendingStyle;
		private GUIStyle _stepRunningStyle;
		private GUIStyle _stepDoneStyle;
		private GUIStyle _stepCancelledStyle;
		private GUIStyle _stepFailedStyle;
		private GUIStyle _durationStyle;

		/// <summary>
		/// Gets the total duration of the process in seconds.
		/// If running, returns current elapsed time.
		/// If finished, returns final duration.
		/// Returns 0 if not started.
		/// </summary>
		public double TotalTime
		{
			get
			{
				if (_startTime <= 0) return 0;
				double currentTime = EditorApplication.timeSinceStartup;
				double currentEnd = _endTime > 0 ? _endTime : currentTime;
				return Math.Max(0, currentEnd - _startTime);
			}
		}

		[Serializable]
		private class GroupedStep
		{
			public string Name;
			public List<int> Indices = new List<int>(); // Raw indices in the original queue
		}

		/// <summary>
		/// Opens the window. If autoStart is true, processing begins immediately.
		/// Otherwise, the user must click "Start Process".
		/// </summary>
		public static void Open(ProcessQueue queue, string title, Action onComplete = null, bool autoClose = false, bool autoStart = true)
		{
			var window = GetWindow<ProcessQueueWindow>(true, title, true);
			window.Initialize(queue, onComplete, autoClose, autoStart);
			window.Show();
		}

		private void Initialize(ProcessQueue queue, Action onComplete, bool autoClose, bool autoStart)
		{
			_queue = queue;
			_cts = new CancellationTokenSource();
			_onComplete = onComplete;
			_isCancelled = false;
			_autoClose = autoClose;

			// Reset time tracking
			_startTime = 0;
			_endTime = 0;
			_lastStepFinishTime = 0;
			_lastActiveGroupIndex = -1;

			// Snapshot the steps for the checklist before they are dequeued
			_allSteps = _queue.GetPendingStepNames().ToList();
			_stepDurations = new double[_allSteps.Count];
			_stepErrors.Clear();

			GroupSteps();

			// Hook into queue events
			_queue.Progressed += OnQueueProgress;
			_queue.StepFailed += OnStepFailed;

			if (autoStart)
			{
				StartProcess();
			}
		}

		private void StartProcess()
		{
			_startTime = EditorApplication.timeSinceStartup;
			_lastStepFinishTime = _startTime;
			RunProcess().Forget();
		}

		private void OnQueueProgress(float progress)
		{
			// Calculate duration for the step that just finished
			double now = EditorApplication.timeSinceStartup;

			// Determine index of finished item based on progress
			int processedCount = Mathf.RoundToInt(progress * _queue.TotalCount);
			int finishedIndex = processedCount - 1;

			if (finishedIndex >= 0 && finishedIndex < _stepDurations.Length)
			{
				_stepDurations[finishedIndex] = now - _lastStepFinishTime;
			}

			_lastStepFinishTime = now;
			Repaint();
		}

		private void OnStepFailed(int index, Exception ex)
		{
			if (!_stepErrors.ContainsKey(index))
			{
				_stepErrors[index] = ex.Message;
			}
		}

		private bool IsIgnoredStep(string name)
		{
			if (string.IsNullOrEmpty(name)) return false;
			return name.StartsWith("Delay") || name == "WaitForNextFrame";
		}

		private void GroupSteps()
		{
			_groupedSteps.Clear();
			if (_allSteps.Count == 0) return;

			GroupedStep current = null;

			for (int i = 0; i < _allSteps.Count; i++)
			{
				string name = _allSteps[i];

				if (IsIgnoredStep(name)) continue;

				if (current != null && current.Name == name)
				{
					current.Indices.Add(i);
				}
				else
				{
					current = new GroupedStep { Name = name };
					current.Indices.Add(i);
					_groupedSteps.Add(current);
				}
			}
		}

		private async UniTaskVoid RunProcess()
		{
			// Handle empty queue case gracefully
			if (_queue.Count == 0)
			{
				_onComplete?.Invoke();
				_endTime = EditorApplication.timeSinceStartup;

				if (_autoClose && !_isCancelled)
				{
					Close();
					return;
				}

				Repaint();
				return;
			}

			try
			{
				await _queue.ProcessAsync(_cts.Token);
				_onComplete?.Invoke();

				if (_autoClose && !_isCancelled)
				{
					Close();
					return;
				}
			}
			catch (OperationCanceledException)
			{
				_isCancelled = true;
				Debug.Log("Process cancelled by user.");
			}
			catch (Exception e)
			{
				_isCancelled = true;
				Debug.LogError($"Process failed: {e}");
			}
			finally
			{
				if (_endTime == 0)
					_endTime = EditorApplication.timeSinceStartup;

				if (_queue != null)
				{
					_queue.Progressed -= OnQueueProgress;
					_queue.StepFailed -= OnStepFailed;
				}

				_cts?.Dispose();
				_cts = null;

				if (!(_autoClose && !_isCancelled))
				{
					Repaint();
				}
			}
		}

		private void OnEnable()
		{
			EditorApplication.update += Repaint;
		}

		private void OnDisable()
		{
			EditorApplication.update -= Repaint;

			if (_queue != null)
			{
				_queue.Progressed -= OnQueueProgress;
				_queue.StepFailed -= OnStepFailed;
			}

			if (_cts != null)
			{
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}
		}

		private void OnGUI()
		{
			InitStyles();

			if (_queue == null)
			{
				EditorGUILayout.HelpBox("No active process.", MessageType.Info);
				return;
			}

			DrawHeader();
			DrawProgressBar();
			DrawStepList();
			DrawFooter();
		}

		private void DrawHeader()
		{
			EditorGUILayout.Space(10);

			string timeStr = FormatDuration(TotalTime);

			Rect r = EditorGUILayout.GetControlRect(false, 20);

			if (_isCancelled)
			{
				var style = new GUIStyle(EditorStyles.boldLabel);
				style.normal.textColor = Color.red;
				EditorGUI.LabelField(r, "Process Cancelled", style);
			}
			else if (_queue.IsProcessing)
			{
				EditorGUI.LabelField(r, $"Processing: {_queue.ProcessingAction}...", EditorStyles.boldLabel);
			}
			else if (_endTime > 0)
			{
				string msg = (_allSteps.Count == 0) ? "Queue Empty (Done)" : "Process Complete!";
				EditorGUI.LabelField(r, msg, EditorStyles.boldLabel);
			}
			else
			{
				string msg = (_allSteps.Count == 0) ? "Queue is Empty" : "Ready to Start";
				EditorGUI.LabelField(r, msg, EditorStyles.boldLabel);
			}

			var timerStyle = new GUIStyle(EditorStyles.label);
			timerStyle.alignment = TextAnchor.MiddleRight;
			EditorGUI.LabelField(r, timeStr, timerStyle);

			EditorGUILayout.Space(5);
		}

		private void DrawProgressBar()
		{
			Rect r = EditorGUILayout.GetControlRect(false, 20);

			int totalVisible = _groupedSteps.Sum(g => g.Indices.Count);

			if (totalVisible == 0)
			{
				float emptyProgress = (_endTime > 0) ? 1.0f : 0.0f;
				EditorGUI.ProgressBar(r, emptyProgress, _endTime > 0 ? "Completed" : "Empty");
				EditorGUILayout.Space(10);
				return;
			}

			int completedVisible = 0;
			int rawProcessedCount = Mathf.RoundToInt(_queue.Progress * _queue.TotalCount);

			foreach (var group in _groupedSteps)
			{
				completedVisible += group.Indices.Count(idx => idx < rawProcessedCount);
			}

			float progress = totalVisible > 0 ? (float)completedVisible / totalVisible : 0f;

			string label = _isCancelled
				? "Cancelled"
				: $"{completedVisible} / {totalVisible}";

			EditorGUI.ProgressBar(r, progress, label);
			EditorGUILayout.Space(10);
		}

		private void DrawStepList()
		{
			EditorGUILayout.LabelField("Tasks", EditorStyles.boldLabel);

			// Calculate state for auto-scroll and rendering
			int rawProcessedCount = Mathf.RoundToInt(_queue.Progress * _queue.TotalCount);
			int dequeuedCount = _queue.TotalCount - _queue.Count;
			int rawCancelledIndex = -1;
			if (_isCancelled)
			{
				rawCancelledIndex = (dequeuedCount > rawProcessedCount) ? dequeuedCount - 1 : dequeuedCount;
			}

			// Identify active group for auto-scroll
			int activeGroupIndex = -1;
			if (_queue.IsProcessing && !_isCancelled)
			{
				for (int i = 0; i < _groupedSteps.Count; i++)
				{
					var group = _groupedSteps[i];
					int min = group.Indices[0];
					int max = group.Indices[group.Indices.Count - 1];
					if (rawProcessedCount >= min && rawProcessedCount <= max)
					{
						activeGroupIndex = i;
						break;
					}
				}
			}

			// Apply Auto-Scroll only if the active group has changed
			if (activeGroupIndex != -1 && activeGroupIndex != _lastActiveGroupIndex)
			{
				_lastActiveGroupIndex = activeGroupIndex;

				float itemHeight = 22f; // Approx height per row
				float topY = activeGroupIndex * itemHeight;
				float bottomY = topY + itemHeight;

				float visibleHeight = position.height - 140f;
				if (visibleHeight < itemHeight) visibleHeight = itemHeight;

				if (bottomY > _scrollPosition.y + visibleHeight)
				{
					_scrollPosition.y = bottomY - visibleHeight;
				}
				else if (topY < _scrollPosition.y)
				{
					_scrollPosition.y = topY;
				}
			}

			using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition, EditorStyles.helpBox))
			{
				_scrollPosition = scroll.scrollPosition;

				if (_groupedSteps.Count == 0)
				{
					EditorGUILayout.LabelField("No tasks to process.", EditorStyles.centeredGreyMiniLabel);
				}
				else
				{
					foreach (var group in _groupedSteps)
					{
						int minIndex = group.Indices[0];
						int maxIndex = group.Indices[group.Indices.Count - 1];
						int count = group.Indices.Count;

						int finishedInGroup = group.Indices.Count(idx => idx < rawProcessedCount);

						// Check errors
						string errorMsg = null;
						bool hasGroupError = false;

						foreach (int idx in group.Indices)
						{
							if (_stepErrors.ContainsKey(idx))
							{
								hasGroupError = true;
								if (errorMsg == null) errorMsg = _stepErrors[idx]; // Grab first error
								else errorMsg += "\n" + _stepErrors[idx]; // Append others
							}
						}

						double groupDuration = 0;
						foreach (int idx in group.Indices)
						{
							if (idx < rawProcessedCount)
							{
								groupDuration += _stepDurations[idx];
							}
						}
						string durationLabel = (finishedInGroup > 0) ? FormatDuration(groupDuration) : "";

						string label = group.Name;
						if (count > 1)
						{
							if (finishedInGroup < count && finishedInGroup > 0)
								label = $"{group.Name} ({finishedInGroup + 1}/{count})";
							else
								label = $"{group.Name} (x{count})";
						}

						bool isGroupCancelled = _isCancelled && rawCancelledIndex >= minIndex && rawCancelledIndex <= maxIndex;

						if (hasGroupError)
						{
							DrawStepItem(label, _stepFailedStyle, "!", durationLabel, errorMsg);
						}
						else if (isGroupCancelled)
						{
							DrawStepItem(label, _stepCancelledStyle, "X", durationLabel);
						}
						else if (finishedInGroup == count)
						{
							DrawStepItem(label, _stepDoneStyle, "✔", durationLabel);
						}
						else if (rawProcessedCount >= minIndex && rawProcessedCount <= maxIndex)
						{
							DrawStepItem(label, _stepRunningStyle, "▶", durationLabel);
						}
						else if (rawProcessedCount > maxIndex)
						{
							DrawStepItem(label, _stepDoneStyle, "✔", durationLabel);
						}
						else
						{
							DrawStepItem(label, _stepPendingStyle, "•", "");
						}
					}
				}
			}
		}

		private void DrawStepItem(string label, GUIStyle style, string icon, string duration, string tooltip = "")
		{
			EditorGUILayout.BeginHorizontal();

			GUILayout.Label(icon, style, GUILayout.Width(20));

			GUIContent content = new GUIContent(label, tooltip);
			GUILayout.Label(content, style);

			GUILayout.FlexibleSpace();

			if (!string.IsNullOrEmpty(duration))
			{
				GUILayout.Label(duration, _durationStyle);
			}

			EditorGUILayout.EndHorizontal();
		}

		private string FormatDuration(double duration)
		{
			if (duration < 60.0)
			{
				return $"{duration:0.0}s";
			}

			int minutes = (int)(duration / 60);
			double seconds = duration % 60;
			return $"{minutes}m {seconds:0}s";
		}

		private void DrawFooter()
		{
			EditorGUILayout.Space(10);

			if (_isCancelled)
			{
				if (GUILayout.Button("Close", GUILayout.Height(30)))
				{
					Close();
				}
			}
			else if (_queue.IsProcessing)
			{
				GUI.backgroundColor = Color.red;
				if (GUILayout.Button("Cancel Process", GUILayout.Height(30)))
				{
					_cts.Cancel();
				}
				GUI.backgroundColor = Color.white;
			}
			else if (_endTime > 0)
			{
				if (GUILayout.Button("Close", GUILayout.Height(30)))
				{
					Close();
				}
			}
			else
			{
				GUI.backgroundColor = Color.green;
				if (GUILayout.Button("Process", GUILayout.Height(30)))
				{
					StartProcess();
				}
				GUI.backgroundColor = Color.white;
			}
		}

		private void InitStyles()
		{
			if (_stepPendingStyle == null)
			{
				_stepPendingStyle = new GUIStyle(EditorStyles.label);
				_stepPendingStyle.normal.textColor = Color.gray;
			}

			if (_stepRunningStyle == null)
			{
				_stepRunningStyle = new GUIStyle(EditorStyles.label);
				_stepRunningStyle.fontStyle = FontStyle.Bold;
				_stepRunningStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.7f, 1f) : Color.blue;
			}

			if (_stepDoneStyle == null)
			{
				_stepDoneStyle = new GUIStyle(EditorStyles.label);
				_stepDoneStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.green : new Color(0, 0.5f, 0);
			}

			if (_stepCancelledStyle == null)
			{
				_stepCancelledStyle = new GUIStyle(EditorStyles.label);
				_stepCancelledStyle.fontStyle = FontStyle.Bold;
				_stepCancelledStyle.normal.textColor = Color.red;
			}

			if (_stepFailedStyle == null)
			{
				_stepFailedStyle = new GUIStyle(EditorStyles.label);
				_stepFailedStyle.fontStyle = FontStyle.Bold;
				_stepFailedStyle.normal.textColor = new Color(1f, 0.4f, 0.4f); // Light Red/Orange
				_stepFailedStyle.hover.textColor = Color.red;
			}

			if (_durationStyle == null)
			{
				_durationStyle = new GUIStyle(EditorStyles.label);
				_durationStyle.normal.textColor = Color.gray;
				_durationStyle.alignment = TextAnchor.MiddleRight;
			}
		}
	}
}