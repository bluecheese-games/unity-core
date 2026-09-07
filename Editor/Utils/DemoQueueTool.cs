using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	public class DemoQueueTool : EditorWindow
	{
		[MenuItem("Tools/Demo Process Queue")]
		public static void Open()
		{
			GetWindow<DemoQueueTool>("Demo Tool");
		}

		private void OnGUI()
		{
			GUILayout.Label("Standard Execution", EditorStyles.boldLabel);
			if (GUILayout.Button("Start Long Process"))
			{
				StartMyProcess(autoClose: false);
			}

			GUILayout.Space(10);

			GUILayout.Label("Automated Execution", EditorStyles.boldLabel);
			if (GUILayout.Button("Start (Auto-close on Success)"))
			{
				StartMyProcess(autoClose: true);
			}

			GUILayout.Space(10);

			GUILayout.Label("Grouped Execution", EditorStyles.boldLabel);
			if (GUILayout.Button("Start Stacked Tasks"))
			{
				StartStackedProcess();
			}

			GUILayout.Space(10);

			GUILayout.Label("Exception Handling", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Exception (Continue)"))
			{
				StartExceptionProcess(ExceptionBehavior.Continue);
			}
			if (GUILayout.Button("Exception (Cancel)"))
			{
				StartExceptionProcess(ExceptionBehavior.Cancel);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10);

			GUILayout.Label("Parallel Execution", EditorStyles.boldLabel);
			if (GUILayout.Button("Start Parallel Demo"))
			{
				StartParallelProcess();
			}

			GUILayout.Space(10);

			GUILayout.Label("Manual Start Execution", EditorStyles.boldLabel);
			if (GUILayout.Button("Open Manual Process"))
			{
				StartManualProcess();
			}
		}

		private void StartMyProcess(bool autoClose)
		{
			var queue = CreateDemoQueue();

			// Launch with default autoStart = true
			ProcessQueueWindow.Open(queue, "Asset Import Pipeline", () =>
			{
				Debug.Log("Pipeline finished successfully!");
			}, autoClose, autoStart: true);
		}

		private void StartManualProcess()
		{
			var queue = CreateDemoQueue();

			// Launch with autoStart = false
			ProcessQueueWindow.Open(queue, "Manual Pipeline", () =>
			{
				Debug.Log("Manual Pipeline finished!");
			}, autoClose: false, autoStart: false);
		}

		private ProcessQueue CreateDemoQueue()
		{
			var queue = new ProcessQueue();
			queue.EnqueueAction(() => Debug.Log("Validating Assets..."), "Validate Assets")
				 .AddDelay(0.5f)
				 .EnqueueAsync(async (ct) =>
				 {
					 await UniTask.Delay(1000, cancellationToken: ct);
				 }, "Download Remote Config")
				 .AddDelay(0.5f)
				 .EnqueueAction(() => Debug.Log("Processing..."), "Baking Data")
				 .AddDelay(1.5f)
				 .EnqueueAction(() => Debug.Log("Done!"), "Save to Disk");
			return queue;
		}

		private void StartStackedProcess()
		{
			var queue = new ProcessQueue();

			queue.EnqueueAction(() => Debug.Log("Init"), "Initialization");

			for (int i = 0; i < 20; i++)
			{
				queue.EnqueueAsync(async (ct) =>
				{
					await UniTask.Delay(100, cancellationToken: ct);
				}, "Batch Processing");
			}

			queue.AddDelay(0.5f);

			ProcessQueueWindow.Open(queue, "Stacked Task Demo", autoStart: true);
		}

		private void StartExceptionProcess(ExceptionBehavior behavior)
		{
			var queue = new ProcessQueue();
			queue.Behavior = behavior;

			queue.EnqueueAction(() => Debug.Log("Preparing..."), "Preparation");
			queue.AddDelay(0.5f);

			// Task 1: Success
			queue.EnqueueAsync(async (ct) => { await UniTask.Delay(500, cancellationToken: ct); }, "Task A (Success)");

			// Task 2: Fails
			queue.EnqueueAction(() =>
			{
				throw new Exception("Something went terribly wrong in Task B!");
			}, "Task B (Fails)");

			// Task 3: Success (only runs if Continue)
			queue.EnqueueAsync(async (ct) => { await UniTask.Delay(500, cancellationToken: ct); }, "Task C (Success)");

			ProcessQueueWindow.Open(queue, $"Exception Demo ({behavior})", autoStart: true);
		}

		private void StartParallelProcess()
		{
			var queue = new ProcessQueue();
			queue.EnqueueAction(() => Debug.Log("Init Parallel..."), "Initialization");

			// Create 5 concurrent tasks with names
			var parallelTasks = new (string, Func<CancellationToken, UniTask>)[5];
			for (int i = 0; i < 5; i++)
			{
				int id = i;
				parallelTasks[i] = ($"Parallel Job {id + 1}", async (ct) =>
				{
					// Variable delay to show they finish independently
					int delay = UnityEngine.Random.Range(500, 3000);
					await UniTask.Delay(delay, cancellationToken: ct);
					Debug.Log($"Parallel Task {id} finished after {delay}ms");
				}
				);
			}

			queue.EnqueueParallel("Download Items (Parallel)", parallelTasks);
			queue.AddDelay(0.5f);
			queue.EnqueueAction(() => Debug.Log("Done"), "Finish");

			ProcessQueueWindow.Open(queue, "Parallel Execution Demo", autoStart: true);
		}
	}
}
