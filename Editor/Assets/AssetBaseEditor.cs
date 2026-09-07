using BlueCheese.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{

	[CustomEditor(typeof(AssetBase), editorForChildClasses: true)]
	public class AssetBaseEditor : UnityEditor.Editor
	{
		static bool _foldout = true;

		// Cached list of bundle keys already used in the project. Rebuilt on project changes.
		private static List<string> _bundleKeysCache;
		private static GUIStyle _placeholderStyle;

		static AssetBaseEditor()
		{
			EditorApplication.projectChanged += () => _bundleKeysCache = null;
		}

		private static List<string> BundleKeys => _bundleKeysCache ??= CollectBundleKeys();

		private static GUIStyle PlaceholderStyle => _placeholderStyle ??= new GUIStyle(EditorStyles.label)
		{
			normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.6f) },
			padding = new RectOffset(3, 0, 1, 0),
		};

		private SerializedProperty _nameProperty;
		private SerializedProperty _tagsProperty;
		private SerializedProperty _registerProperty;
		private SerializedProperty _loadModeProperty;
		private SerializedProperty _bundleKeyProperty;

		private AssetBase _asset => target as AssetBase;

		virtual protected void OnEnable()
		{
			_nameProperty = serializedObject.FindProperty(nameof(_asset.Name));
			_tagsProperty = serializedObject.FindProperty(nameof(_asset.Tags));
			_registerProperty = serializedObject.FindProperty(nameof(_asset.RegisterInAssetBank));
			_loadModeProperty = serializedObject.FindProperty(nameof(_asset.LoadMode));
			_bundleKeyProperty = serializedObject.FindProperty(nameof(_asset.BundleKey));

#if UNITY_ADDRESSABLES
			SuppressAddressablesHeader();
#endif
		}

		protected virtual void OnDisable()
		{
#if UNITY_ADDRESSABLES
			RestoreAddressablesHeader();
#endif
		}

		protected override void OnHeaderGUI()
		{
			serializedObject.Update();

			EditorGUILayout.BeginVertical("box");
			_foldout = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout, $"Asset #{_asset.Guid}");
			EditorGUILayout.EndFoldoutHeaderGroup();
			if (_foldout)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(_registerProperty);
				// LoadMode is only meaningful for registered assets, so disable it when unregistered.
				using (new EditorGUI.DisabledScope(!_registerProperty.boolValue))
				{
					EditorGUILayout.PropertyField(_loadModeProperty, GUIContent.none);
				}
				if (GUILayout.Button("Open Asset Bank", GUILayout.Width(150)))
				{
					AssetBank.SelectInProject();
				}
				EditorGUILayout.EndHorizontal();
				DrawBundleKeyField();
				EditorGUILayout.PropertyField(_nameProperty);
				EditorGUILayout.PropertyField(_tagsProperty, true);
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();

			// Regenerate the bank when a bank-relevant field changes (name, load mode, register,
			// bundle key typed, or a tag removed). Deferred so it runs outside the GUI pass.
			if (serializedObject.ApplyModifiedProperties())
				EditorApplication.delayCall += AssetBankGenerator.Regenerate;
		}

		// Bundle key editor, styled like the Tags field: a free-text input plus a picker that lists
		// keys already used in the project. Shown only for Addressables-mode assets; if the
		// Addressables package is missing, a warning is shown in its place.
		private void DrawBundleKeyField()
		{
			if (_loadModeProperty.enumValueIndex != (int)AssetLoadMode.Addressables)
				return;

#if UNITY_ADDRESSABLES
			EditorGUILayout.BeginHorizontal();

			string current = _bundleKeyProperty.stringValue;
			const string controlName = "AssetBank_BundleKeyField";
			GUI.SetNextControlName(controlName);
			// Delayed: the value is committed on Enter / focus loss, not on every keystroke, so the
			// bank regenerates once with the final key instead of during editing.
			string newValue = EditorGUILayout.DelayedTextField("Bundle Key", current);
			if (newValue != current)
				_bundleKeyProperty.stringValue = newValue;

			// Placeholder overlay over the empty input area (hidden while editing).
			if (string.IsNullOrEmpty(current) && GUI.GetNameOfFocusedControl() != controlName)
			{
				var rect = GUILayoutUtility.GetLastRect();
				rect.xMin += EditorGUIUtility.labelWidth + 2f;
				GUI.Label(rect, "Bundle key…", PlaceholderStyle);
			}

			if (GUILayout.Button(new GUIContent(EditorIcon.Plus, "Pick or create a bundle key"),
				EditorStyles.miniButton, GUILayout.Width(22)))
			{
				ShowBundleKeyMenu();
			}

			EditorGUILayout.EndHorizontal();
#else
			EditorGUILayout.HelpBox(
				"Addressables is not installed. Install com.unity.addressables and add UNITY_ADDRESSABLES " +
				"to the Scripting Define Symbols to assign a bundle key.",
				MessageType.Warning);
#endif
		}

		private void ShowBundleKeyMenu()
		{
			string current = _bundleKeyProperty.stringValue?.Trim() ?? string.Empty;
			var keys = BundleKeys;

			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("(default)"), string.IsNullOrEmpty(current), () => SetBundleKey(string.Empty));

			// Offer to create the currently typed key when it is new.
			if (!string.IsNullOrEmpty(current) && !keys.Contains(current))
				menu.AddItem(new GUIContent($"New \"{current}\""), false, () => SetBundleKey(current));

			// Existing keys, filtered by what is currently typed.
			var suggestions = keys
				.Where(key => string.IsNullOrEmpty(current) || key.ToLowerInvariant().Contains(current.ToLowerInvariant()))
				.ToArray();
			if (suggestions.Length > 0)
			{
				menu.AddSeparator(string.Empty);
				foreach (string key in suggestions)
					menu.AddItem(new GUIContent(key), key == _bundleKeyProperty.stringValue, () => SetBundleKey(key));
			}

			menu.ShowAsContext();
		}

		private void SetBundleKey(string key)
		{
			serializedObject.Update();
			_bundleKeyProperty.stringValue = key;
			serializedObject.ApplyModifiedProperties();
			EditorApplication.delayCall += AssetBankGenerator.Regenerate;
		}

		private static List<string> CollectBundleKeys()
		{
			var keys = new SortedSet<string>(System.StringComparer.Ordinal);

			foreach (string path in AssetDatabase.FindAssets($"t:{nameof(AssetBase)}").Select(AssetDatabase.GUIDToAssetPath))
			{
				var asset = AssetDatabase.LoadAssetAtPath<AssetBase>(path);
				if (asset != null && !string.IsNullOrWhiteSpace(asset.BundleKey))
					keys.Add(asset.BundleKey);
			}

#if UNITY_ADDRESSABLES
			// Also list keys for which an Addressables group already exists, even if no asset uses it yet.
			var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
			if (settings != null)
			{
				const string prefix = "AssetBank_";
				foreach (var group in settings.groups)
				{
					if (group != null && group.Name.StartsWith(prefix) && group.Name != AssetBankGenerator.DefaultAddressableGroup)
						keys.Add(group.Name.Substring(prefix.Length));
				}
			}
#endif

			return keys.ToList();
		}

#if UNITY_ADDRESSABLES
		// Unity's Addressables package injects an "Addressable" toggle into every asset header through
		// the static Editor.finishedDefaultHeaderGUI event. For AssetBase that duplicates the module's
		// own load-mode / bundle-key UI, so we detach the Addressables handler while inspecting one and
		// restore it afterwards. Best-effort and version-tolerant: it silently no-ops if the internals
		// change, and any leftover state self-heals on the next domain reload.
		private static readonly List<System.Delegate> _detachedHeaderHandlers = new();
		private static int _suppressionRefCount;

		private static System.Reflection.FieldInfo HeaderGUIEventField =>
			typeof(UnityEditor.Editor).GetField("finishedDefaultHeaderGUI",
				System.Reflection.BindingFlags.Static
				| System.Reflection.BindingFlags.NonPublic
				| System.Reflection.BindingFlags.Public);

		private static void SuppressAddressablesHeader()
		{
			if (_suppressionRefCount++ > 0) return;

			try
			{
				var field = HeaderGUIEventField;
				var current = field?.GetValue(null) as System.Delegate;
				if (current == null) return;

				System.Delegate remaining = current;
				foreach (var handler in current.GetInvocationList())
				{
					if (handler.Method?.DeclaringType?.FullName?.Contains("Addressable") == true)
					{
						remaining = System.Delegate.Remove(remaining, handler);
						_detachedHeaderHandlers.Add(handler);
					}
				}
				field.SetValue(null, remaining);
			}
			catch
			{
				// Addressables internals may differ between versions; leave the header untouched.
			}
		}

		private static void RestoreAddressablesHeader()
		{
			if (_suppressionRefCount > 0) _suppressionRefCount--;
			if (_suppressionRefCount > 0 || _detachedHeaderHandlers.Count == 0) return;

			try
			{
				var field = HeaderGUIEventField;
				if (field == null) return;

				var current = field.GetValue(null) as System.Delegate;
				foreach (var handler in _detachedHeaderHandlers)
					current = System.Delegate.Combine(current, handler);
				field.SetValue(null, current);
			}
			catch
			{
				// Ignore: restoration is best-effort and self-heals on domain reload.
			}
			finally
			{
				_detachedHeaderHandlers.Clear();
			}
		}
#endif
	}
}
