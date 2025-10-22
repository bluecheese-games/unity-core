//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	public static class EditorIcon
	{
		public static Texture2D Search => GetTexture("Search On Icon");
		public static Texture2D Valid => GetTexture("d_Valid");
		public static Texture2D Trash => GetTexture("TreeEditor.Trash");
		public static Texture2D Plus => GetTexture("d_Toolbar Plus");
		public static Texture2D Cross => GetTexture("CrossIcon");
		public static Texture2D Play => GetTexture("d_PlayButton");
		public static Texture2D Stop => GetTexture("d_PauseButton");
		public static Texture2D Restart => GetTexture("Refresh");
		public static Texture2D Warning => GetTexture("d_console.warnicon.sml");
		public static Texture2D Skybox => GetTexture("d_ReflectionProbe Icon");
		public static Texture2D Menu => GetTexture("d__Menu");
		public static Texture2D Open => GetTexture("d_FolderOpened Icon");
		public static Texture2D Link => GetTexture("d_UnityEditor.FindDependencies");
		public static Texture2D Select => GetTexture("d_curvekeyframeselectedoverlay");

		private static Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>();

		private static Texture2D GetTexture(string name)
		{
			if (!_icons.ContainsKey(name) || _icons[name] == null)
			{
				_icons[name] = EditorGUIUtility.Load(name) as Texture2D;
			}
			return _icons[name];
		}
	}

	public static partial class EditorGUIHelper
	{
		// Styles
		private static bool _initialized = false;
		private static GUIStyle _textFieldWithIconStyle;
		private static GUIStyle _titleStyle;

		private static void InitStyles()
		{
			if (_initialized)
			{
				return;
			}

			// Init styles
			_textFieldWithIconStyle = new(GUI.skin.textField)
			{
				normal = { background = null }, // Clear any background for the icon
				padding = new RectOffset(25, 5, 2, 2), // Add padding to make room for the icon
			};

			_titleStyle = new GUIStyle(EditorStyles.helpBox)
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 18,
				fontStyle = FontStyle.Bold
			};

			_initialized = true;
		}

		public static string DrawTextfieldWithIcon(string text, Texture2D icon, Color? iconColor = null)
		{
			InitStyles();

			if (icon != null)
			{
				var textFieldRect = EditorGUILayout.GetControlRect();
				var iconRect = new Rect(textFieldRect.x + 3, textFieldRect.y + 2, 16, 16); // Icon size and position
				text = EditorGUI.TextField(textFieldRect, text, _textFieldWithIconStyle);
				var color = GUI.color;
				if (iconColor.HasValue)
				{
					GUI.color = iconColor.Value;
				}
				GUI.DrawTexture(iconRect, icon);
				GUI.color = color;
			}
			else
			{
				text = EditorGUILayout.TextField(text);
			}

			return text;
		}

		public static void DrawSearchableKeyProperty(SerializedProperty keyProperty, GUIContent label, string[] keys, int maxItems = 0)
		{
			DrawSearchableKeyProperty(keyProperty, label, keys, null, maxItems);
		}

		public static void DrawSearchableKeyProperty(SerializedProperty keyProperty, GUIContent label, string[] keys, string[] labels, int maxItems = 0)
		{
			if (keys == null || keyProperty == null)
			{
				EditorGUILayout.PropertyField(keyProperty, label);
				return;
			}

			// Validate labels array (must match keys length)
			string[] effectiveLabels = (labels != null && labels.Length == keys.Length) ? labels : null;

			// Build quick lookups if we have valid labels
			Dictionary<string, int> labelToIndex = null;
			Dictionary<string, int> keyToIndex = null;

			keyToIndex = new Dictionary<string, int>(keys.Length);
			for (int i = 0; i < keys.Length; i++)
				if (!keyToIndex.ContainsKey(keys[i])) keyToIndex.Add(keys[i], i);

			if (effectiveLabels != null)
			{
				labelToIndex = new Dictionary<string, int>(effectiveLabels.Length);
				for (int i = 0; i < effectiveLabels.Length; i++)
					if (!labelToIndex.ContainsKey(effectiveLabels[i])) labelToIndex.Add(effectiveLabels[i], i);
			}

			// Determine what to display in the text field: label (preferred) or raw key text
			string currentKey = keyProperty.stringValue;
			bool keyIsValid = keyToIndex.ContainsKey(currentKey);

			string displayText = currentKey;
			if (keyIsValid && effectiveLabels != null)
			{
				int idx = keyToIndex[currentKey];
				displayText = effectiveLabels[idx];
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);

			var icon = keyIsValid ? EditorIcon.Valid : EditorIcon.Warning;
			var color = keyIsValid ? Color.green : Color.white;

			// Draw text field showing the "label" if available
			string editedText = DrawTextfieldWithIcon(displayText, icon, color);

			// Map edited text back to a key:
			// 1) Exact match on label -> set the corresponding key
			// 2) Exact match on key -> set that key
			// 3) Otherwise, store the raw input (remains invalid until it matches a key/label)
			if (effectiveLabels != null && labelToIndex.TryGetValue(editedText, out int labelIdx))
			{
				keyProperty.stringValue = keys[labelIdx];
			}
			else if (keyToIndex.TryGetValue(editedText, out int keyIdx))
			{
				keyProperty.stringValue = keys[keyIdx];
			}
			else
			{
				keyProperty.stringValue = editedText; // may be invalid; shows warning icon
			}

			var propRect = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
			if (GUILayout.Button(new GUIContent(EditorIcon.Search), GUILayout.Width(30), GUILayout.Height(20)))
			{
				// Use the new SearchKeyWindow overload with labels
				SearchKeyWindow.Open(keyProperty, keys, effectiveLabels, propRect, maxItems);
			}

			EditorGUILayout.EndHorizontal();
		}

		public static void DrawTitle(string title)
		{
			InitStyles();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(title, _titleStyle);
			EditorGUILayout.EndHorizontal();
		}
	}
}