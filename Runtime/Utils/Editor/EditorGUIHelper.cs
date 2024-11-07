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
		public static Texture2D Play => GetTexture("d_PlayButton");
		public static Texture2D Stop => GetTexture("d_PauseButton");
		public static Texture2D Restart => GetTexture("Refresh");

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

	public static partial class EditorGUIHelper
	{
		#region Text AutoComplete
		private const string m_AutoCompleteField = "AutoCompleteField";
		private static List<string> m_CacheCheckList = null;
		private static string m_AutoCompleteLastInput;
		private static string m_EditorFocusAutoComplete;

		/// <summary>A textField to popup a matching popup, based on developers input values.</summary>
		/// <param name="input">string input.</param>
		/// <param name="source">the data of all possible values (string).</param>
		/// <param name="maxShownCount">the amount to display result.</param>
		/// <param name="levenshteinDistance">
		/// value between 0f ~ 1f,
		/// - more then 0f will enable the fuzzy matching
		/// - 1f = anything thing is okay.
		/// - 0f = require full match to the reference
		/// - recommend 0.4f ~ 0.7f
		/// </param>
		/// <returns>output string.</returns>
		public static string TextFieldAutoComplete(Rect position, GUIContent label, string input, string[] source, int maxShownCount = 5, float levenshteinDistance = 0.5f)
		{
			string tag = m_AutoCompleteField + GUIUtility.GetControlID(FocusType.Passive);
			int uiDepth = GUI.depth;
			GUI.SetNextControlName(tag);
			string rst = EditorGUI.TextField(position, label, input);
			if (input.Length > 0 && GUI.GetNameOfFocusedControl() == tag)
			{
				if (m_AutoCompleteLastInput != input || // input changed
					m_EditorFocusAutoComplete != tag) // another field.
				{
					// Update cache
					m_EditorFocusAutoComplete = tag;
					m_AutoCompleteLastInput = input;

					List<string> uniqueSrc = new List<string>(new HashSet<string>(source)); // remove duplicate
					int srcCnt = uniqueSrc.Count;
					m_CacheCheckList = new List<string>(System.Math.Min(maxShownCount, srcCnt)); // optimize memory alloc

					// Start with - slow
					for (int i = 0; i < srcCnt && m_CacheCheckList.Count < maxShownCount; i++)
					{
						if (uniqueSrc[i].ToLower().StartsWith(input.ToLower()))
						{
							m_CacheCheckList.Add(uniqueSrc[i]);
							uniqueSrc.RemoveAt(i);
							srcCnt--;
							i--;
						}
					}

					// Contains - very slow
					if (m_CacheCheckList.Count == 0)
					{
						for (int i = 0; i < srcCnt && m_CacheCheckList.Count < maxShownCount; i++)
						{
							if (uniqueSrc[i].ToLower().Contains(input.ToLower()))
							{
								m_CacheCheckList.Add(uniqueSrc[i]);
								uniqueSrc.RemoveAt(i);
								srcCnt--;
								i--;
							}
						}
					}

					// Levenshtein Distance - very very slow.
					if (levenshteinDistance > 0f && // only developer request
						input.Length > 3 && // 3 characters on input, hidden value to avoid doing too early.
						m_CacheCheckList.Count < maxShownCount) // have some empty space for matching.
					{
						levenshteinDistance = Mathf.Clamp01(levenshteinDistance);
						string keywords = input.ToLower();
						for (int i = 0; i < srcCnt && m_CacheCheckList.Count < maxShownCount; i++)
						{
							int distance = uniqueSrc[i].LevenshteinDistance(keywords, caseSensitive: false);
							bool closeEnough = (int)(levenshteinDistance * uniqueSrc[i].Length) > distance;
							if (closeEnough)
							{
								m_CacheCheckList.Add(uniqueSrc[i]);
								uniqueSrc.RemoveAt(i);
								srcCnt--;
								i--;
							}
						}
					}
				}

				// Draw recommend keyward(s)
				if (m_CacheCheckList.Count > 0)
				{
					int cnt = m_CacheCheckList.Count;
					float height = cnt * EditorGUIUtility.singleLineHeight;
					Rect area = position;
					area = new Rect(area.x + EditorGUIUtility.labelWidth, area.y - height, area.width - EditorGUIUtility.labelWidth, height);
					GUI.depth -= 10;
					GUI.BeginGroup(area);
					// area.position = Vector2.zero;
					//GUI.BeginClip(area);
					Rect line = new Rect(0, 0, area.width, EditorGUIUtility.singleLineHeight);

					for (int i = 0; i < cnt; i++)
					{
						if (Button(line, m_CacheCheckList[i]))//, EditorStyles.toolbarDropDown))
						{
							rst = m_CacheCheckList[i];
							GUI.changed = true;
							GUI.FocusControl(""); // force update
						}
						line.y += line.height;
					}
					//GUI.EndClip();
					GUI.EndGroup();
					GUI.depth += 10;
				}
			}
			return rst;
		}

		public static string TextFieldAutoComplete(GUIContent label, string input, string[] source, int maxShownCount = 5, float levenshteinDistance = 0.5f)
		{
			Rect rect = EditorGUILayout.GetControlRect();
			return TextFieldAutoComplete(rect, label, input, source, maxShownCount, levenshteinDistance);
		}
		#endregion
	}

	public static partial class EditorGUIHelper
	{
		private static int highestDepthID = 0;
		private static EventType lastEventType = EventType.Layout;

		private static int frame = 0;
		private static int lastEventFrame = 0;

		public static bool noButtonActive = false;
		public static Vector2 lastClickPos = Vector2.zero;

		public static int lastDownButtonHash = 0;


		public static bool Button(Rect bounds, string caption, GUIStyle btnStyle = null)
		{
			// this made problems when adding/removing controls during UI's: 
			// int controlID = GUIUtility.GetControlID( bounds.GetHashCode(), FocusType.Passive );

			// this one works also with dynamic GUI's !
			int controlID = GUIUtility.GetControlID(FocusType.Passive);

			bool isMouseOver = bounds.Contains(Event.current.mousePosition);
			int depth = (1000 - GUI.depth) * 1000 + controlID;
			if (isMouseOver && depth > highestDepthID) highestDepthID = depth;
			bool isTopmostMouseOver = (highestDepthID == depth);
			bool wasDragging = false;
			if (isMouseOver) noButtonActive = false;

#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
		bool paintMouseOver = isTopmostMouseOver && (Input.touchCount > 0) && !wasDragging;
#else
			bool paintMouseOver = isTopmostMouseOver;
#endif

			if (btnStyle == null)
			{
				btnStyle = GUI.skin.FindStyle("button");
			}

			if (Event.current.type == EventType.Layout && lastEventType != EventType.Layout)
			{
				highestDepthID = 0;
				frame++;
				noButtonActive = true;
			}
			lastEventType = Event.current.type;

			if (Event.current.type == EventType.Repaint)
			{
				bool isDown = (GUIUtility.hotControl == controlID);

				if (caption == null || caption == "")
				{
					// optimized: only 1 drawcall instead of two!
					if (paintMouseOver && btnStyle.hover != null && btnStyle.hover.background != null)
					{
						GUI.DrawTexture(bounds, btnStyle.hover.background, ScaleMode.StretchToFill);
					}
					else if (isDown && btnStyle.active != null && btnStyle.active.background != null)
					{
						GUI.DrawTexture(bounds, btnStyle.active.background, ScaleMode.StretchToFill);
					}
					else if (btnStyle.normal != null && btnStyle.normal.background != null)
					{
						GUI.DrawTexture(bounds, btnStyle.normal.background, ScaleMode.StretchToFill);
					}
				}
				else
				{
					// Works but is slow: always creates 2 drawcalls: (1 bg and 1 for text)
					btnStyle.Draw(bounds, new GUIContent(caption), paintMouseOver, isDown, false, false);
				}
			}

			// Workaround:
			// ignore duplicate mouseUp events. These can occur when running
			// unity editor with unity remote on iOS ... (anybody knows WHY?)
			//		if ( frame <= (1+lastEventFrame) ) return false;
			if (frame <= (10 + lastEventFrame)) return false;       // increased to 10 due to double clicks on ipad 3 ...

			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:
					{
						// Debug.Log ("mouseclick: topmost=" + isTopmostMouseOver + " wasDragging=" + wasDragging + " controlID=" + controlID + " caption=" + caption );
						if (isTopmostMouseOver && !wasDragging)
						{
							GUIUtility.hotControl = controlID;
						}
						break;
					}

				case EventType.MouseUp:
					{
						GUIUtility.hotControl = 0;
						if (isTopmostMouseOver && !wasDragging)
						{
							lastClickPos.x = Input.mousePosition.x;
							lastClickPos.y = Input.mousePosition.y;
							lastEventFrame = frame;
							return true;
						}
						break;
					}
			}
			return false;
		}
	}
}