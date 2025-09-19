//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Utils.Editor
{
	public class SearchKeyWindow : EditorWindow
	{
		public static void Open(SerializedProperty targetProperty, string[] keys, Rect rect, int maxItems = 0)
		{
			if (maxItems == 0 && keys.Length <= 6)
			{
				maxItems = keys.Length;
			}

			var window = CreateInstance<SearchKeyWindow>();
			window._targetProperty = targetProperty;
			window._keys = keys;
			window._maxItems = maxItems;

			float itemHeight = 20;
			float height = maxItems > 0 ? 27 + itemHeight * maxItems : 140;
			window.ShowAsDropDown(rect, new(rect.width, height));
		}

		private SerializedProperty _targetProperty;
		private string[] _keys;
		private string _searchText;
		private Vector2 _scrollPosition;
		private int _maxItems;
		private GUIStyle _keyStyle;

		private void OnGUI()
		{
			if (_keyStyle == null)
			{
				_keyStyle = new GUIStyle(EditorStyles.textField)
				{
					hover = new GUIStyleState
					{
						textColor = Color.yellow
					},
				};
			}

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
				int count = 0;
				string searchText = _searchText != null ? _searchText.ToLowerInvariant() : null;
				_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
				foreach (var key in _keys)
				{
					if (string.IsNullOrWhiteSpace(searchText) || key.ToLowerInvariant().Contains(searchText))
					{
						count++;
						if (GUILayout.Button(new GUIContent(key), _keyStyle))
						{
							_targetProperty.stringValue = key;
							_targetProperty.serializedObject.ApplyModifiedProperties();
							Close();
						}
					}
					if (_maxItems > 0 && count >= _maxItems)
					{
						break;
					}
				}
				EditorGUILayout.EndScrollView();
				EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
			}

			EditorGUI.FocusTextInControl("search-text");
		}
	}
}