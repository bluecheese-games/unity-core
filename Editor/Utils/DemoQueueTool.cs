//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;
using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;

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
		}

		private void StartMyProcess(bool autoClose)
		{
			var queue = new ProcessQueue();

			// Build the queue
			queue.Enqueue(() => Debug.Log("Validating Assets..."), "Validate Assets")
				 .AddDelay(0.5f, "Checking Database")
				 .Enqueue(async (ct) =>
				 {
					 // Simulate heavier work
					 await UniTask.Delay(1000, cancellationToken: ct);
				 }, "Download Remote Config")
				 .AddDelay(0.5f, "Parsing JSON")
				 .Enqueue(() => Debug.Log("Processing..."), "Baking Data")
				 .AddDelay(1.5f, "Finalizing")
				 .Enqueue(() => Debug.Log("Done!"), "Save to Disk");

			// Launch the generic visualizer
			ProcessQueueWindow.Process(queue, "Asset Import Pipeline", () =>
			{
				Debug.Log("Pipeline finished successfully!");
			}, autoClose);
		}

		private void StartStackedProcess()
		{
			var queue = new ProcessQueue();

			queue.Enqueue(() => Debug.Log("Init"), "Initialization");

			// Add multiple items with same name to demonstrate stacking
			for (int i = 0; i < 20; i++)
			{
				// Local copy for closure, though not strictly needed since we don't use 'i' inside
				queue.Enqueue(async (ct) =>
				{
					// Simulate fast work (100ms)
					await UniTask.Delay(100, cancellationToken: ct);
				}, "Batch Processing");
			}

			queue.AddDelay(0.5f, "Cleanup");

			ProcessQueueWindow.Process(queue, "Stacked Task Demo");
		}
	}
}