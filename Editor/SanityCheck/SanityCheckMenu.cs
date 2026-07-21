//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using BlueCheese.Core.Editor;
using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.SanityCheck.Editor
{
	/// <summary>
	/// Scans for [SanityCheck] classes and runs them, either interactively through a
	/// ProcessQueueWindow or silently in the background (see <see cref="RunAllInBackground"/>).
	/// </summary>
	public static class SanityCheckMenu
	{
		[MenuItem("Tools/Sanity Checks/Start Scan")]
		public static void RunAll()
		{
			var entries = SanityCheckScanner.Scan();
			if (entries.Count == 0)
			{
				Debug.Log("[SanityCheck] No sanity checks found. Tag a static class with [SanityCheck] and add a Run() or RunAsync() method.");
				return;
			}

			var queue = new ProcessQueue { Behavior = ExceptionBehavior.Continue };

			foreach (var entry in entries)
			{
				queue.EnqueueAsync(async ct =>
				{
					double startTime = EditorApplication.timeSinceStartup;
					var result = await entry.InvokeAsync(ct);
					LogResult(entry, result, EditorApplication.timeSinceStartup - startTime);

					if (result.Severity == SanitySeverity.Error)
						throw new Exception(result.Message ?? $"{entry.DisplayName} failed.");
					if (result.Severity == SanitySeverity.Warning)
						queue.ReportWarning(result.Message);
				}, entry.DisplayName);
			}

			ProcessQueueWindow.Open(queue, "Sanity Checks");
		}

		/// <summary>
		/// Runs all checks without ever showing a ProcessQueueWindow. Progress is reported through
		/// Unity's native background task indicator (bottom status bar) instead. If, once finished,
		/// any check reported a Warning or an Error, the ProcessQueueWindow is opened already showing
		/// the final results (see <see cref="ShowResults"/>) — otherwise nothing is shown. Used by the
		/// auto-run-on-project-open hook.
		/// </summary>
		public static void RunAllInBackground()
		{
			// Don't let a stale/leftover ProcessQueueWindow (e.g. restored from the saved editor
			// layout) sit on screen while this is supposed to run silently in the background.
			if (EditorWindow.HasOpenInstances<ProcessQueueWindow>())
			{
				EditorWindow.GetWindow<ProcessQueueWindow>().Close();
			}

			var entries = SanityCheckScanner.Scan();
			if (entries.Count == 0)
			{
				Debug.Log("[SanityCheck] No sanity checks found. Tag a static class with [SanityCheck] and add a Run() or RunAsync() method.");
				return;
			}

			bool hasError = false;
			bool hasWarning = false;
			var results = new List<(SanityCheckEntry entry, SanityCheckResult result, double duration)>();
			var queue = new ProcessQueue { Behavior = ExceptionBehavior.Continue };

			foreach (var entry in entries)
			{
				queue.EnqueueAsync(async ct =>
				{
					double startTime = EditorApplication.timeSinceStartup;
					var result = await entry.InvokeAsync(ct);
					double duration = EditorApplication.timeSinceStartup - startTime;
					LogResult(entry, result, duration);
					results.Add((entry, result, duration));

					if (result.Severity == SanitySeverity.Error)
					{
						hasError = true;
						throw new Exception(result.Message ?? $"{entry.DisplayName} failed.");
					}
					if (result.Severity == SanitySeverity.Warning)
					{
						hasWarning = true;
						queue.ReportWarning(result.Message);
					}
				}, entry.DisplayName);
			}

			int progressId = UnityEditor.Progress.Start("Sanity Checks", $"Running {entries.Count} check(s)...");
			queue.Progressed += p => UnityEditor.Progress.Report(progressId, p, queue.ProcessingAction ?? string.Empty);

			RunInBackground().Forget();

			async UniTaskVoid RunInBackground()
			{
				try
				{
					await UniTask.SwitchToMainThread();
					UnityEditor.Progress.Report(progressId, 0f, "Sanity Checks");
					await UniTask.WaitForSeconds(0.25f);
					await queue.ProcessAsync();
				}
				catch (Exception e)
				{
					Debug.LogError($"[SanityCheck] Background scan failed: {e}");
				}
				finally
				{
					UnityEditor.Progress.Finish(progressId, hasError ? UnityEditor.Progress.Status.Failed : UnityEditor.Progress.Status.Succeeded);

					if (hasError || hasWarning)
					{
						ShowResults(results);
					}
				}
			}
		}

		/// <summary>
		/// Shows already-computed results in a ProcessQueueWindow without re-running anything —
		/// the window opens already in its final state (correct icons, tooltips and real durations).
		/// </summary>
		private static void ShowResults(List<(SanityCheckEntry entry, SanityCheckResult result, double duration)> results)
		{
			var steps = results.Select(r => new ProcessQueueWindow.CompletedStep(
				r.entry.DisplayName,
				r.duration,
				errorMessage: r.result.Severity == SanitySeverity.Error ? (r.result.Message ?? $"{r.entry.DisplayName} failed.") : null,
				warningMessage: r.result.Severity == SanitySeverity.Warning ? r.result.Message : null
			)).ToList();

			ProcessQueueWindow.ShowCompleted("Sanity Checks", steps);
		}

		private static void LogResult(SanityCheckEntry entry, SanityCheckResult result, double duration)
		{
			string message = $"[SanityCheck] {entry.DisplayName}: {result.Severity} ({duration:0.00}s)" + (string.IsNullOrEmpty(result.Message) ? "" : $" — {result.Message}");

			switch (result.Severity)
			{
				case SanitySeverity.Warning:
					Debug.LogWarning(message);
					break;
				case SanitySeverity.Error:
					Debug.LogError(message);
					break;
				default:
					Debug.Log(message);
					break;
			}
		}
	}
}
