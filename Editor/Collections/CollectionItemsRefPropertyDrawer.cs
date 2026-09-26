using BlueCheese.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// Inspector drawer for <see cref="CollectionItemsRef{T}"/>: an object field for the source
	/// collection, then the picked items as removable chips, plus a filtered add picker sourced from
	/// the collection's current content. Mirrors <see cref="TagsPropertyDrawer"/>'s chip/layout code,
	/// adapted for object references instead of strings -- there is no "type to create" affordance
	/// here, since items must already exist in the source collection.
	/// </summary>
	[CustomPropertyDrawer(typeof(CollectionItemsRef<>))]
	public class CollectionItemsRefPropertyDrawer : PropertyDrawer
	{
		// ── Constants ────────────────────────────────────────────────────────

		private const float ChipSpacingX = 4f;
		private const float LineSpacingY = 2f;
		private const float DeleteBtnW = 14f;
		private const float PickerBtnW = 22f;
		private const float MinChipWidth = 48f;

		// ── Shared styles (lazy, static — valid after skin is loaded) ────────

		private static GUIStyle _chipStyle;
		private static GUIStyle _placeholderStyle;

		private static void EnsureStyles()
		{
			if (_chipStyle != null) return;

			_chipStyle = new GUIStyle(EditorStyles.miniButton)
			{
				alignment = TextAnchor.MiddleLeft,
				fontStyle = FontStyle.Normal,
				fontSize = 11,
				// right padding leaves room for the delete overlay
				padding = new RectOffset(6, (int)DeleteBtnW + 4, 1, 1),
				margin = new RectOffset(0, 0, 0, 0),
				fixedHeight = 0,
			};

			_placeholderStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.6f) },
				padding = new RectOffset(3, 0, 1, 0),
			};
		}

		// ── Per-property-path state (instance is shared across all fields of this type) ──

		// text currently in the "search items" input, keyed by property path
		private static readonly Dictionary<string, string> _inputs = new();

		// chip row layout cache, keyed by property path
		private static readonly Dictionary<string, (float contentWidth, List<List<ChipInfo>> rows)> _layouts = new();

		// ── PropertyDrawer API ───────────────────────────────────────────────

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			EnsureStyles();

			var collectionProperty = property.FindPropertyRelative("_collection");
			float lineH = EditorGUIUtility.singleLineHeight;

			// Collection field + a one-line hint, nothing else to show until a collection is assigned.
			if (collectionProperty.objectReferenceValue == null)
				return 2f * (lineH + LineSpacingY) - LineSpacingY;

			var itemsProperty = property.FindPropertyRelative("_items");
			// Use approximation before the first OnGUI sets an exact width (mirrors TagsPropertyDrawer).
			float contentWidth = Mathf.Max(1f, EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 22f);
			int chipRowCount = GetOrBuildLayout(property.propertyPath, itemsProperty, contentWidth).Count;

			int totalRows = 1 + chipRowCount + 1; // collection field + chip rows + add row
			return totalRows * (lineH + LineSpacingY) - LineSpacingY;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EnsureStyles();
			EditorGUI.BeginProperty(position, label, property);

			var collectionProperty = property.FindPropertyRelative("_collection");
			var itemsProperty = property.FindPropertyRelative("_items");
			string path = property.propertyPath;
			float lineH = EditorGUIUtility.singleLineHeight;
			float labelW = EditorGUIUtility.labelWidth;

			var cursor = new Rect(position.x, position.y, position.width, lineH);

			// ── Source collection field ─────────────────────────────────────────
			// EditorGUI.BeginChangeCheck/EndChangeCheck is Unity's own recommended way to detect an
			// object field's value changing -- it correctly covers every assignment path (drag & drop,
			// the object picker window's async callback, "None" via the context menu), unlike a manual
			// before/after reference comparison which can miss the picker's deferred callback.
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(cursor, collectionProperty, label);
			bool collectionChanged = EditorGUI.EndChangeCheck();
			cursor.y += lineH + LineSpacingY;

			// Everything below the collection field lines up under its value area (past the label
			// column), not under the prefix label -- matches where the object field itself starts.
			float valuesX = cursor.x + labelW;
			float valuesW = cursor.width - labelW;

			var collectionObj = collectionProperty.objectReferenceValue;
			if (collectionObj == null)
			{
				GUI.Label(new Rect(valuesX, cursor.y, valuesW, lineH), "Assign a collection above to pick items.", _placeholderStyle);
				EditorGUI.EndProperty();
				return;
			}

			var candidateItems = CollectionRefEditorUtility.GetCandidateItems(collectionObj);

			// Source collection swapped: drop any picked items no longer part of the new one.
			if (collectionChanged)
			{
				PruneMissingItems(itemsProperty, candidateItems);
				_layouts.Remove(path);
			}

			// ── Chip rows ─────────────────────────────────────────────────────
			var rows = GetOrBuildLayout(path, itemsProperty, valuesW);

			bool needRebuild = false;
			foreach (var row in rows)
			{
				float chipX = valuesX;
				foreach (var chip in row)
				{
					DrawChip(chip, chipX, cursor.y, lineH, itemsProperty, ref needRebuild);
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

			// ── Add row ──────────────────────────────────────────────────────────
			float inputW = valuesW - PickerBtnW - ChipSpacingX;
			var inputRect = new Rect(valuesX, cursor.y, inputW, lineH);
			var pickerRect = new Rect(inputRect.xMax + ChipSpacingX, cursor.y, PickerBtnW, lineH);

			if (!_inputs.TryGetValue(path, out string inputText))
				inputText = "";

			string controlName = "CollectionItemsRefInput_" + path;
			GUI.SetNextControlName(controlName);
			string newInput = EditorGUI.TextField(inputRect, inputText);

			if (string.IsNullOrEmpty(inputText) && GUI.GetNameOfFocusedControl() != controlName)
				GUI.Label(inputRect, "Search items…", _placeholderStyle);

			if (newInput != inputText)
			{
				inputText = newInput;
				_inputs[path] = inputText;
			}

			var pickerContent = new GUIContent(EditorIcon.Plus, "Add an item from the collection");
			using (new EditorGUI.DisabledScope(candidateItems.Length == 0))
			{
				if (GUI.Button(pickerRect, pickerContent, EditorStyles.miniButton))
					ShowPickerMenu(itemsProperty, candidateItems, path, inputText?.Trim() ?? "");
			}

			EditorGUI.EndProperty();
		}

		// ── Drawing ───────────────────────────────────────────────────────────

		private static void DrawChip(in ChipInfo chip, float x, float y, float lineH,
			SerializedProperty itemsProperty, ref bool needRebuild)
		{
			var chipRect = new Rect(x, y, chip.Width, lineH);
			GUI.Label(chipRect, chip.Label, _chipStyle);

			// Delete button, overlaid on the chip's right side.
			var delRect = new Rect(chipRect.xMax - DeleteBtnW - 1f, chipRect.y + 3f, DeleteBtnW - 2f, lineH - 6f);
			Color prevContent = GUI.contentColor;
			GUI.contentColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
			if (GUI.Button(delRect, EditorIcon.Cross, GUIStyle.none))
			{
				itemsProperty.DeleteArrayElementAtIndex(chip.Index);
				needRebuild = true;
			}
			GUI.contentColor = prevContent;
		}

		// ── Picker menu ───────────────────────────────────────────────────────

		private static void ShowPickerMenu(SerializedProperty itemsProperty,
			UnityEngine.Object[] candidateItems, string path, string filter)
		{
			var alreadySelected = new HashSet<UnityEngine.Object>();
			for (int i = 0; i < itemsProperty.arraySize; i++)
				alreadySelected.Add(itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue);

			string lowerFilter = filter.ToLowerInvariant();
			var suggestions = candidateItems
				.Where(item => !alreadySelected.Contains(item))
				.Where(item => string.IsNullOrEmpty(filter)
					|| CollectionRefEditorUtility.GetDisplayName(item).ToLowerInvariant().Contains(lowerFilter))
				.ToArray();

			var menu = new GenericMenu();

			if (suggestions.Length == 0)
			{
				menu.AddDisabledItem(new GUIContent(candidateItems.Length == 0 ? "Collection is empty" : "No matching items"));
			}
			else
			{
				foreach (var item in suggestions)
				{
					var captured = item;
					menu.AddItem(new GUIContent(CollectionRefEditorUtility.GetDisplayName(item)), false, () =>
					{
						itemsProperty.serializedObject.Update();
						itemsProperty.InsertArrayElementAtIndex(itemsProperty.arraySize);
						itemsProperty.GetArrayElementAtIndex(itemsProperty.arraySize - 1).objectReferenceValue = captured;
						itemsProperty.serializedObject.ApplyModifiedProperties();
						_inputs[path] = "";
						_layouts.Remove(path);
					});
				}
			}

			menu.ShowAsContext();
		}

		// ── Pruning ───────────────────────────────────────────────────────────

		// Drops any selected item no longer present in the (possibly just swapped) source collection.
		private static void PruneMissingItems(SerializedProperty itemsProperty, UnityEngine.Object[] candidateItems)
		{
			for (int i = itemsProperty.arraySize - 1; i >= 0; i--)
			{
				var current = itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
				if (current == null || System.Array.IndexOf(candidateItems, current) < 0)
					itemsProperty.DeleteArrayElementAtIndex(i);
			}
		}

		// ── Layout ────────────────────────────────────────────────────────────

		private static List<List<ChipInfo>> GetOrBuildLayout(
			string path, SerializedProperty itemsProperty, float contentWidth)
		{
			if (_layouts.TryGetValue(path, out var cached)
				&& Mathf.Abs(cached.contentWidth - contentWidth) < 1f)
				return cached.rows;

			var rows = BuildLayout(itemsProperty, contentWidth);
			_layouts[path] = (contentWidth, rows);
			return rows;
		}

		private static List<List<ChipInfo>> BuildLayout(SerializedProperty itemsProperty, float contentWidth)
		{
			EnsureStyles();
			var rows = new List<List<ChipInfo>>();
			if (itemsProperty.arraySize == 0) return rows;

			var currentRow = new List<ChipInfo>();
			rows.Add(currentRow);
			float rowUsed = 0f;

			for (int i = 0; i < itemsProperty.arraySize; i++)
			{
				var item = itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
				string label = CollectionRefEditorUtility.GetDisplayName(item);
				float chipW = Mathf.Max(_chipStyle.CalcSize(new GUIContent(label)).x, MinChipWidth);
				var chip = new ChipInfo(label, i, chipW);

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

		// ── Data ──────────────────────────────────────────────────────────────

		private readonly struct ChipInfo
		{
			public readonly string Label;
			public readonly int Index;
			public readonly float Width;

			public ChipInfo(string label, int index, float width) =>
				(Label, Index, Width) = (label, index, width);
		}
	}
}
