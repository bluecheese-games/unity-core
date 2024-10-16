//
// Copyright (c) 2024 BlueCheese Games All rights reserved
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

    public static class EditorGUIHelper
    {
		// Styles
		private static bool _initialized = false;
		private static GUIStyle _textFieldWithIconStyle;

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
    }
}
