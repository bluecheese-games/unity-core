//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	public class SearchKeyWindow : EditorWindow
	{
		public static void Open(SerializedProperty targetProperty, string[] keys, Rect rect)
		{
			//var window = GetWindow<SearchKeyWindow>();
			var window = CreateInstance<SearchKeyWindow>();
			window._targetProperty = targetProperty;
			window._keys = keys;

			window.ShowAsDropDown(rect, new(rect.width, 300));
		}

		private SerializedProperty _targetProperty;
		private string[] _keys;
		private string _searchText;
		private Vector2 _scrollPosition;
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
				_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
				foreach (var key in _keys)
				{
					if (string.IsNullOrWhiteSpace(_searchText) || key.Contains(_searchText))
					{
						if (GUILayout.Button(new GUIContent(key), _keyStyle))
						{
							_targetProperty.stringValue = key;
							_targetProperty.serializedObject.ApplyModifiedProperties();
							Close();
						}
					}
				}
				EditorGUILayout.EndScrollView();
				EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
			}

			EditorGUI.FocusTextInControl("search-text");
		}
	}
}