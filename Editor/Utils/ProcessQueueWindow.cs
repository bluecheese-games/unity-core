//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using BlueCheese.Core.Utils;

namespace BlueCheese.Core.Editor
{
	public class ProcessQueueWindow : EditorWindow
	{
		private static readonly string[] StatusClasses =
		{
			"pqw-status--pending", "pqw-status--running", "pqw-status--done",
			"pqw-status--cancelled", "pqw-status--failed", "pqw-status--warning"
		};

		private ProcessQueue _queue;
		private CancellationTokenSource _cts;
		private Action _onComplete;

		// True when driven by a live ProcessQueue (Open). False when showing an already-known,
		// completed set of results with no queue to run (ShowCompleted) — e.g. a background scan
		// replayed for display without re-executing anything.
		private bool _liveMode;

		private List<QueueStep> _allSteps = new List<QueueStep>();
		private List<GroupedStep> _groupedSteps = new List<GroupedStep>();
		private List<GroupRowView> _groupRows = new List<GroupRowView>();

		private bool _isCancelled;
		private bool _autoClose;

		// Time tracking
		[SerializeField] private double _startTime;
		[SerializeField] private double _endTime;

		// Per-step duration tracking
		private double[] _stepDurations;
		private double _lastStepFinishTime;

		// Error / warning tracking
		private Dictionary<int, string> _stepErrors = new Dictionary<int, string>();
		private Dictionary<int, string> _stepWarnings = new Dictionary<int, string>();

		// Parallel sub-task tracking: Key = MainIndex, Value = Set of finished SubIndices
		private Dictionary<int, HashSet<int>> _finishedSubTasks = new Dictionary<int, HashSet<int>>();

		// Auto-scroll tracking
		private int _lastActiveGroupIndex = -1;

		// UI Toolkit elements
		private Label _statusLabel;
		private Label _timerLabel;
		private ProgressBar _progressBar;
		private Label _tasksLabel;
		private ScrollView _scrollView;
		private VisualElement _tasksContainer;
		private Label _emptyLabel;
		private Button _footerButton;
		private Action _footerAction;
		private IVisualElementScheduledItem _timerTick;

		// These read from the live queue when running interactively, or fall back to static
		// "already completed" values when showing pre-computed results (ShowCompleted).
		private bool CurrentIsProcessing => _liveMode && _queue.IsProcessing;
		private float CurrentProgress => _liveMode ? _queue.Progress : 1f;
		private int CurrentTotalCount => _liveMode ? _queue.TotalCount : _allSteps.Count;
		private int CurrentCount => _liveMode ? _queue.Count : 0;
		private string CurrentProcessingAction => _liveMode ? _queue.ProcessingAction : null;

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
			public bool IsParallel;
			public string[] SubStepNames; // Only valid if Count == 1 (non-grouped parallel items)
			public List<int> Indices = new List<int>();
		}

		private class GroupRowView
		{
			public VisualElement Root;
			public Label IconLabel;
			public Label NameLabel;
			public Label DurationLabel;
			public List<(Label icon, Label name)> SubRows;
		}

		public static ProcessQueueWindow Open(ProcessQueue queue, string title, Action onComplete = null, bool autoClose = false, bool autoStart = true)
		{
			var window = GetWindow<ProcessQueueWindow>(true, title, true);
			window.Initialize(queue, onComplete, autoClose, autoStart);
			window.Show();
			return window;
		}

		/// <summary>
		/// Shows a static, already-finished set of results — no queue is run, nothing executes.
		/// Useful to display the outcome of work that already happened elsewhere (e.g. a background
		/// scan) without re-running it just to populate the UI.
		/// </summary>
		public static ProcessQueueWindow ShowCompleted(string title, IReadOnlyList<CompletedStep> steps)
		{
			var window = GetWindow<ProcessQueueWindow>(true, title, true);
			window.InitializeCompleted(steps);
			window.Show();
			return window;
		}

		/// <summary>
		/// One already-finished step, as displayed by <see cref="ShowCompleted"/>.
		/// </summary>
		public readonly struct CompletedStep
		{
			public readonly string Name;
			public readonly double Duration;
			public readonly string ErrorMessage;
			public readonly string WarningMessage;

			public CompletedStep(string name, double duration, string errorMessage = null, string warningMessage = null)
			{
				Name = name;
				Duration = duration;
				ErrorMessage = errorMessage;
				WarningMessage = warningMessage;
			}
		}

		#region Layout

		private void OnEnable()
		{
			BuildLayout();
			LoadStyles();
		}

		private void BuildLayout()
		{
			rootVisualElement.Clear();
			rootVisualElement.AddToClassList("pqw-root");

			var header = new VisualElement();
			header.AddToClassList("pqw-header");

			_statusLabel = new Label("No active process.");
			_statusLabel.AddToClassList("pqw-header__status");
			header.Add(_statusLabel);

			_timerLabel = new Label();
			_timerLabel.AddToClassList("pqw-header__timer");
			header.Add(_timerLabel);

			rootVisualElement.Add(header);

			_progressBar = new ProgressBar { lowValue = 0f, highValue = 1f };
			_progressBar.AddToClassList("pqw-progress");
			rootVisualElement.Add(_progressBar);

			_tasksLabel = new Label("Tasks");
			_tasksLabel.AddToClassList("pqw-tasks-label");
			rootVisualElement.Add(_tasksLabel);

			_scrollView = new ScrollView();
			_scrollView.AddToClassList("pqw-scroll");
			rootVisualElement.Add(_scrollView);

			_emptyLabel = new Label("No tasks to process.");
			_emptyLabel.AddToClassList("pqw-empty-label");
			_scrollView.Add(_emptyLabel);

			_tasksContainer = new VisualElement();
			_scrollView.Add(_tasksContainer);

			_footerButton = new Button(() => _footerAction?.Invoke());
			_footerButton.AddToClassList("pqw-footer-button");
			rootVisualElement.Add(_footerButton);

			// Nothing to show until Initialize() is called (e.g. window restored from a saved
			// layout after an editor restart, with no queue attached).
			SetContentVisible(false);
		}

		private void LoadStyles()
		{
			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
				"Assets/unity-core/Editor/Utils/Styles/ProcessQueueWindow.uss");
			if (styleSheet != null)
				rootVisualElement.styleSheets.Add(styleSheet);
		}

		private void SetContentVisible(bool visible)
		{
			var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			_progressBar.style.display = display;
			_tasksLabel.style.display = display;
			_scrollView.style.display = display;
			_footerButton.style.display = display;
		}

		#endregion

		#region Initialize / Run

		private void Initialize(ProcessQueue queue, Action onComplete, bool autoClose, bool autoStart)
		{
			_queue = queue;
			_liveMode = true;
			_cts = new CancellationTokenSource();
			_onComplete = onComplete;
			_isCancelled = false;
			_autoClose = autoClose;

			_startTime = 0;
			_endTime = 0;
			_lastStepFinishTime = 0;
			_lastActiveGroupIndex = -1;

			_allSteps = _queue.GetPendingSteps().ToList();
			_stepDurations = new double[_allSteps.Count];
			_stepErrors.Clear();
			_stepWarnings.Clear();
			_finishedSubTasks.Clear();

			GroupSteps();
			BuildStepRows();

			_queue.Progressed += OnQueueProgress;
			_queue.StepFailed += OnStepFailed;
			_queue.StepWarning += OnStepWarning;
			_queue.ParallelSubProgress += OnParallelSubProgress;

			SetContentVisible(true);
			RefreshView();

			if (autoStart)
			{
				StartProcess();
			}
		}

		private void InitializeCompleted(IReadOnlyList<CompletedStep> steps)
		{
			_queue = null;
			_liveMode = false;
			_cts = null;
			_onComplete = null;
			_isCancelled = false;
			_autoClose = false;
			_lastActiveGroupIndex = -1;

			_allSteps = steps.Select(s => new QueueStep(s.Name, isParallel: false)).ToList();
			_stepDurations = steps.Select(s => s.Duration).ToArray();
			_stepErrors = new Dictionary<int, string>();
			_stepWarnings = new Dictionary<int, string>();
			_finishedSubTasks.Clear();

			for (int i = 0; i < steps.Count; i++)
			{
				if (!string.IsNullOrEmpty(steps[i].ErrorMessage)) _stepErrors[i] = steps[i].ErrorMessage;
				else if (!string.IsNullOrEmpty(steps[i].WarningMessage)) _stepWarnings[i] = steps[i].WarningMessage;
			}

			double totalDuration = _stepDurations.Sum();
			_endTime = EditorApplication.timeSinceStartup;
			_startTime = _endTime - totalDuration;

			GroupSteps();
			BuildStepRows();

			SetContentVisible(true);
			RefreshView();
		}

		private void StartProcess()
		{
			_startTime = EditorApplication.timeSinceStartup;
			_lastStepFinishTime = _startTime;

			_timerTick?.Pause();
			_timerTick = rootVisualElement.schedule.Execute(UpdateHeader).Every(100);

			RunProcess().Forget();
		}

		private void OnQueueProgress(float progress)
		{
			double now = EditorApplication.timeSinceStartup;
			int processedCount = Mathf.RoundToInt(progress * _queue.TotalCount);
			int finishedIndex = processedCount - 1;

			if (finishedIndex >= 0 && finishedIndex < _stepDurations.Length)
			{
				_stepDurations[finishedIndex] = now - _lastStepFinishTime;
			}

			_lastStepFinishTime = now;
			RefreshView();
		}

		private void OnStepFailed(int index, Exception ex)
		{
			if (!_stepErrors.ContainsKey(index))
			{
				_stepErrors[index] = ex.Message;
			}
			RefreshView();
		}

		private void OnStepWarning(int index, string message)
		{
			if (!_stepWarnings.ContainsKey(index))
			{
				_stepWarnings[index] = message;
			}
			RefreshView();
		}

		private void OnParallelSubProgress(int mainIndex, int subIndex)
		{
			if (!_finishedSubTasks.ContainsKey(mainIndex))
			{
				_finishedSubTasks[mainIndex] = new HashSet<int>();
			}
			_finishedSubTasks[mainIndex].Add(subIndex);
			RefreshView();
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
				var step = _allSteps[i];
				string name = step.Name;

				if (IsIgnoredStep(name)) continue;

				// Only group consecutive items if they match name/parallel AND aren't complex parallel tasks
				// We generally don't group parallel tasks if they have sub-steps to display
				bool distinctParallel = step.IsParallel && step.SubStepNames != null && step.SubStepNames.Length > 0;

				if (current != null && !distinctParallel && current.Name == name && current.IsParallel == step.IsParallel && current.SubStepNames == null)
				{
					current.Indices.Add(i);
				}
				else
				{
					current = new GroupedStep
					{
						Name = name,
						IsParallel = step.IsParallel,
						SubStepNames = step.SubStepNames
					};
					current.Indices.Add(i);
					_groupedSteps.Add(current);
				}
			}
		}

		private async UniTaskVoid RunProcess()
		{
			if (_queue.Count == 0)
			{
				_onComplete?.Invoke();
				_endTime = EditorApplication.timeSinceStartup;
				StopTimerTick();

				if (_autoClose && !_isCancelled)
				{
					Close();
					return;
				}
				RefreshView();
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

				StopTimerTick();

				if (_queue != null)
				{
					_queue.Progressed -= OnQueueProgress;
					_queue.StepFailed -= OnStepFailed;
					_queue.StepWarning -= OnStepWarning;
					_queue.ParallelSubProgress -= OnParallelSubProgress;
				}

				_cts?.Dispose();
				_cts = null;

				if (!(_autoClose && !_isCancelled))
				{
					RefreshView();
				}
			}
		}

		private void StopTimerTick()
		{
			_timerTick?.Pause();
			_timerTick = null;
		}

		private void OnDisable()
		{
			StopTimerTick();

			if (_queue != null)
			{
				_queue.Progressed -= OnQueueProgress;
				_queue.StepFailed -= OnStepFailed;
				_queue.StepWarning -= OnStepWarning;
				_queue.ParallelSubProgress -= OnParallelSubProgress;
			}

			if (_cts != null)
			{
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}
		}

		#endregion

		#region View

		private void RefreshView()
		{
			UpdateHeader();
			UpdateProgressBar();
			UpdateGroups();
			UpdateFooter();
		}

		private void UpdateHeader()
		{
			_statusLabel.RemoveFromClassList("pqw-header__status--cancelled");
			if (_isCancelled) _statusLabel.AddToClassList("pqw-header__status--cancelled");

			_statusLabel.text = HeaderStatusText();
			_timerLabel.text = FormatDuration(TotalTime);
		}

		private string HeaderStatusText()
		{
			if (_isCancelled) return "Process Cancelled";
			if (CurrentIsProcessing) return $"Processing: {CurrentProcessingAction}...";
			if (_endTime > 0) return (_allSteps.Count == 0) ? "Queue Empty (Done)" : "Process Complete!";
			return (_allSteps.Count == 0) ? "Queue is Empty" : "Ready to Start";
		}

		private void UpdateProgressBar()
		{
			int totalVisible = _groupedSteps.Sum(g => g.Indices.Count);

			if (totalVisible == 0)
			{
				_progressBar.value = (_endTime > 0) ? 1f : 0f;
				_progressBar.title = _endTime > 0 ? "Completed" : "Empty";
				return;
			}

			int rawProcessedCount = Mathf.RoundToInt(CurrentProgress * CurrentTotalCount);
			int completedVisible = _groupedSteps.Sum(g => g.Indices.Count(idx => idx < rawProcessedCount));

			_progressBar.value = (float)completedVisible / totalVisible;
			_progressBar.title = _isCancelled ? "Cancelled" : $"{completedVisible} / {totalVisible}";
		}

		private void BuildStepRows()
		{
			_tasksContainer.Clear();
			_groupRows.Clear();

			bool hasSteps = _groupedSteps.Count > 0;
			_emptyLabel.style.display = hasSteps ? DisplayStyle.None : DisplayStyle.Flex;
			_tasksContainer.style.display = hasSteps ? DisplayStyle.Flex : DisplayStyle.None;
			if (!hasSteps) return;

			foreach (var group in _groupedSteps)
			{
				var groupRoot = new VisualElement();
				groupRoot.AddToClassList("pqw-group");

				var row = new VisualElement();
				row.AddToClassList("pqw-group-row");

				var icon = new Label();
				icon.AddToClassList("pqw-group-row__icon");
				row.Add(icon);

				var name = new Label();
				name.AddToClassList("pqw-group-row__name");
				row.Add(name);

				var duration = new Label();
				duration.AddToClassList("pqw-group-row__duration");
				row.Add(duration);

				groupRoot.Add(row);

				List<(Label icon, Label name)> subRows = null;
				if (group.SubStepNames != null && group.SubStepNames.Length > 0)
				{
					subRows = new List<(Label icon, Label name)>();
					foreach (var subStepName in group.SubStepNames)
					{
						var subRow = new VisualElement();
						subRow.AddToClassList("pqw-sub-row");

						var subIcon = new Label();
						subIcon.AddToClassList("pqw-sub-row__icon");
						subRow.Add(subIcon);

						var subName = new Label(subStepName);
						subName.AddToClassList("pqw-sub-row__name");
						subRow.Add(subName);

						groupRoot.Add(subRow);
						subRows.Add((subIcon, subName));
					}
				}

				_tasksContainer.Add(groupRoot);
				_groupRows.Add(new GroupRowView
				{
					Root = groupRoot,
					IconLabel = icon,
					NameLabel = name,
					DurationLabel = duration,
					SubRows = subRows
				});
			}
		}

		private void UpdateGroups()
		{
			if (_groupedSteps.Count == 0) return;

			int rawProcessedCount = Mathf.RoundToInt(CurrentProgress * CurrentTotalCount);
			int dequeuedCount = CurrentTotalCount - CurrentCount;
			int rawCancelledIndex = -1;
			if (_isCancelled)
			{
				rawCancelledIndex = (dequeuedCount > rawProcessedCount) ? dequeuedCount - 1 : dequeuedCount;
			}

			int activeGroupIndex = -1;
			if (CurrentIsProcessing && !_isCancelled)
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

			for (int i = 0; i < _groupedSteps.Count; i++)
			{
				UpdateGroupRow(i, rawProcessedCount, rawCancelledIndex);
			}

			if (activeGroupIndex != -1 && activeGroupIndex != _lastActiveGroupIndex)
			{
				_lastActiveGroupIndex = activeGroupIndex;
				_scrollView.ScrollTo(_groupRows[activeGroupIndex].Root);
			}
		}

		private void UpdateGroupRow(int index, int rawProcessedCount, int rawCancelledIndex)
		{
			var group = _groupedSteps[index];
			var view = _groupRows[index];

			int minIndex = group.Indices[0];
			int maxIndex = group.Indices[group.Indices.Count - 1];
			int count = group.Indices.Count;

			int finishedInGroup = group.Indices.Count(idx => idx < rawProcessedCount);

			string errorMsg = null;
			bool hasGroupError = false;
			foreach (int idx in group.Indices)
			{
				if (_stepErrors.TryGetValue(idx, out var err))
				{
					hasGroupError = true;
					errorMsg = errorMsg == null ? err : errorMsg + "\n" + err;
				}
			}

			string warningMsg = null;
			bool hasGroupWarning = false;
			foreach (int idx in group.Indices)
			{
				if (_stepWarnings.TryGetValue(idx, out var warn))
				{
					hasGroupWarning = true;
					warningMsg = warningMsg == null ? warn : warningMsg + "\n" + warn;
				}
			}

			double groupDuration = 0;
			foreach (int idx in group.Indices)
			{
				if (idx < rawProcessedCount) groupDuration += _stepDurations[idx];
			}
			string durationLabel = (finishedInGroup > 0) ? FormatDuration(groupDuration) : "";

			string label = group.Name;
			if (count > 1)
			{
				label = (finishedInGroup < count && finishedInGroup > 0)
					? $"{group.Name} ({finishedInGroup + 1}/{count})"
					: $"{group.Name} (x{count})";
			}

			bool isGroupCancelled = _isCancelled && rawCancelledIndex >= minIndex && rawCancelledIndex <= maxIndex;
			bool isRunning = (rawProcessedCount >= minIndex && rawProcessedCount <= maxIndex) && !isGroupCancelled;
			bool isDone = (finishedInGroup == count) || (rawProcessedCount > maxIndex);

			string icon;
			string statusClass;
			string tooltip = "";

			if (hasGroupError) { icon = "!"; statusClass = "pqw-status--failed"; tooltip = errorMsg; }
			else if (hasGroupWarning) { icon = "⚠"; statusClass = "pqw-status--warning"; tooltip = warningMsg; }
			else if (isGroupCancelled) { icon = "X"; statusClass = "pqw-status--cancelled"; }
			else if (isDone) { icon = "✔"; statusClass = "pqw-status--done"; }
			else if (isRunning) { icon = "▶"; statusClass = "pqw-status--running"; }
			else { icon = group.IsParallel ? "||" : "•"; statusClass = "pqw-status--pending"; }

			view.IconLabel.text = icon;
			view.NameLabel.text = label;
			view.DurationLabel.text = durationLabel;
			ApplyStatusClass(view.IconLabel, statusClass);
			ApplyStatusClass(view.NameLabel, statusClass);
			view.IconLabel.tooltip = tooltip;
			view.NameLabel.tooltip = tooltip;

			if (view.SubRows == null) return;

			for (int s = 0; s < view.SubRows.Count; s++)
			{
				var (subIcon, subName) = view.SubRows[s];

				bool subDone = isDone;
				if (!subDone && isRunning && _finishedSubTasks.TryGetValue(minIndex, out var finishedSet))
				{
					subDone = finishedSet.Contains(s);
				}

				string subIconText;
				string subStatusClass;
				if (subDone) { subIconText = "✔"; subStatusClass = "pqw-status--done"; }
				else if (isRunning) { subIconText = "•"; subStatusClass = "pqw-status--running"; }
				else { subIconText = "-"; subStatusClass = "pqw-status--pending"; }

				subIcon.text = subIconText;
				ApplyStatusClass(subIcon, subStatusClass);
				ApplyStatusClass(subName, subStatusClass);
			}
		}

		private void UpdateFooter()
		{
			_footerButton.RemoveFromClassList("pqw-footer-button--start");
			_footerButton.RemoveFromClassList("pqw-footer-button--cancel");

			if (_isCancelled)
			{
				_footerButton.text = "Close";
				_footerAction = Close;
			}
			else if (CurrentIsProcessing)
			{
				_footerButton.text = "Cancel Process";
				_footerButton.AddToClassList("pqw-footer-button--cancel");
				_footerAction = () => _cts?.Cancel();
			}
			else if (_endTime > 0)
			{
				_footerButton.text = "Close";
				_footerAction = Close;
			}
			else
			{
				_footerButton.text = "Process";
				_footerButton.AddToClassList("pqw-footer-button--start");
				_footerAction = StartProcess;
			}
		}

		private static void ApplyStatusClass(VisualElement element, string statusClass)
		{
			foreach (var c in StatusClasses) element.RemoveFromClassList(c);
			element.AddToClassList(statusClass);
		}

		private string FormatDuration(double duration)
		{
			if (duration < 60.0) return $"{duration:0.0}s";
			int minutes = (int)(duration / 60);
			double seconds = duration % 60;
			return $"{minutes}m {seconds:0}s";
		}

		#endregion
	}
}
