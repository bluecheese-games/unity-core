#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
public class SearchableEnumDrawer : PropertyDrawer
{
	// ---------- Enum cache (per enum type + sort flag) ----------

	private class EnumCache
	{
		public string[] RawNames;          // original enum display names
		public string[] DisplayNames;      // possibly-sorted names used by UI
		public int[] SortedToOriginal;     // mapping from sorted index -> original index (null if not sorted)
	}

	// Key: (enum System.Type, sortByName flag)
	private static readonly Dictionary<(Type, bool), EnumCache> _enumCache =
		new Dictionary<(Type, bool), EnumCache>();

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var attr = (SearchableEnumAttribute)attribute;

		// 1) Resolve the actual enum SerializedProperty
		SerializedProperty enumProp = GetEnumProperty(property, attr);
		if (enumProp == null || enumProp.propertyType != SerializedPropertyType.Enum)
		{
			EditorGUI.HelpBox(position, "SearchableEnum: no enum field found", MessageType.Warning);
			EditorGUI.PropertyField(position, property, label, true);
			return;
		}

		EditorGUI.BeginProperty(position, label, property);

		// Draw label like a regular enum field
		position = EditorGUI.PrefixLabel(position, label);

		// 2) Get enum System.Type for caching
		Type enumType = GetEnumTypeForProperty(enumProp, attr);
		if (enumType == null || !enumType.IsEnum)
		{
			// Fallback path without cache
			string[] rawNames = enumProp.enumDisplayNames;
			int rawIndex = Mathf.Clamp(enumProp.enumValueIndex, 0, rawNames.Length - 1);
			string buttonText = rawNames[rawIndex];

			if (GUI.Button(position, buttonText, EditorStyles.popup))
			{
				SerializedObject so = property.serializedObject;
				string enumPath = enumProp.propertyPath;

				var popup = new SearchableEnumPopup(
					width: position.width,
					names: rawNames,
					currentIndex: rawIndex,
					onSelect: (newIndex) =>
					{
						SerializedProperty p = so.FindProperty(enumPath);
						if (p != null)
						{
							p.enumValueIndex = newIndex;
							so.ApplyModifiedProperties();
						}
					}
				);

				PopupWindow.Show(position, popup);
			}

			EditorGUI.EndProperty();
			return;
		}

		// 3) Use / build cache
		EnumCache cache = GetOrCreateEnumCache(enumProp, attr, enumType);
		string[] displayNames = cache.DisplayNames;
		int[] sortedToOriginal = cache.SortedToOriginal;

		int rawEnumIndex = enumProp.enumValueIndex;
		if (rawEnumIndex < 0 || rawEnumIndex >= cache.RawNames.Length)
			rawEnumIndex = 0;

		int currentIndex = rawEnumIndex;
		if (sortedToOriginal != null)
		{
			currentIndex = Array.IndexOf(sortedToOriginal, rawEnumIndex);
			if (currentIndex < 0) currentIndex = 0;
		}

		string text = displayNames[currentIndex];

		// 4) Draw the "normal" enum-like dropdown button
		if (GUI.Button(position, text, EditorStyles.popup))
		{
			SerializedObject so = property.serializedObject;
			string enumPath = enumProp.propertyPath;

			var popup = new SearchableEnumPopup(
				width: position.width,
				names: displayNames,
				currentIndex: currentIndex,
				onSelect: (selectedIndexInNames) =>
				{
					int finalIndex = (sortedToOriginal != null)
						? sortedToOriginal[selectedIndexInNames]
						: selectedIndexInNames;

					SerializedProperty p = so.FindProperty(enumPath);
					if (p != null)
					{
						p.enumValueIndex = finalIndex;
						so.ApplyModifiedProperties();
					}
				}
			);

			PopupWindow.Show(position, popup);
		}

		EditorGUI.EndProperty();
	}

	// ---------- Enum cache helpers ----------

	private EnumCache GetOrCreateEnumCache(SerializedProperty enumProp, SearchableEnumAttribute attr, Type enumType)
	{
		var key = (enumType, attr.SortByName);

		if (_enumCache.TryGetValue(key, out EnumCache cache))
			return cache;

		cache = new EnumCache();

		string[] rawNames = enumProp.enumDisplayNames;
		cache.RawNames = rawNames;

		if (!attr.SortByName)
		{
			cache.DisplayNames = rawNames;
			cache.SortedToOriginal = null;
		}
		else
		{
			int length = rawNames.Length;

			cache.SortedToOriginal = new int[length];
			for (int i = 0; i < length; i++)
				cache.SortedToOriginal[i] = i;

			Array.Sort(cache.SortedToOriginal,
				(a, b) => string.Compare(rawNames[a], rawNames[b], StringComparison.OrdinalIgnoreCase));

			cache.DisplayNames = new string[length];
			for (int i = 0; i < length; i++)
				cache.DisplayNames[i] = rawNames[cache.SortedToOriginal[i]];
		}

		_enumCache[key] = cache;
		return cache;
	}

	private Type GetEnumTypeForProperty(SerializedProperty enumProp, SearchableEnumAttribute attr)
	{
		// If the field itself is an enum:
		if (fieldInfo.FieldType.IsEnum)
			return fieldInfo.FieldType;

		// Wrapper type case
		Type wrapperType = fieldInfo.FieldType;
		if (!wrapperType.IsClass && !wrapperType.IsValueType)
			return null;

		// 1) If InnerFieldName provided, use that
		if (!string.IsNullOrEmpty(attr.InnerFieldName))
		{
			FieldInfo inner = wrapperType.GetField(
				attr.InnerFieldName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			if (inner != null && inner.FieldType.IsEnum)
				return inner.FieldType;
		}

		// 2) Otherwise, auto-detect the first enum field
		FieldInfo[] fields = wrapperType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (var f in fields)
		{
			if (f.FieldType.IsEnum)
				return f.FieldType;
		}

		return null;
	}

	// ---------- Find enum SerializedProperty (self or wrapper inner) ----------

	private SerializedProperty GetEnumProperty(SerializedProperty property, SearchableEnumAttribute attr)
	{
		// Simple case: field is an enum itself
		if (property.propertyType == SerializedPropertyType.Enum)
			return property;

		// Wrapper type: usually Generic
		if (property.propertyType != SerializedPropertyType.Generic)
			return null;

		// 1) If user gave InnerFieldName, try that first
		if (!string.IsNullOrEmpty(attr.InnerFieldName))
		{
			SerializedProperty child = property.FindPropertyRelative(attr.InnerFieldName);
			if (child != null && child.propertyType == SerializedPropertyType.Enum)
				return child;
		}

		// 2) Otherwise, auto-detect first enum child
		SerializedProperty copy = property.Copy();
		SerializedProperty end = copy.GetEndProperty();
		bool enterChildren = true;

		while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
		{
			enterChildren = false;

			if (copy.propertyType == SerializedPropertyType.Enum)
				return copy.Copy(); // return a copy, not the iterator
		}

		return null;
	}

	// ========================================================================
	// Popup: search + icon + clear + keyboard nav + auto-scroll + highlight
	// ========================================================================
	private class SearchableEnumPopup : PopupWindowContent
	{
		private readonly string[] _names;     // display names (sorted or not)
		private readonly Action<int> _onSelect;
		private int _currentIndex;            // index into _names for current enum value

		private string _search = string.Empty;
		private Vector2 _scroll;

		private readonly float _width;
		private const float WindowHeight = 300f;

		private const float Margin = 0f;   // requested: margin = 0
		private const float SearchHeight = 20f;

		private bool _requestFocus = false;
		private const string SearchControlName = "SearchableEnumPopupSearchField";

		private readonly List<int> _filteredIndices = new List<int>(); // indices into _names
		private int _keyboardIndex = 0;  // index into _filteredIndices

		private bool _syncKeyboardToCurrentOnNextRebuild = true;
		private bool _scrollToKeyboardOnNextRepaint = true;
		private bool _filterDirty = true;

		public SearchableEnumPopup(float width, string[] names, int currentIndex, Action<int> onSelect)
		{
			_width = width;
			_names = names;
			_currentIndex = Mathf.Clamp(currentIndex, 0, _names.Length - 1);
			_onSelect = onSelect;

			_syncKeyboardToCurrentOnNextRebuild = true; // default keyboard selection = current enum value
			_scrollToKeyboardOnNextRepaint = true; // ensure visible on open
			_filterDirty = true; // build filtered list on first draw
		}

		public override Vector2 GetWindowSize()
		{
			return new Vector2(_width, WindowHeight);
		}

		public override void OnOpen()
		{
			_requestFocus = true;

			// Delay so Unity has fully created the window
			EditorApplication.delayCall += () =>
			{
				if (editorWindow == null)
					return;

				editorWindow.Focus();
				_requestFocus = true;
				editorWindow.Repaint();
			};
		}

		public override void OnGUI(Rect rect)
		{
			HandleGlobalKeys(); // ESC, arrows, Enter

			DrawSearchField(rect);
			DrawList(rect);
		}

		// ----------------- Keyboard handling -----------------

		private void HandleGlobalKeys()
		{
			Event e = Event.current;
			if (e.type != EventType.KeyDown)
				return;

			if (_filteredIndices.Count == 0)
			{
				if (e.keyCode == KeyCode.Escape)
				{
					editorWindow.Close();
					e.Use();
				}
				return;
			}

			switch (e.keyCode)
			{
				case KeyCode.DownArrow:
					_keyboardIndex = Mathf.Clamp(_keyboardIndex + 1, 0, _filteredIndices.Count - 1);
					_scrollToKeyboardOnNextRepaint = true;
					e.Use();
					editorWindow.Repaint();
					break;

				case KeyCode.UpArrow:
					_keyboardIndex = Mathf.Clamp(_keyboardIndex - 1, 0, _filteredIndices.Count - 1);
					_scrollToKeyboardOnNextRepaint = true;
					e.Use();
					editorWindow.Repaint();
					break;

				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					SelectByKeyboard();
					e.Use();
					break;

				case KeyCode.Escape:
					editorWindow.Close();
					e.Use();
					break;
			}
		}

		private void SelectByKeyboard()
		{
			if (_filteredIndices.Count == 0)
				return;

			int nameIndex = _filteredIndices[Mathf.Clamp(_keyboardIndex, 0, _filteredIndices.Count - 1)];
			_currentIndex = nameIndex;
			_onSelect?.Invoke(nameIndex);
			editorWindow.Close();
		}

		// ----------------- Search field -----------------

		private void DrawSearchField(Rect rect)
		{
			rect.x += Margin;
			rect.y += Margin;
			rect.width -= Margin * 2f;
			rect.height = SearchHeight;

			// Background
			EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f, 0.25f));

			// Magnifying glass icon
			Rect iconRect = new Rect(rect.x + 4f, rect.y + 2f, 16f, 16f);
			GUI.Label(iconRect, EditorGUIUtility.IconContent("Search Icon"));

			bool hasSearch = !string.IsNullOrEmpty(_search);
			float clearWidth = hasSearch ? 18f : 0f;

			// Text field rect
			Rect textRect = new Rect(
				rect.x + 22f,
				rect.y + 1f,
				rect.width - 22f - clearWidth,
				rect.height - 2f
			);

			string oldSearch = _search;

			GUI.SetNextControlName(SearchControlName);
			_search = EditorGUI.TextField(textRect, _search);

			// Clear button (x)
			if (hasSearch)
			{
				Rect clearRect = new Rect(
					rect.xMax - clearWidth + 2f,
					rect.y + 2f,
					clearWidth - 4f,
					rect.height - 4f
				);

				if (GUI.Button(clearRect, "x", EditorStyles.miniButton))
				{
					_search = string.Empty;
					_syncKeyboardToCurrentOnNextRebuild = true;
					_scrollToKeyboardOnNextRepaint = true;
					_filterDirty = true;
					GUI.FocusControl(SearchControlName);
					editorWindow.Repaint();
				}
			}

			// When search text changes, mark filter dirty and re-sync keyboard
			if (oldSearch != _search)
			{
				_syncKeyboardToCurrentOnNextRebuild = true;
				_scrollToKeyboardOnNextRepaint = true;
				_filterDirty = true;
			}

			// Focus search field on open
			if (_requestFocus && Event.current.type == EventType.Repaint)
			{
				_requestFocus = false;
				EditorGUI.FocusTextInControl(SearchControlName);
			}
		}

		// ----------------- List -----------------

		private void DrawList(Rect outerRect)
		{
			float listTop = Margin + SearchHeight;

			Rect listRect = new Rect(
				outerRect.x + Margin,
				outerRect.y + listTop,
				outerRect.width - Margin * 2f,
				outerRect.height - listTop - Margin
			);

			// Rebuild filtered indices only when search changes (dirty)
			if (_filterDirty)
			{
				_filteredIndices.Clear();

				string searchLower = string.IsNullOrEmpty(_search)
					? null
					: _search.ToLowerInvariant();

				for (int i = 0; i < _names.Length; i++)
				{
					string name = _names[i];
					if (!string.IsNullOrEmpty(searchLower) &&
						name.ToLowerInvariant().IndexOf(searchLower, StringComparison.Ordinal) < 0)
					{
						continue;
					}

					_filteredIndices.Add(i);
				}

				_filterDirty = false;
			}

			if (_filteredIndices.Count == 0)
			{
				GUILayout.BeginArea(listRect);
				_scroll = GUILayout.BeginScrollView(_scroll);
				GUILayout.Label("No results", EditorStyles.centeredGreyMiniLabel);
				GUILayout.EndScrollView();
				GUILayout.EndArea();
				return;
			}

			// Sync keyboard selection to current enum value when needed
			if (_syncKeyboardToCurrentOnNextRebuild)
			{
				int idx = _filteredIndices.IndexOf(_currentIndex);
				_keyboardIndex = idx >= 0 ? idx : 0;
				_syncKeyboardToCurrentOnNextRebuild = false;
			}

			_keyboardIndex = Mathf.Clamp(_keyboardIndex, 0, _filteredIndices.Count - 1);

			float rowHeight = EditorGUIUtility.singleLineHeight + 2f;
			float totalHeight = rowHeight * _filteredIndices.Count;

			Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, totalHeight); // minus scrollbar width

			// Ensure selected row is visible when navigating / on open
			if ((Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout)
				&& _scrollToKeyboardOnNextRepaint)
			{
				_scrollToKeyboardOnNextRepaint = false;
				EnsureVisibleIndex(_keyboardIndex, listRect.height, rowHeight);
			}

			_scroll = GUI.BeginScrollView(listRect, _scroll, viewRect);

			// Draw rows
			string searchLowerCached = string.IsNullOrEmpty(_search)
				? null
				: _search.ToLowerInvariant();

			for (int filteredIdx = 0; filteredIdx < _filteredIndices.Count; filteredIdx++)
			{
				int nameIndex = _filteredIndices[filteredIdx];
				string name = _names[nameIndex];

				bool isCurrent = (nameIndex == _currentIndex);
				bool isKeyboard = (filteredIdx == _keyboardIndex);

				GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
				if (isCurrent)
					labelStyle.fontStyle = FontStyle.Bold;

				labelStyle.richText = true;

				string displayName = BuildHighlightedName(name, searchLowerCached);

				Rect rowRect = new Rect(
					0f,
					filteredIdx * rowHeight,
					viewRect.width,
					rowHeight
				);

				// Mouse selection
				if (Event.current.type == EventType.MouseDown &&
					rowRect.Contains(Event.current.mousePosition))
				{
					_currentIndex = nameIndex;
					_onSelect?.Invoke(nameIndex);
					editorWindow.Close();
					Event.current.Use();
				}

				// Background: keyboard > hover > current
				if (rowRect.Contains(Event.current.mousePosition))
				{
					EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.25f));
				}
				else if (isKeyboard)
				{
					EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.20f));
				}
				else if (isCurrent)
				{
					EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.10f));
				}

				GUI.Label(rowRect, displayName, labelStyle);
			}

			GUI.EndScrollView();
		}

		private void EnsureVisibleIndex(int filteredIndex, float viewportHeight, float rowHeight)
		{
			if (filteredIndex < 0)
				return;

			float top = filteredIndex * rowHeight;
			float bottom = top + rowHeight;

			if (top < _scroll.y)
			{
				_scroll.y = top;
			}
			else if (bottom > _scroll.y + viewportHeight)
			{
				_scroll.y = bottom - viewportHeight;
			}
		}

		private string BuildHighlightedName(string name, string searchLower)
		{
			if (string.IsNullOrEmpty(searchLower))
				return name;

			int idx = name.ToLowerInvariant().IndexOf(searchLower, StringComparison.Ordinal);
			if (idx < 0)
				return name;

			string before = name.Substring(0, idx);
			string match = name.Substring(idx, _search.Length);
			string after = name.Substring(idx + _search.Length);

			return $"{before}<b>{match}</b>{after}";
		}
	}
}
#endif
