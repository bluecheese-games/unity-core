//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// Custom Inspector drawer for <see cref="Tags"/>.
	/// Displays tags as colored chips with a delete button each.
	/// The input row lets the user type a new tag (Enter to confirm) or pick an existing
	/// one via the dropdown button, which also filters by whatever is already typed.
	/// </summary>
	[CustomPropertyDrawer(typeof(Tags))]
	public class TagsPropertyDrawer : PropertyDrawer
	{
		// ── Constants ────────────────────────────────────────────────────────

		private const float ChipSpacingX  = 4f;
		private const float LineSpacingY  = 2f;
		private const float DeleteBtnW    = 14f;
		private const float PickerBtnW    = 22f;
		private const float MinChipWidth  = 48f;

		// ── Shared styles (lazy, static — valid after skin is loaded) ────────

		private static GUIStyle _chipStyle;
		private static GUIStyle _placeholderStyle;

		private static void EnsureStyles()
		{
			if (_chipStyle != null) return;

			_chipStyle = new GUIStyle(EditorStyles.miniButton)
			{
				alignment  = TextAnchor.MiddleLeft,
				fontStyle  = FontStyle.Normal,
				fontSize   = 11,
				// right padding leaves room for the delete overlay
				padding    = new RectOffset(6, (int)DeleteBtnW + 4, 1, 1),
				margin     = new RectOffset(0, 0, 0, 0),
				fixedHeight = 0,
			};

			_placeholderStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.6f) },
				padding = new RectOffset(3, 0, 1, 0),
			};
		}

		// ── Per-property-path state (instance is shared across all Tags fields) ──

		// text currently in the "add tag" input, keyed by property path
		private static readonly Dictionary<string, string> _inputs = new();

		// chip row layout cache, keyed by property path
		private static readonly Dictionary<string, (float contentWidth, List<List<ChipInfo>> rows)> _layouts = new();

		// ── PropertyDrawer API ───────────────────────────────────────────────

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			EnsureStyles();
			var tagsProperty = property.FindPropertyRelative("_values");
			// Use approximation before the first OnGUI sets an exact width
			float contentWidth = Mathf.Max(1f, EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 22f);
			int chipRowCount = GetOrBuildLayout(property.propertyPath, tagsProperty, contentWidth).Count;

			float lineH   = EditorGUIUtility.singleLineHeight;
			bool editable = GUI.enabled;
			// Read-only: only chip rows (minimum 1 line for the label).
			// Editable: chip rows + 1 input row.
			int totalRows = editable ? chipRowCount + 1 : Mathf.Max(chipRowCount, 1);
			return totalRows * (lineH + LineSpacingY) - LineSpacingY;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EnsureStyles();
			EditorGUI.BeginProperty(position, label, property);

			var tagsProperty = property.FindPropertyRelative("_values");
			Tags tags         = (Tags)property.boxedValue;
			string path       = property.propertyPath;
			float lineH       = EditorGUIUtility.singleLineHeight;
			float labelW      = EditorGUIUtility.labelWidth;
			float contentW    = position.width - labelW;

			// Rebuild layout when the content width changes
			var rows = GetOrBuildLayout(path, tagsProperty, contentW);

			var cursor     = new Rect(position.x, position.y, position.width, lineH);
			bool labelDrawn  = false;
			bool needRebuild = false;

			// ── Chip rows ─────────────────────────────────────────────────────
			foreach (var row in rows)
			{
				if (!labelDrawn)
				{
					EditorGUI.LabelField(new Rect(cursor.x, cursor.y, labelW, lineH), label);
					labelDrawn = true;
				}

				float chipX = cursor.x + labelW;
				foreach (var chip in row)
				{
					DrawChip(chip, chipX, cursor.y, lineH, tagsProperty, ref needRebuild);
					chipX += chip.Width + ChipSpacingX;
				}

				cursor.y += lineH + LineSpacingY;
			}

			if (needRebuild)
			{
				_layouts.Remove(path);
				EditorGUI.EndProperty();
				return;
			}

			// ── Input row (editable only) ──────────────────────────────────────
			if (!GUI.enabled)
			{
				if (!labelDrawn)
					EditorGUI.LabelField(new Rect(cursor.x, cursor.y, labelW, lineH), label);
				EditorGUI.EndProperty();
				return;
			}
			if (!labelDrawn)
			{
				EditorGUI.LabelField(new Rect(cursor.x, cursor.y, labelW, lineH), label);
			}

			float inputX = cursor.x + labelW;
			float inputW = cursor.xMax - inputX - PickerBtnW - ChipSpacingX;
			var inputRect  = new Rect(inputX, cursor.y, inputW, lineH);
			var pickerRect = new Rect(inputRect.xMax + ChipSpacingX, cursor.y, PickerBtnW, lineH);

			// Text field
			if (!_inputs.TryGetValue(path, out string inputText))
				inputText = "";

			string controlName = "TagInput_" + path;
			GUI.SetNextControlName(controlName);
			string newInput = EditorGUI.TextField(inputRect, inputText);

			// Placeholder overlay when field is empty and unfocused
			if (string.IsNullOrEmpty(inputText) && GUI.GetNameOfFocusedControl() != controlName)
				GUI.Label(inputRect, "Add tag…", _placeholderStyle);

			if (newInput != inputText)
			{
				inputText = newInput;
				_inputs[path] = inputText;
			}

			string trimmed = inputText?.Trim() ?? "";
			bool canAdd    = !string.IsNullOrEmpty(trimmed) && !tags.Contains(trimmed);

			// Enter key → commit typed tag
			if (GUI.GetNameOfFocusedControl() == controlName
				&& canAdd
				&& Event.current.type == EventType.KeyDown
				&& Event.current.keyCode == KeyCode.Return)
			{
				CommitTag(tagsProperty, trimmed, path);
				_inputs[path] = "";
				Event.current.Use();
			}

			// Picker button → dropdown with existing tags filtered by current input
			var pickerContent = new GUIContent(EditorIcon.Plus, "Add typed tag or pick an existing one");
			if (GUI.Button(pickerRect, pickerContent, EditorStyles.miniButton))
				ShowPickerMenu(tagsProperty, tags, path, trimmed);

			EditorGUI.EndProperty();
		}

		// ── Drawing ───────────────────────────────────────────────────────────

		private static void DrawChip(in ChipInfo chip, float x, float y, float lineH,
			SerializedProperty tagsProperty, ref bool needRebuild)
		{
			var chipRect = new Rect(x, y, chip.Width, lineH);

			GUI.backgroundColor = chip.Color;
			GUI.Label(chipRect, chip.Tag, _chipStyle);
			GUI.backgroundColor = Color.white;

			// Delete button, overlaid on the chip's right side (editable mode only)
			if (GUI.enabled)
			{
				var delRect = new Rect(chipRect.xMax - DeleteBtnW - 1f, chipRect.y + 3f, DeleteBtnW - 2f, lineH - 6f);
				Color prevContent = GUI.contentColor;
				GUI.contentColor  = new Color(0.15f, 0.15f, 0.15f, 0.85f);
				if (GUI.Button(delRect, EditorIcon.Cross, GUIStyle.none))
				{
					tagsProperty.DeleteArrayElementAtIndex(chip.Index);
					needRebuild = true;
				}
				GUI.contentColor = prevContent;
			}
		}

		// ── Picker menu ───────────────────────────────────────────────────────

		private static void ShowPickerMenu(SerializedProperty tagsProperty, Tags tags, string path, string filter)
		{
			var knownTags   = TagRegistry.GetKnownTags();
			string lowerFilter = filter.ToLowerInvariant();

			var menu     = new GenericMenu();
			bool hasItems = false;

			// "Create new tag" entry for whatever is currently typed
			if (!string.IsNullOrEmpty(filter) && !tags.Contains(filter))
			{
				string captured = filter;
				menu.AddItem(new GUIContent($"New \"{captured}\""), false, () =>
				{
					CommitTag(tagsProperty, captured, path);
					_inputs[path] = "";
				});
				hasItems = true;
			}

			// Existing tags not yet applied, optionally filtered by input text
			var suggestions = knownTags
				.Where(t => !tags.Contains(t)
					&& (string.IsNullOrEmpty(filter) || t.ToLowerInvariant().Contains(lowerFilter)))
				.ToArray();

			if (suggestions.Length > 0)
			{
				if (hasItems) menu.AddSeparator("");
				foreach (var tag in suggestions)
				{
					string captured = tag;
					menu.AddItem(new GUIContent(captured), false, () =>
					{
						CommitTag(tagsProperty, captured, path);
						_inputs[path] = "";
					});
					hasItems = true;
				}
			}

			if (!hasItems)
				menu.AddDisabledItem(new GUIContent("No tags available"));

			menu.ShowAsContext();
		}

		// ── Commit ────────────────────────────────────────────────────────────

		private static void CommitTag(SerializedProperty tagsProperty, string tag, string path)
		{
			tagsProperty.arraySize++;
			tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tag;
			tagsProperty.serializedObject.ApplyModifiedProperties();
			_layouts.Remove(path);
			TagRegistry.Invalidate();
		}

		// ── Layout ────────────────────────────────────────────────────────────

		private static List<List<ChipInfo>> GetOrBuildLayout(
			string path, SerializedProperty tagsProperty, float contentWidth)
		{
			if (_layouts.TryGetValue(path, out var cached)
				&& Mathf.Abs(cached.contentWidth - contentWidth) < 1f)
				return cached.rows;

			var rows = BuildLayout(tagsProperty, contentWidth);
			_layouts[path] = (contentWidth, rows);
			return rows;
		}

		private static List<List<ChipInfo>> BuildLayout(SerializedProperty tagsProperty, float contentWidth)
		{
			EnsureStyles();
			var rows = new List<List<ChipInfo>>();
			if (tagsProperty.arraySize == 0) return rows;

			var  currentRow = new List<ChipInfo>();
			rows.Add(currentRow);
			float rowUsed = 0f;

			for (int i = 0; i < tagsProperty.arraySize; i++)
			{
				string tag    = tagsProperty.GetArrayElementAtIndex(i).stringValue;
				float chipW   = Mathf.Max(_chipStyle.CalcSize(new GUIContent(tag)).x, MinChipWidth);
				var chip      = new ChipInfo(tag, i, chipW, ChipColor(tag));

				if (currentRow.Count > 0 && rowUsed + chipW > contentWidth)
				{
					currentRow = new List<ChipInfo>();
					rows.Add(currentRow);
					rowUsed = 0f;
				}

				currentRow.Add(chip);
				rowUsed += chipW + ChipSpacingX;
			}

			return rows;
		}

		// ── Color ─────────────────────────────────────────────────────────────

		// Deterministic pastel color derived from the tag's hash code.
		// Does not modify Unity's global Random state.
		private static Color ChipColor(string tag)
		{
			uint hash = (uint)tag.GetHashCode();
			float h = (hash % 360u) / 360f;
			return Color.HSVToRGB(h, 0.42f, 0.90f);
		}

		// ── Data ──────────────────────────────────────────────────────────────

		private readonly struct ChipInfo
		{
			public readonly string Tag;
			public readonly int    Index;
			public readonly float  Width;
			public readonly Color  Color;

			public ChipInfo(string tag, int index, float width, Color color) =>
				(Tag, Index, Width, Color) = (tag, index, width, color);
		}
	}
}
