using BlueCheese.Core.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomEditor(typeof(Collection<>), editorForChildClasses: true)]
	public class CollectionEditor : AssetBaseEditor
	{
		protected ReorderableList _itemsList;
		protected SerializedProperty _itemsProperty;
		private SerializedProperty _searchScopeProperty;
		private SerializedProperty _searchFoldersProperty;
		private string _autoCollectionItemTypeName;
		private System.Reflection.PropertyInfo _itemIndexer;
		private System.Reflection.MethodInfo _searchFilterMethod;
		private string _searchFilter = string.Empty;
		private bool _isEditable = true;

		override protected void OnEnable()
		{
			base.OnEnable();

			// Only present on AutoCollection<T> subclasses; null for plain Collection<T> ones.
			_searchScopeProperty = serializedObject.FindProperty("_searchScope");
			_searchFoldersProperty = serializedObject.FindProperty("_searchFolders");
			if (_searchScopeProperty != null)
				_autoCollectionItemTypeName = GetAutoCollectionItemTypeName(target.GetType());

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
			if (_searchScopeProperty != null)
				DrawAutoCollectionSettings();
			else
				base.OnInspectorGUI();

			DrawItemsList();
		}

		// AutoCollection<T> only: drawn manually (instead of via the default full-object inspector
		// used above) so Search Folders can be hidden unless Search Scope is SpecificFolders.
		private void DrawAutoCollectionSettings()
		{
			EditorGUILayout.HelpBox(
				$"This collection is automatically populated with all assets of type {_autoCollectionItemTypeName} in the project.\n" +
				"Editing the items list manually is not supported.",
				MessageType.Info);

			serializedObject.Update();

			int previousScope = _searchScopeProperty.enumValueIndex;
			string previousFolders = SerializeFolders(_searchFoldersProperty);

			EditorGUILayout.PropertyField(_searchScopeProperty);
			if (_searchFoldersProperty != null &&
				_searchScopeProperty.enumNames[_searchScopeProperty.enumValueIndex] == "SpecificFolders")
			{
				EditorGUILayout.PropertyField(_searchFoldersProperty, true);
			}

			// Compared by actual value, not GUI.changed/ApplyModifiedProperties()'s return value:
			// both react to any interaction with the array control -- including just expanding or
			// collapsing the Search Folders foldout, or firing twice for one popup selection across
			// the Layout/Repaint passes -- neither of which should retrigger a regen.
			bool changed = _searchScopeProperty.enumValueIndex != previousScope
				|| SerializeFolders(_searchFoldersProperty) != previousFolders;

			serializedObject.ApplyModifiedProperties();

			// Mirrors AssetBaseEditor.OnHeaderGUI: these fields drive what OnRegister() finds, so an
			// actual value change here should re-run the same debounced regen as editing Name/Tags/
			// LoadMode does.
			if (changed)
				EditorApplication.delayCall += AssetBankGenerator.Regenerate;
		}

		// Flattens a string[] SerializedProperty into a single comparable value.
		private static string SerializeFolders(SerializedProperty arrayProperty)
		{
			if (arrayProperty == null) return string.Empty;

			var parts = new string[arrayProperty.arraySize];
			for (int i = 0; i < arrayProperty.arraySize; i++)
				parts[i] = arrayProperty.GetArrayElementAtIndex(i).stringValue;
			return string.Join("|", parts);
		}

		// Walks up from the concrete leaf type (e.g. FXCollection) to find its closed AutoCollection<T>
		// ancestor and returns T's name, for the "populated with all assets of type X" message.
		private static string GetAutoCollectionItemTypeName(System.Type type)
		{
			while (type != null)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AutoCollection<>))
					return type.GetGenericArguments()[0].Name;
				type = type.BaseType;
			}
			return "?";
		}

		/// <summary>
		/// Draws the search field (if any items) and the reorderable items list.
		/// Split out so derived editors can insert their own fields before it without
		/// pulling in the default full-object inspector drawn by <see cref="OnInspectorGUI"/>.
		/// </summary>
		protected void DrawItemsList()
		{
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
