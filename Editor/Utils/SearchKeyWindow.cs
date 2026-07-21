//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	public class SearchKeyWindow : EditorWindow
	{
		public static void Open(SerializedProperty targetProperty, string[] keys, Rect rect, int maxItems = 0)
		{
			Open(targetProperty, keys, null, rect, maxItems);
		}

		public static void Open(SerializedProperty targetProperty, string[] keys, string[] labels, Rect rect, int maxItems = 0, Action<string> onCreateNew = null)
		{
			if (keys == null) return;

			if (maxItems == 0 && keys.Length <= 6)
			{
				maxItems = keys.Length;
			}

			var window = CreateInstance<SearchKeyWindow>();
			window._targetProperty = targetProperty;
			window._keys = keys;
			window._labels = (labels != null && labels.Length == keys.Length) ? labels : null; // null => fallback to keys
			window._maxItems = maxItems;
			window._onCreateNew = onCreateNew;

			float itemHeight = 20f;
			float height = maxItems > 0 ? 27f + itemHeight * maxItems : 140f;
			window.ShowAsDropDown(rect, new(rect.width, height));
		}

		private SerializedProperty _targetProperty;
		private string[] _keys;
		private string[] _labels; // optional; may be null if not provided or mismatched
		private string _searchText;
		private Vector2 _scrollPosition;
		private int _maxItems;
		private GUIStyle _keyStyle;
		private Action<string> _onCreateNew; // optional: offered when the search matches nothing

		private void OnGUI()
		{
			if (_keyStyle == null)
			{
				_keyStyle = new GUIStyle(EditorStyles.textField)
				{
					hover = new GUIStyleState { textColor = Color.yellow },
				};
			}

			// Search + Close
			EditorGUILayout.BeginHorizontal();
			GUI.SetNextControlName("search-text");
			_searchText = EditorGUIHelper.DrawTextfieldWithIcon(_searchText, EditorIcon.Search);
			if (GUILayout.Button(EditorIcon.Cross, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(20)))
			{
				Close();
			}
			EditorGUILayout.EndHorizontal();

			if (_keys != null)
			{
				string searchText = _searchText != null ? _searchText.ToLowerInvariant() : null;

				// When nothing matches, offer to create a new key from the typed text (if enabled).
				if (_onCreateNew != null && !string.IsNullOrWhiteSpace(_searchText) && !HasMatches(searchText))
				{
					if (GUILayout.Button($"＋ Create new key: \"{_searchText.Trim()}\""))
					{
						var newKey = _searchText.Trim();
						Close();
						_onCreateNew(newKey);
						return;
					}
				}

				int shown = 0;
				_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

				for (int i = 0; i < _keys.Length; i++)
				{
					string key = _keys[i];
					// Use label if provided and valid, otherwise fallback to key
					string label = (_labels != null) ? _labels[i] : key;

					if (IsMatch(label, key, searchText))
					{
						shown++;
						// Show label; store key. Tooltip shows the key for clarity.
						if (GUILayout.Button(new GUIContent(label, key), _keyStyle))
						{
							_targetProperty.stringValue = key; // always store the key
							_targetProperty.serializedObject.ApplyModifiedProperties();
							Close();
						}

						if (_maxItems > 0 && shown >= _maxItems)
						{
							break;
						}
					}
				}

				EditorGUILayout.EndScrollView();
				EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
			}

			EditorGUI.FocusTextInControl("search-text");
		}

		private bool HasMatches(string searchText)
		{
			for (int i = 0; i < _keys.Length; i++)
			{
				string label = (_labels != null) ? _labels[i] : _keys[i];
				if (IsMatch(label, _keys[i], searchText))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsMatch(string label, string key, string searchText)
		{
			if (string.IsNullOrWhiteSpace(searchText)) return true;
			if (!string.IsNullOrEmpty(label) && label.ToLowerInvariant().Contains(searchText)) return true;
			if (string.IsNullOrEmpty(label) && key.ToLowerInvariant().Contains(searchText)) return true;
			return false;
		}
	}

}