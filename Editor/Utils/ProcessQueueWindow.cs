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
		private List<string> _allSteps = new List<string>();
		private List<GroupedStep> _groupedSteps = new List<GroupedStep>();
		private Vector2 _scrollPosition;
		private bool _isCancelled;
		private bool _autoClose;

		// Time tracking
		private double _startTime;
		private double _endTime;

		// Styles
		private GUIStyle _stepPendingStyle;
		private GUIStyle _stepRunningStyle;
		private GUIStyle _stepDoneStyle;
		private GUIStyle _stepCancelledStyle;

		private struct GroupedStep
		{
			public string Name;
			public int StartIndex;
			public int Count;
		}

		/// <summary>
		/// Opens the window, configures it, and starts the process immediately.
		/// </summary>
		public static void Process(ProcessQueue queue, string title, Action onComplete = null, bool autoClose = false)
		{
			var window = GetWindow<ProcessQueueWindow>(true, title, true);
			window.Initialize(queue, onComplete, autoClose);
			window.Show();
		}

		private void Initialize(ProcessQueue queue, Action onComplete, bool autoClose)
		{
			_queue = queue;
			_cts = new CancellationTokenSource();
			_isCancelled = false;
			_autoClose = autoClose;

			// Start tracking time (using realtimeSinceStartup so it works in Edit Mode)
			_startTime = Time.realtimeSinceStartup;
			_endTime = 0;

			// Snapshot the steps for the checklist before they are dequeued
			_allSteps = _queue.GetPendingStepNames().ToList();
			GroupSteps();

			// Start the process
			RunProcess(onComplete).Forget();
		}

		private void GroupSteps()
		{
			_groupedSteps.Clear();
			if (_allSteps.Count == 0) return;

			var current = new GroupedStep { Name = _allSteps[0], StartIndex = 0, Count = 1 };

			for (int i = 1; i < _allSteps.Count; i++)
			{
				if (_allSteps[i] == current.Name)
				{
					current.Count++;
				}
				else
				{
					_groupedSteps.Add(current);
					current = new GroupedStep { Name = _allSteps[i], StartIndex = i, Count = 1 };
				}
			}
			_groupedSteps.Add(current);
		}

		private async UniTaskVoid RunProcess(Action onComplete)
		{
			try
			{
				await _queue.ProcessAsync(_cts.Token);
				onComplete?.Invoke();

				// Check auto-close condition before final repaint
				if (_autoClose && !_isCancelled)
				{
					// Stop timer now so we don't count the delay
					_endTime = Time.realtimeSinceStartup;
					// Force a repaint so user sees "Complete" state briefly
					Repaint();

					await UniTask.Delay(1000, cancellationToken: _cts.Token);

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
				// Treat errors as cancellation/stop for visual purposes
				_isCancelled = true;
				Debug.LogError($"Process failed: {e}");
			}
			finally
			{
				// Stop the timer if not already stopped (e.g. cancelled or error)
				if (_endTime == 0)
					_endTime = Time.realtimeSinceStartup;

				_cts?.Dispose();
				_cts = null;

				// Only repaint if we didn't just close the window
				if (!(_autoClose && !_isCancelled))
				{
					Repaint();
				}
			}
		}

		private void OnEnable()
		{
			// Force repaint regularly for smooth progress bar and timer
			EditorApplication.update += Repaint;
		}

		private void OnDisable()
		{
			EditorApplication.update -= Repaint;
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

			// Calculate duration
			double currentEnd = _endTime > 0 ? _endTime : Time.realtimeSinceStartup;
			double duration = Math.Max(0, currentEnd - _startTime);
			string timeStr = $"{duration:0.0}s";

			// Get a single rect for the whole header line
			Rect r = EditorGUILayout.GetControlRect(false, 20);

			// 1. Draw Status (Left Aligned)
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
			else if (_queue.TotalCount > 0 && _queue.Count == 0)
			{
				EditorGUI.LabelField(r, "Process Complete!", EditorStyles.boldLabel);
			}
			else
			{
				EditorGUI.LabelField(r, "Waiting...", EditorStyles.boldLabel);
			}

			// 2. Draw Timer (Right Aligned)
			var timerStyle = new GUIStyle(EditorStyles.label); // Standard label font (not bold) for contrast, or use boldLabel if preferred
			timerStyle.alignment = TextAnchor.MiddleRight;
			EditorGUI.LabelField(r, timeStr, timerStyle);

			EditorGUILayout.Space(5);
		}

		private void DrawProgressBar()
		{
			Rect r = EditorGUILayout.GetControlRect(false, 20);
			float progress = _queue.Progress;

			// If cancelled, show the progress frozen where it stopped
			string label = _isCancelled
				? "Cancelled"
				: $"{_queue.TotalCount - _queue.Count} / {_queue.TotalCount}";

			EditorGUI.ProgressBar(r, progress, label);
			EditorGUILayout.Space(10);
		}

		private void DrawStepList()
		{
			EditorGUILayout.LabelField("Tasks", EditorStyles.boldLabel);

			using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition, EditorStyles.helpBox))
			{
				_scrollPosition = scroll.scrollPosition;

				// Calculate indices to determine state of each item
				int processedCount = Mathf.RoundToInt(_queue.Progress * _queue.TotalCount);
				// Items that have been removed from the queue (either finished or currently running/cancelled)
				int dequeuedCount = _queue.TotalCount - _queue.Count;

				// Determine which index was the "stopper" if cancelled
				int cancelledIndex = -1;
				if (_isCancelled)
				{
					// If we dequeued more than we finished, the last dequeued item failed/cancelled mid-run.
					// Otherwise, we cancelled between steps, so the next pending item is the stopper.
					cancelledIndex = (dequeuedCount > processedCount) ? dequeuedCount - 1 : dequeuedCount;
				}

				foreach (var group in _groupedSteps)
				{
					int groupEnd = group.StartIndex + group.Count - 1;
					string label = group.Count > 1 ? $"{group.Name} (x{group.Count})" : group.Name;

					// Determine state
					if (_isCancelled && cancelledIndex >= group.StartIndex && cancelledIndex <= groupEnd)
					{
						// Cancelled within this group
						DrawStepItem(label, _stepCancelledStyle, "X");
					}
					else if (processedCount > groupEnd)
					{
						// Entire group finished
						DrawStepItem(label, _stepDoneStyle, "✔");
					}
					else if (processedCount >= group.StartIndex && _queue.IsProcessing)
					{
						// Inside this group
						// Show progress in label e.g. "Name (2/5)"
						if (group.Count > 1)
						{
							int currentInGroup = processedCount - group.StartIndex + 1; // 1-based index
							label = $"{group.Name} ({currentInGroup}/{group.Count})";
						}
						DrawStepItem(label, _stepRunningStyle, "▶");
					}
					else if (processedCount >= group.StartIndex && processedCount <= groupEnd && !_queue.IsProcessing && !_isCancelled)
					{
						// Edge case: Queue might be finishing up or between frames, but technically "Active"
						DrawStepItem(label, _stepRunningStyle, "▶");
					}
					else
					{
						// Pending
						DrawStepItem(label, _stepPendingStyle, "•");
					}
				}
			}
		}

		private void DrawStepItem(string label, GUIStyle style, string icon)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(icon, style, GUILayout.Width(20));
			GUILayout.Label(label, style);
			EditorGUILayout.EndHorizontal();
		}

		private void DrawFooter()
		{
			EditorGUILayout.Space(10);

			if (!_isCancelled && _queue.IsProcessing)
			{
				GUI.backgroundColor = Color.red;
				if (GUILayout.Button("Cancel Process", GUILayout.Height(30)))
				{
					_cts.Cancel();
				}
				GUI.backgroundColor = Color.white;
			}
			else
			{
				// Show Close button if finished OR cancelled
				if (GUILayout.Button("Close", GUILayout.Height(30)))
				{
					Close();
				}
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
		}
	}
}