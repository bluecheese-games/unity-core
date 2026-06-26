//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
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
		public static Texture2D Scene => GetTexture("d_SceneViewLighting");
		public static Texture2D Prefab => GetTexture("d_Prefab Icon");

		private static readonly Dictionary<string, Texture2D> _icons = new();

		private static Texture2D GetTexture(string name)
		{
			if (!_icons.ContainsKey(name) || _icons[name] == null)
			{
				_icons[name] = EditorGUIUtility.Load(name) as Texture2D;
			}
			return _icons[name];
		}
	}

	public static class EditorGUIHelper
	{
		// Styles
		private static bool _initialized = false;
		private static GUIStyle _textFieldWithIconStyle;
		private static GUIStyle _clickableFieldStyle;
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

			// Read-only field: based on the editor text field so it aligns with native inspector rows
			_clickableFieldStyle = new(EditorStyles.textField)
			{
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset(25, 5, 0, 0), // Left room for the icon; vertical centering handled by alignment
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

		// Draws a non-editable field with an icon at the given rect. Returns true when the user clicks it.
		public static bool DrawClickableFieldWithIcon(Rect fieldRect, string text, Texture2D icon, Color? iconColor = null)
		{
			InitStyles();

			GUI.Label(fieldRect, text, _clickableFieldStyle);

			if (icon != null)
			{
				var iconRect = new Rect(fieldRect.x + 3, fieldRect.y + (fieldRect.height - 16) * 0.5f, 16, 16); // Icon size and position
				var color = GUI.color;
				if (iconColor.HasValue)
				{
					GUI.color = iconColor.Value;
				}
				GUI.DrawTexture(iconRect, icon);
				GUI.color = color;
			}

			EditorGUIUtility.AddCursorRect(fieldRect, MouseCursor.Link);

			var evt = Event.current;
			bool clicked = evt.type == EventType.MouseDown && evt.button == 0 && fieldRect.Contains(evt.mousePosition);
			if (clicked)
			{
				evt.Use();
			}

			return clicked;
		}

		// Layout variant: reserves a control rect then draws the clickable field.
		public static bool DrawClickableFieldWithIcon(string text, Texture2D icon, out Rect fieldRect, Color? iconColor = null)
		{
			fieldRect = EditorGUILayout.GetControlRect();
			return DrawClickableFieldWithIcon(fieldRect, text, icon, iconColor);
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

			ResolveKeyDisplay(keyProperty, keys, labels, out string[] effectiveLabels, out string displayText, out bool keyIsValid);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);

			var icon = keyIsValid ? EditorIcon.Valid : EditorIcon.Warning;
			var color = keyIsValid ? Color.green : Color.white;

			// Clicking the (read-only) field opens the search dropdown
			if (DrawClickableFieldWithIcon(displayText, icon, out Rect fieldRect, color))
			{
				var propRect = GUIUtility.GUIToScreenRect(fieldRect);
				SearchKeyWindow.Open(keyProperty, keys, effectiveLabels, propRect, maxItems);
			}

			EditorGUILayout.EndHorizontal();
		}

		// Rect variant: draws at the given position. Use this in PropertyDrawers so the field aligns
		// pixel-perfectly with the surrounding inspector rows.
		public static void DrawSearchableKeyProperty(Rect position, SerializedProperty keyProperty, GUIContent label, string[] keys, string[] labels = null, int maxItems = 0)
		{
			if (keys == null || keyProperty == null)
			{
				EditorGUI.PropertyField(position, keyProperty, label);
				return;
			}

			ResolveKeyDisplay(keyProperty, keys, labels, out string[] effectiveLabels, out string displayText, out bool keyIsValid);

			var icon = keyIsValid ? EditorIcon.Valid : EditorIcon.Warning;
			var color = keyIsValid ? Color.green : Color.white;

			Rect fieldRect = EditorGUI.PrefixLabel(position, label);

			// Clicking the (read-only) field opens the search dropdown
			if (DrawClickableFieldWithIcon(fieldRect, displayText, icon, color))
			{
				var propRect = GUIUtility.GUIToScreenRect(fieldRect);
				SearchKeyWindow.Open(keyProperty, keys, effectiveLabels, propRect, maxItems);
			}
		}

		// Validates the current key and resolves the text to display (label when available, else raw key).
		private static void ResolveKeyDisplay(SerializedProperty keyProperty, string[] keys, string[] labels, out string[] effectiveLabels, out string displayText, out bool keyIsValid)
		{
			effectiveLabels = (labels != null && labels.Length == keys.Length) ? labels : null;

			var keyToIndex = new Dictionary<string, int>(keys.Length);
			for (int i = 0; i < keys.Length; i++)
				if (!keyToIndex.ContainsKey(keys[i])) keyToIndex.Add(keys[i], i);

			string currentKey = keyProperty.stringValue;
			keyIsValid = keyToIndex.TryGetValue(currentKey, out int currentIndex);
			displayText = (keyIsValid && effectiveLabels != null) ? effectiveLabels[currentIndex] : currentKey;
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