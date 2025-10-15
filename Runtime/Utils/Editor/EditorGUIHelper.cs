//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Linq;
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
			bool validKey = keys.Contains(keyProperty.stringValue);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);
			var icon = validKey ? EditorIcon.Valid : EditorIcon.Warning;
			var color = validKey ? Color.green : Color.white;
			keyProperty.stringValue = DrawTextfieldWithIcon(keyProperty.stringValue, icon, color);
			var propRect = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
			if (GUILayout.Button(new GUIContent(EditorIcon.Search), GUILayout.Width(30), GUILayout.Height(20)))
			{
				SearchKeyWindow.Open(keyProperty, keys, propRect, maxItems);
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