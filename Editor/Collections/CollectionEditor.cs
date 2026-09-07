using BlueCheese.Core.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomEditor(typeof(Collection<>), editorForChildClasses: true)]
	public class CollectionEditor : AssetBaseEditor
	{
		private ReorderableList _itemsList;
		private SerializedProperty _itemsProperty;
		private System.Reflection.PropertyInfo _itemIndexer;
		private System.Reflection.MethodInfo _searchFilterMethod;
		private string _searchFilter = string.Empty;
		private bool _isEditable = true;

		override protected void OnEnable()
		{
			base.OnEnable();

			_itemsProperty = serializedObject.FindProperty("_items");
			if (_itemsProperty == null) return;

			// Reflect through the public indexer/SearchFilter/IsEditable so the most-derived override
			// runs, even though this editor is not generic and doesn't know the closed T at compile time.
			var targetType = target.GetType();
			_itemIndexer = targetType.GetProperty("Item");
			_searchFilterMethod = targetType.GetMethod("SearchFilter");
			var isEditableProperty = targetType.GetProperty("IsEditable");
			_isEditable = isEditableProperty == null || (bool)isEditableProperty.GetValue(target);

			// Headerless reorderable list: keeps add/remove/reorder but hides the "Items" foldout.
			// Non-editable collections (e.g. AutoCollection) disable add/remove/drag/value editing.
			_itemsList = new ReorderableList(serializedObject, _itemsProperty,
				draggable: _isEditable, displayHeader: false, displayAddButton: _isEditable, displayRemoveButton: _isEditable)
			{
				drawElementCallback = (rect, index, isActive, isFocused) =>
				{
					if (!PassesSearchFilter(index)) return;

					var element = _itemsProperty.GetArrayElementAtIndex(index);
					rect.y += 2;
					rect.height = GetItemHeight(element, index);
					DrawItem(rect, element, index);
				},
				elementHeightCallback = index =>
					PassesSearchFilter(index)
						? GetItemHeight(_itemsProperty.GetArrayElementAtIndex(index), index) + 4
						: 0f,
			};
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();
			if (_itemsProperty != null && _itemsProperty.arraySize > 0)
				DrawSearchField();
			_itemsList?.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// Draws a single item of the list. Override to customize how an item is rendered,
		/// e.g. to show fields of a referenced ScriptableObject instead of just the reference field.
		/// </summary>
		protected virtual void DrawItem(Rect rect, SerializedProperty element, int index)
		{
			using (new EditorGUI.DisabledScope(!_isEditable))
			{
				EditorGUI.PropertyField(rect, element, GUIContent.none, includeChildren: true);
			}
		}

		/// <summary>
		/// Returns the height needed to draw the item at the given index. Override alongside DrawItem.
		/// </summary>
		protected virtual float GetItemHeight(SerializedProperty element, int index)
		{
			return EditorGUI.GetPropertyHeight(element, includeChildren: true);
		}

		private void DrawSearchField()
		{
			EditorGUILayout.BeginHorizontal();
			_searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
			using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_searchFilter)))
			{
				if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20)))
				{
					_searchFilter = string.Empty;
					GUI.FocusControl(null);
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		// Filtered-out elements are drawn with zero height rather than removed from the list, so the
		// underlying array indices stay correct for add/remove/reorder.
		private bool PassesSearchFilter(int index)
		{
			if (string.IsNullOrEmpty(_searchFilter) || _itemIndexer == null || _searchFilterMethod == null)
				return true;

			var item = _itemIndexer.GetValue(target, new object[] { index });
			return (bool)_searchFilterMethod.Invoke(target, new object[] { item, _searchFilter });
		}
	}
}
