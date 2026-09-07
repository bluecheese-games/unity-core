using BlueCheese.Core.Editor;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.SanityCheck.Editor
{
	/// <summary>
	/// Optionally runs the sanity check scan once per editor session, the first time the project
	/// is opened (not on every domain reload triggered by script recompilation).
	/// </summary>
	[InitializeOnLoad]
	public static class SanityCheckAutoRunner
	{
		private const string MenuPath = "Tools/Sanity Checks/Auto-Run On Project Open";

		// Give the editor a moment to settle after opening (import/compile churn), then wait for
		// any other background task (asset import, script compilation, other Progress items) to
		// finish before starting the scan, so it doesn't compete for CPU/IO during project startup.
		private const int InitialDelayMs = 5000;
		private const int PollIntervalMs = 500;

		// Project-scoped: EditorPrefs is shared across all projects on this machine, so the key
		// is namespaced with a hash of the project path.
		private static string PrefKey => $"BlueCheese.SanityCheck.{(Application.dataPath?.GetHashCode() ?? 0)}.autoRun";

		// SessionState resets on editor restart but survives domain reloads, so a recompile
		// won't re-trigger the scan within the same editor session.
		private const string SessionKey = "BlueCheese.SanityCheck.autoRunDone";

		public static bool Enabled
		{
			get => EditorPrefs.GetBool(PrefKey, false);
			set => EditorPrefs.SetBool(PrefKey, value);
		}

		static SanityCheckAutoRunner()
		{
			EditorApplication.delayCall += RunOnceIfEnabled;
		}

		private static void RunOnceIfEnabled()
		{
			if (!Enabled) return;
			if (SessionState.GetBool(SessionKey, false)) return;

			SessionState.SetBool(SessionKey, true);

			// A ProcessQueueWindow left open from a previous session (e.g. a manual "Start Scan")
			// gets restored by Unity's saved editor layout on startup, before this hook even runs.
			// Close it so nothing lingers on screen while the auto-run is meant to stay silent.
			CloseStaleWindow();

			WaitThenRun().Forget();
		}

		private static void CloseStaleWindow()
		{
			if (EditorWindow.HasOpenInstances<ProcessQueueWindow>())
			{
				EditorWindow.GetWindow<ProcessQueueWindow>().Close();
			}
		}

		private static async UniTaskVoid WaitThenRun()
		{
			await UniTask.Delay(InitialDelayMs);

			while (IsAnyBackgroundTaskRunning())
			{
				await UniTask.Delay(PollIntervalMs);
			}

			SanityCheckMenu.RunAllInBackground();
		}

		private static bool IsAnyBackgroundTaskRunning()
		{
			if (EditorApplication.isCompiling || EditorApplication.isUpdating) return true;

			foreach (var item in UnityEditor.Progress.EnumerateItems())
			{
				if (item.running) return true;
			}

			return false;
		}

		[MenuItem(MenuPath)]
		private static void ToggleEnabled()
		{
			Enabled = !Enabled;
		}

		[MenuItem(MenuPath, true)]
		private static bool ToggleEnabledValidate()
		{
			Menu.SetChecked(MenuPath, Enabled);
			return true;
		}
	}
}
