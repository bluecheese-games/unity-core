//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using System;
using BlueCheese.Core.Utils;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// Editor-only recorder for development metrics such as boot time, compilation time, and enter-play-mode time.
	/// </summary>
	[InitializeOnLoad]
	public static class DevMetricRecorder
	{
		private const string AssetFolder = "Assets/_Local";
		private const string AssetPath = AssetFolder + "/DevMetricData.asset";

		private const string BootRecordedKey = "DevMetric_BootRecorded";
		private const string PlayStartKey = "DevMetric_PlayStart_t";
		private const string PlayHadStartKey = "DevMetric_PlayHadStart_b";
		private const string PlayNoDomainFlagKey = "DevMetric_PlayNoDomain_b";

		// Metric keys
		private const string BootMetricName = "ProjectBootTime_s";
		private const string CompileMetricName = "CompilationTime_s";
		private const string EnterPlayWithDomainMetric = "EnterPlayMode_DomainReload_s";
		private const string EnterPlayNoDomainMetric = "EnterPlayMode_NoDomainReload_s";

		private static DevMetricDataAsset _data;
		private static bool _dataFileJustCreated = false;
		private static double _compileStartTime = -1.0;

		// Deferred cloud username refresh
		private static bool _cloudNameDone;
		private static double _cloudNameDeadline; // stop polling after this time

		// An event for other systems to hook into
		public static event Action<string, double> OnMetricRecorded;

		[InitializeOnLoadMethod]
		private static void EnsureOnLoadMethod() { /* no-op: forces class init */ }

		static DevMetricRecorder()
		{
			EnsureAssetExists();          // creates asset & sets provisional username
			ScheduleCloudUserNameRefresh(); // attempts to improve username after services are ready
			HookCompilation();
			HookPlayMode();
			if (!_dataFileJustCreated)
			{
				// Record boot time only if the data file already existed (not on first run)
				RecordBootOncePerSession();
			}

			DevMetricRuntime.OnMetricRecorded += Record;
		}

		public static void Record(string metricName, double valueSeconds)
		{
			if (string.IsNullOrEmpty(metricName) || valueSeconds < 0) return;
			if (!EnsureAssetLoaded()) return;
			_data.AddSample(metricName, valueSeconds);
			OnMetricRecorded?.Invoke(metricName, valueSeconds);
			SaveAssetSoon();
		}

		#region Boot
		private static void RecordBootOncePerSession()
		{
			if (SessionState.GetBool(BootRecordedKey, false)) return;
			double bootSeconds = EditorApplication.timeSinceStartup; // from editor launch
			Record(BootMetricName, bootSeconds);
			SessionState.SetBool(BootRecordedKey, true);
		}
		#endregion

		#region Compilation
		private static void HookCompilation()
		{
			CompilationPipeline.compilationStarted += _ => { _compileStartTime = EditorApplication.timeSinceStartup; };
			CompilationPipeline.compilationFinished += _ =>
			{
				if (_compileStartTime >= 0)
				{
					double dt = EditorApplication.timeSinceStartup - _compileStartTime;
					_compileStartTime = -1.0;
					Record(CompileMetricName, dt);
				}
			};
		}
		#endregion

		#region Enter Play Mode
		private static void HookPlayMode()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			switch (state)
			{
				case PlayModeStateChange.ExitingEditMode:
					{
						SessionState.SetFloat(PlayStartKey, (float)EditorApplication.timeSinceStartup);
						SessionState.SetBool(PlayHadStartKey, true);

						bool domainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled &&
							(EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) != 0;

						SessionState.SetBool(PlayNoDomainFlagKey, domainReloadDisabled);
						break;
					}
				case PlayModeStateChange.EnteredPlayMode:
					{
						if (SessionState.GetBool(PlayHadStartKey, false))
						{
							float t0 = SessionState.GetFloat(PlayStartKey, -1f);
							if (t0 >= 0f)
							{
								double dt = EditorApplication.timeSinceStartup - t0;
								bool noDomain = SessionState.GetBool(PlayNoDomainFlagKey, false);
								Record(noDomain ? EnterPlayNoDomainMetric : EnterPlayWithDomainMetric, dt);
							}
							SessionState.EraseFloat(PlayStartKey);
							SessionState.EraseBool(PlayHadStartKey);
							SessionState.EraseBool(PlayNoDomainFlagKey);
						}
						break;
					}
			}
		}
		#endregion

		#region Asset plumbing + username logic
		private static bool EnsureAssetLoaded()
		{
			if (_data != null) return true;
			_data = AssetDatabase.LoadAssetAtPath<DevMetricDataAsset>(AssetPath);
			if (_data == null) EnsureAssetExists();
			return _data != null;
		}

		private static void EnsureAssetExists()
		{
			if (!AssetDatabase.IsValidFolder(AssetFolder))
				AssetDatabase.CreateFolder("Assets", "_Local");

			_dataFileJustCreated = false;

			_data = AssetDatabase.LoadAssetAtPath<DevMetricDataAsset>(AssetPath);
			if (_data == null)
			{
				_data = ScriptableObject.CreateInstance<DevMetricDataAsset>();
				// Fill metadata with safe defaults first
				_data.projectName = Application.productName;
				_data.userName = SafeEnvironmentUserName(); // provisional (not "anonymous")
				AssetDatabase.CreateAsset(_data, AssetPath);
				AssetDatabase.SaveAssets();
				_dataFileJustCreated = true;
			}
			else
			{
				bool dirty = false;
				if (string.IsNullOrEmpty(_data.projectName))
				{
					_data.projectName = Application.productName;
					dirty = true;
				}
				if (string.IsNullOrEmpty(_data.userName) || IsAnonymousLike(_data.userName))
				{
					_data.userName = SafeEnvironmentUserName(); // provisional
					dirty = true;
				}
				if (dirty)
				{
					EditorUtility.SetDirty(_data);
					AssetDatabase.SaveAssets();
					_dataFileJustCreated = true;
				}
			}
		}

		/// <summary>
		/// Polls briefly after startup to replace the provisional username with Unity Cloud username/email
		/// once Services have finished logging in. Stops after ~15s or after a successful update.
		/// </summary>
		private static void ScheduleCloudUserNameRefresh()
		{
			_cloudNameDone = false;
			_cloudNameDeadline = EditorApplication.timeSinceStartup + 15.0; // seconds

			EditorApplication.update -= CloudNameUpdater;
			EditorApplication.update += CloudNameUpdater;
		}

		private static void CloudNameUpdater()
		{
			if (_cloudNameDone) { EditorApplication.update -= CloudNameUpdater; return; }
			if (_data == null) { EditorApplication.update -= CloudNameUpdater; return; }

			// Try to get a good cloud username
			if (TryGetCloudUserName(out var cloudName))
			{
				if (!string.IsNullOrEmpty(cloudName) && !IsAnonymousLike(cloudName) && !string.Equals(_data.userName, cloudName, StringComparison.Ordinal))
				{
					_data.userName = cloudName;
					EditorUtility.SetDirty(_data);
					AssetDatabase.SaveAssets();
				}
				_cloudNameDone = true;
				EditorApplication.update -= CloudNameUpdater;
				return;
			}

			// timeout
			if (EditorApplication.timeSinceStartup >= _cloudNameDeadline)
			{
				_cloudNameDone = true;
				EditorApplication.update -= CloudNameUpdater;
			}
		}

		/// <summary>
		/// Best-effort fetch of Unity Cloud account identity (display name or email).
		/// Returns false if not logged in / not available yet.
		/// </summary>
		private static bool TryGetCloudUserName(out string nameOrEmail)
		{
			nameOrEmail = null;

			try
			{
				// Use UnityEditor.Connect without creating a hard compile dependency on exact APIs.
				var connectType = Type.GetType("UnityEditor.Connect.UnityConnect, UnityEditor");
				if (connectType == null) return false;

				var instProp = connectType.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				var instance = instProp?.GetValue(null);
				if (instance == null) return false;

				bool loggedIn = false;
				var loggedInProp = connectType.GetProperty("loggedIn", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				if (loggedInProp != null)
					loggedIn = (bool)loggedInProp.GetValue(instance);

				if (!loggedIn) return false;

				var userInfoProp = connectType.GetProperty("userInfo", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				var userInfo = userInfoProp?.GetValue(instance);
				if (userInfo == null) return false;

				var uiType = userInfo.GetType();

				string displayName = GetStringProp(uiType, userInfo, "displayName");
				string userName = GetStringProp(uiType, userInfo, "userName");      // sometimes present
				string primaryMail = GetStringProp(uiType, userInfo, "primaryEmail");  // sometimes present

				// Prefer displayName, else email, else userName
				string candidate =
					FirstNonEmpty(displayName, primaryMail, userName);

				if (string.IsNullOrEmpty(candidate) || IsAnonymousLike(candidate))
					return false;

				nameOrEmail = candidate;
				return true;
			}
			catch
			{
				// Unity Connect API shape can vary; failing here is fine, we�ll keep provisional name
				return false;
			}
		}

		private static string FirstNonEmpty(params string[] values)
		{
			foreach (var v in values)
				if (!string.IsNullOrEmpty(v)) return v;
			return null;
		}

		private static string GetStringProp(Type t, object obj, string prop)
		{
			try
			{
				var p = t.GetProperty(prop, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				if (p != null && p.PropertyType == typeof(string))
					return (string)p.GetValue(obj);
			}
			catch { }
			return null;
		}

		private static bool IsAnonymousLike(string s)
		{
			if (string.IsNullOrEmpty(s)) return true;
			return string.Equals(s, "anonymous", StringComparison.OrdinalIgnoreCase) ||
				   string.Equals(s, "anon", StringComparison.OrdinalIgnoreCase);
		}

		private static string SafeEnvironmentUserName()
		{
			// Fallback that won�t be �anonymous�
			try
			{
				var env = Environment.UserName;
				return string.IsNullOrEmpty(env) ? "LocalUser" : env;
			}
			catch { return "LocalUser"; }
		}

		private static bool _saveQueued;
		private static void SaveAssetSoon()
		{
			if (_saveQueued) return;
			_saveQueued = true;
			EditorApplication.delayCall += () =>
			{
				_saveQueued = false;
				if (_data != null)
				{
					EditorUtility.SetDirty(_data);
					AssetDatabase.SaveAssets();
				}
			};
		}
		#endregion
	}
}
