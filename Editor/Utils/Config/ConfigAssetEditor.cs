using BlueCheese.Core.Editor;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Core.Config.Editor
{
    // Non-fallback and for the exact type: takes priority over AssetBaseEditor's
    // editorForChildClasses editor, while still inheriting its Name/Tags/LoadMode header.
    [CustomEditor(typeof(ConfigAsset))]
    public class ConfigAssetEditor : AssetBaseEditor
    {
        private const int RowHeight = 22;
        private const int FieldHeight = 18;
        private const int MaxListHeight = 320;

        private static readonly List<string> _typeChoices = Enum.GetNames(typeof(ConfigItem.ValueType)).ToList();
        private static readonly Color _frameBorderColor = new Color(0f, 0f, 0f, 0.3f);

        private SerializedProperty _itemsProperty;
        private ListView _listView;
        private Toggle _selectAllToggle;
        private Button _deleteSelectedButton;
        private Label _selectedCountLabel;
        private HelpBox _duplicateWarning;
        private HelpBox _invalidKeyWarning;

        private string _searchString = string.Empty;
        private readonly HashSet<string> _selectedKeys = new HashSet<string>();
        private readonly HashSet<string> _duplicateKeys = new HashSet<string>();
        private readonly List<int> _filteredIndices = new List<int>();

        public override VisualElement CreateInspectorGUI()
        {
            _itemsProperty = serializedObject.FindProperty(nameof(ConfigAsset.Items));

            var root = new VisualElement { style = { paddingTop = 4, paddingBottom = 4, paddingLeft = 4, paddingRight = 4 } };

            root.Add(BuildAddSection());
            root.Add(BuildSearchRow());
            root.Add(BuildHeader());
            root.Add(BuildListView());

            var bulkRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 4 } };
            _selectedCountLabel = new Label { style = { flexGrow = 1, opacity = 0.7f } };
            bulkRow.Add(_selectedCountLabel);
            _deleteSelectedButton = new Button(DeleteSelected) { text = "Delete Selected" };
            bulkRow.Add(_deleteSelectedButton);
            root.Add(bulkRow);

            _duplicateWarning = new HelpBox("Please remove duplicate keys.", HelpBoxMessageType.Warning) { style = { marginTop = 4, display = DisplayStyle.None } };
            root.Add(_duplicateWarning);

            _invalidKeyWarning = new HelpBox("Keys must start with a letter, an underscore (_), or at symbol (@)." +
                " It can't contain any punctuation marks, symbols, or spaces. It can't be a C# reserved keyword.",
                HelpBoxMessageType.Error) { style = { marginTop = 4, display = DisplayStyle.None } };
            root.Add(_invalidKeyWarning);

            RefreshList();

            return root;
        }

        #region Search / header / list

        private VisualElement BuildSearchRow()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };

            var search = new ToolbarSearchField { value = _searchString, tooltip = "Search key or value", style = { flexGrow = 1 } };
            search.RegisterValueChangedCallback(evt =>
            {
                _searchString = evt.newValue;
                RefreshList();
            });
            row.Add(search);

            return row;
        }

        private VisualElement BuildHeader()
        {
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 2, paddingRight = 2, marginBottom = 2 } };

            _selectAllToggle = StripLabel(new Toggle { style = { width = 18, flexShrink = 0 } });
            _selectAllToggle.RegisterValueChangedCallback(evt => ToggleSelectAll(evt.newValue));
            header.Add(_selectAllToggle);

            header.Add(new Label("Key") { style = { flexGrow = 1, flexBasis = 0, minWidth = 80, marginRight = 3, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(new Label("Type") { style = { width = 90, marginRight = 3, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(new Label("Value") { style = { flexGrow = 1, flexBasis = 0, minWidth = 80, marginRight = 3, unityFontStyleAndWeight = FontStyle.Bold } });
            header.Add(new VisualElement { style = { width = 22, flexShrink = 0 } }); // spacer matching the per-row delete button

            return header;
        }

        private VisualElement BuildListView()
        {
            _listView = new ListView
            {
                itemsSource = _filteredIndices,
                makeItem = MakeRow,
                bindItem = BindRow,
                fixedItemHeight = RowHeight,
                selectionType = SelectionType.None,
                showBorder = true,
                reorderable = false,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                style = { marginBottom = 2 },
            };
            return _listView;
        }

        private VisualElement MakeRow()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 2, paddingRight = 2, height = RowHeight } };

            var toggle = StripLabel(new Toggle { name = "select", style = { width = 18, flexShrink = 0 } });
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (row.userData is not int index) return;
                string key = _itemsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(ConfigItem.Key)).stringValue;
                if (evt.newValue) _selectedKeys.Add(key);
                else _selectedKeys.Remove(key);
                UpdateSelectionUI();
            });
            row.Add(toggle);

            var keyField = StripLabel(new TextField { name = "key", style = { flexGrow = 1, flexBasis = 0, minWidth = 80, height = FieldHeight, marginRight = 3 } });
            keyField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (row.userData is not int index) return;
                CommitKeyRename(index, keyField.value);
            });
            row.Add(keyField);

            var typeField = StripLabel(new DropdownField(_typeChoices, 0) { name = "type", style = { width = 90, height = FieldHeight, marginRight = 3 } });
            typeField.RegisterValueChangedCallback(evt =>
            {
                if (row.userData is not int index) return;
                var itemProperty = _itemsProperty.GetArrayElementAtIndex(index);
                itemProperty.FindPropertyRelative(nameof(ConfigItem.Type)).enumValueIndex = typeField.index;
                serializedObject.ApplyModifiedProperties();
                _listView.RefreshItems();
            });
            row.Add(typeField);

            row.Add(new VisualElement { name = "valueContainer", style = { flexGrow = 1, flexBasis = 0, minWidth = 80, marginRight = 3 } });

            var deleteButton = new Button { name = "delete", style = { width = 22, height = FieldHeight, flexShrink = 0, alignItems = Align.Center, justifyContent = Justify.Center } };
            deleteButton.Add(new Image { image = EditorIcon.Trash, style = { width = 12, height = 12 } });
            deleteButton.clicked += () =>
            {
                if (row.userData is not int index) return;
                DeleteItem(index);
            };
            row.Add(deleteButton);

            return row;
        }

        private void BindRow(VisualElement element, int displayIndex)
        {
            int index = _filteredIndices[displayIndex];
            element.userData = index;

            var itemProperty = _itemsProperty.GetArrayElementAtIndex(index);
            var keyProperty = itemProperty.FindPropertyRelative(nameof(ConfigItem.Key));
            var typeProperty = itemProperty.FindPropertyRelative(nameof(ConfigItem.Type));

            string key = keyProperty.stringValue;
            var type = (ConfigItem.ValueType)typeProperty.enumValueIndex;

            element.Q<Toggle>("select").SetValueWithoutNotify(_selectedKeys.Contains(key));

            var keyField = element.Q<TextField>("key");
            keyField.SetValueWithoutNotify(key);
            bool invalid = !CodeGenerator.IsValidLanguageIndependentIdentifier(key);
            bool duplicate = _duplicateKeys.Contains(key);
            SetFieldBackground(keyField, invalid
                ? new Color(0.9f, 0.3f, 0.3f, 0.35f)
                : duplicate
                    ? new Color(0.9f, 0.8f, 0.2f, 0.35f)
                    : Color.clear);

            var typeField = element.Q<DropdownField>("type");
            typeField.SetValueWithoutNotify(_typeChoices[(int)type]);

            var valueContainer = element.Q<VisualElement>("valueContainer");
            valueContainer.Clear();
            valueContainer.Add(BuildValueField(itemProperty, type, index));
        }

        private VisualElement BuildValueField(SerializedProperty itemProperty, ConfigItem.ValueType type, int index)
        {
            VisualElement field;
            switch (type)
            {
                case ConfigItem.ValueType.Int:
                    {
                        var intField = new IntegerField { value = itemProperty.FindPropertyRelative(nameof(ConfigItem.IntValue)).intValue };
                        intField.RegisterValueChangedCallback(evt => SetItemValue(index, nameof(ConfigItem.IntValue), p => p.intValue = evt.newValue));
                        field = intField;
                        break;
                    }
                case ConfigItem.ValueType.Float:
                    {
                        var floatField = new FloatField { value = itemProperty.FindPropertyRelative(nameof(ConfigItem.FloatValue)).floatValue };
                        floatField.RegisterValueChangedCallback(evt => SetItemValue(index, nameof(ConfigItem.FloatValue), p => p.floatValue = evt.newValue));
                        field = floatField;
                        break;
                    }
                case ConfigItem.ValueType.Boolean:
                    {
                        var boolField = new Toggle { value = itemProperty.FindPropertyRelative(nameof(ConfigItem.BoolValue)).boolValue };
                        boolField.RegisterValueChangedCallback(evt => SetItemValue(index, nameof(ConfigItem.BoolValue), p => p.boolValue = evt.newValue));
                        field = boolField;
                        break;
                    }
                case ConfigItem.ValueType.Object:
                    {
                        var objectField = new ObjectField { objectType = typeof(UnityEngine.Object), value = itemProperty.FindPropertyRelative(nameof(ConfigItem.ObjectValue)).objectReferenceValue };
                        objectField.RegisterValueChangedCallback(evt => SetItemValue(index, nameof(ConfigItem.ObjectValue), p => p.objectReferenceValue = evt.newValue));
                        field = objectField;
                        break;
                    }
                default:
                    {
                        var stringField = new TextField { value = itemProperty.FindPropertyRelative(nameof(ConfigItem.StringValue)).stringValue };
                        stringField.RegisterValueChangedCallback(evt => SetItemValue(index, nameof(ConfigItem.StringValue), p => p.stringValue = evt.newValue));
                        field = stringField;
                        break;
                    }
            }
            field.style.flexGrow = 1;
            field.style.height = FieldHeight;
            StripLabel(field);
            return field;
        }

        private void SetItemValue(int index, string propertyName, Action<SerializedProperty> apply)
        {
            var itemProperty = _itemsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(propertyName);
            apply(itemProperty);
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Mutations

        private void CommitKeyRename(int index, string newKey)
        {
            newKey = newKey?.Trim() ?? string.Empty;
            var keyProperty = _itemsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(ConfigItem.Key));
            if (keyProperty.stringValue == newKey) return;

            string oldKey = keyProperty.stringValue;
            keyProperty.stringValue = newKey;
            serializedObject.ApplyModifiedProperties();

            if (_selectedKeys.Remove(oldKey)) _selectedKeys.Add(newKey);

            RefreshList();
        }

        private void DeleteItem(int index)
        {
            string key = _itemsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(ConfigItem.Key)).stringValue;
            _itemsProperty.DeleteArrayElementAtIndex(index);
            _selectedKeys.Remove(key);
            serializedObject.ApplyModifiedProperties();
            RefreshList();
        }

        private void DeleteSelected()
        {
            if (_selectedKeys.Count == 0) return;

            for (int i = _itemsProperty.arraySize - 1; i >= 0; i--)
            {
                string key = _itemsProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ConfigItem.Key)).stringValue;
                if (_selectedKeys.Contains(key))
                {
                    _itemsProperty.DeleteArrayElementAtIndex(i);
                }
            }
            _selectedKeys.Clear();
            serializedObject.ApplyModifiedProperties();
            RefreshList();
        }

        private void ToggleSelectAll(bool value)
        {
            _selectedKeys.Clear();
            if (value)
            {
                foreach (int index in _filteredIndices)
                {
                    string key = _itemsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(ConfigItem.Key)).stringValue;
                    _selectedKeys.Add(key);
                }
            }
            UpdateSelectionUI();
        }

        #endregion

        #region Add new item

        private VisualElement BuildAddSection()
        {
            var section = new VisualElement
            {
                style =
                {
                    marginBottom = 10,
                    paddingTop = 6, paddingBottom = 8, paddingLeft = 8, paddingRight = 8,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = _frameBorderColor, borderBottomColor = _frameBorderColor,
                    borderLeftColor = _frameBorderColor, borderRightColor = _frameBorderColor,
                    borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                    backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.06f),
                },
            };
            section.Add(new Label("Add New Config") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var keyField = StripLabel(new TextField { style = { flexGrow = 1, flexBasis = 0, minWidth = 80, height = FieldHeight, marginRight = 3 } });
            row.Add(keyField);

            void UpdateKeyValidation()
            {
                string typedKey = keyField.value?.Trim();
                bool exists = !string.IsNullOrEmpty(typedKey) && KeyExists(typedKey);
                SetFieldBackground(keyField, exists ? new Color(0.9f, 0.3f, 0.3f, 0.35f) : Color.clear);
            }
            keyField.RegisterValueChangedCallback(_ => UpdateKeyValidation());

            var typeField = StripLabel(new DropdownField(_typeChoices, 0) { style = { width = 90, height = FieldHeight, marginRight = 3 } });
            row.Add(typeField);

            var valueContainer = new VisualElement { style = { flexGrow = 1, flexBasis = 0, minWidth = 80, marginRight = 3 } };
            row.Add(valueContainer);

            VisualElement valueControl = null;

            // Editor-hosted text fields consume the first Return keydown internally (to commit their
            // own pending edit) before it bubbles or gets converted to a NavigationSubmitEvent -- that
            // swallowed first press is exactly the "must press Enter twice" symptom. Registering with
            // TrickleDown.TrickleDown intercepts the event during the capture phase, which on the same
            // element runs before its own internal (target-phase) handling gets a chance to consume it.
            void OnEnter(KeyDownEvent evt)
            {
                if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                {
                    Commit();
                    evt.StopPropagation();
                }
            }

            void RebuildValueControl()
            {
                valueContainer.Clear();
                var type = (ConfigItem.ValueType)typeField.index;
                valueControl = type switch
                {
                    ConfigItem.ValueType.Int => new IntegerField(),
                    ConfigItem.ValueType.Float => new FloatField(),
                    ConfigItem.ValueType.Boolean => new Toggle(),
                    ConfigItem.ValueType.Object => new ObjectField { objectType = typeof(UnityEngine.Object) },
                    _ => new TextField(),
                };
                valueControl.style.flexGrow = 1;
                valueControl.style.height = FieldHeight;
                StripLabel(valueControl);
                valueControl.RegisterCallback<KeyDownEvent>(OnEnter, TrickleDown.TrickleDown);
                valueContainer.Add(valueControl);
            }

            void Commit()
            {
                string key = keyField.value?.Trim();
                if (string.IsNullOrEmpty(key))
                {
                    EditorUtility.DisplayDialog("Error", "Key can't be empty.", "Ok");
                    return;
                }
                if (!CodeGenerator.IsValidLanguageIndependentIdentifier(key))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid key. Keys must start with a letter, an underscore (_), or at symbol (@)," +
                        " and can't contain punctuation, symbols, spaces, or be a C# reserved keyword.", "Ok");
                    return;
                }
                if (KeyExists(key))
                {
                    EditorUtility.DisplayDialog("Error", "Key already exists.", "Ok");
                    return;
                }

                var type = (ConfigItem.ValueType)typeField.index;

                int newIndex = _itemsProperty.arraySize;
                _itemsProperty.InsertArrayElementAtIndex(newIndex);
                var itemProperty = _itemsProperty.GetArrayElementAtIndex(newIndex);
                itemProperty.FindPropertyRelative(nameof(ConfigItem.Key)).stringValue = key;
                itemProperty.FindPropertyRelative(nameof(ConfigItem.Type)).enumValueIndex = (int)type;
                itemProperty.FindPropertyRelative(nameof(ConfigItem.StringValue)).stringValue = type == ConfigItem.ValueType.String && valueControl is TextField stringField ? stringField.value : string.Empty;
                itemProperty.FindPropertyRelative(nameof(ConfigItem.IntValue)).intValue = type == ConfigItem.ValueType.Int && valueControl is IntegerField intField ? intField.value : 0;
                itemProperty.FindPropertyRelative(nameof(ConfigItem.FloatValue)).floatValue = type == ConfigItem.ValueType.Float && valueControl is FloatField floatField ? floatField.value : 0f;
                itemProperty.FindPropertyRelative(nameof(ConfigItem.BoolValue)).boolValue = type == ConfigItem.ValueType.Boolean && valueControl is Toggle boolField && boolField.value;
                itemProperty.FindPropertyRelative(nameof(ConfigItem.ObjectValue)).objectReferenceValue = type == ConfigItem.ValueType.Object && valueControl is ObjectField objectField ? objectField.value : null;

                serializedObject.ApplyModifiedProperties();

                keyField.value = string.Empty;
                RebuildValueControl();
                RefreshList();
                keyField.Focus();
            }

            RebuildValueControl();
            typeField.RegisterValueChangedCallback(_ => RebuildValueControl());

            var addButton = IconButton("Add", EditorIcon.Plus, Commit);
            keyField.RegisterCallback<KeyDownEvent>(OnEnter, TrickleDown.TrickleDown);

            row.Add(addButton);
            section.Add(row);

            return section;
        }

        private bool KeyExists(string key)
        {
            for (int i = 0; i < _itemsProperty.arraySize; i++)
            {
                if (_itemsProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ConfigItem.Key)).stringValue == key)
                {
                    return true;
                }
            }
            return false;
        }

        // Unity's field controls (TextField, DropdownField, IntegerField, ...) always reserve space
        // for a label -- even an empty one -- via the "unity-base-field__label" element. That extra
        // left indent is what desyncs a labelless field from a plain Label used as a column header.
        private static T StripLabel<T>(T field) where T : VisualElement
        {
            field.Q<Label>(className: "unity-base-field__label")?.RemoveFromHierarchy();
            return field;
        }

        // A BaseField's own background is invisible: the actual box you see is its "input" child
        // (unity-base-field__input), which paints over anything set on the field root itself.
        private static void SetFieldBackground(VisualElement field, Color color)
        {
            var input = field.Q(className: "unity-base-field__input") ?? field;
            input.style.backgroundColor = color;
        }

        private static Button IconButton(string text, Texture2D icon, Action onClick)
        {
            var button = onClick != null ? new Button(onClick) : new Button();
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            if (icon != null)
            {
                button.Add(new Image { image = icon, style = { width = 14, height = 14, marginRight = 4, flexShrink = 0 } });
            }
            button.Add(new Label(text));
            return button;
        }

        #endregion

        #region Refresh

        private void RefreshList()
        {
            int count = _itemsProperty.arraySize;

            _duplicateKeys.Clear();
            var seen = new HashSet<string>();
            bool hasInvalidKey = false;
            for (int i = 0; i < count; i++)
            {
                string key = _itemsProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ConfigItem.Key)).stringValue;
                if (!seen.Add(key)) _duplicateKeys.Add(key);
                if (!CodeGenerator.IsValidLanguageIndependentIdentifier(key)) hasInvalidKey = true;
            }

            _filteredIndices.Clear();
            for (int i = 0; i < count; i++)
            {
                var itemProperty = _itemsProperty.GetArrayElementAtIndex(i);
                if (MatchesSearch(itemProperty))
                {
                    _filteredIndices.Add(i);
                }
            }
            _filteredIndices.Sort((a, b) => string.Compare(
                _itemsProperty.GetArrayElementAtIndex(a).FindPropertyRelative(nameof(ConfigItem.Key)).stringValue,
                _itemsProperty.GetArrayElementAtIndex(b).FindPropertyRelative(nameof(ConfigItem.Key)).stringValue,
                StringComparison.OrdinalIgnoreCase));

            _listView.itemsSource = _filteredIndices;
            // +8 covers the list's own top/bottom border (showBorder) plus a little slack so the
            // last row's bottom edge never gets clipped by a too-tight viewport.
            int contentHeight = _filteredIndices.Count * RowHeight + 8;
            _listView.style.height = Mathf.Clamp(contentHeight, RowHeight + 8, MaxListHeight);
            _listView.RefreshItems();

            _duplicateWarning.style.display = _duplicateKeys.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _invalidKeyWarning.style.display = hasInvalidKey ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateSelectionUI();
        }

        private bool MatchesSearch(SerializedProperty itemProperty)
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;

            string key = itemProperty.FindPropertyRelative(nameof(ConfigItem.Key)).stringValue;
            if (key.IndexOf(_searchString, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            var type = (ConfigItem.ValueType)itemProperty.FindPropertyRelative(nameof(ConfigItem.Type)).enumValueIndex;
            string valueText = type switch
            {
                ConfigItem.ValueType.String => itemProperty.FindPropertyRelative(nameof(ConfigItem.StringValue)).stringValue,
                ConfigItem.ValueType.Int => itemProperty.FindPropertyRelative(nameof(ConfigItem.IntValue)).intValue.ToString(CultureInfo.InvariantCulture),
                ConfigItem.ValueType.Float => itemProperty.FindPropertyRelative(nameof(ConfigItem.FloatValue)).floatValue.ToString(CultureInfo.InvariantCulture),
                ConfigItem.ValueType.Boolean => itemProperty.FindPropertyRelative(nameof(ConfigItem.BoolValue)).boolValue.ToString(),
                ConfigItem.ValueType.Object => itemProperty.FindPropertyRelative(nameof(ConfigItem.ObjectValue)).objectReferenceValue != null
                    ? itemProperty.FindPropertyRelative(nameof(ConfigItem.ObjectValue)).objectReferenceValue.name
                    : string.Empty,
                _ => string.Empty,
            };
            return !string.IsNullOrEmpty(valueText) && valueText.IndexOf(_searchString, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void UpdateSelectionUI()
        {
            _selectAllToggle.SetValueWithoutNotify(_filteredIndices.Count > 0 && _filteredIndices.All(index =>
                _selectedKeys.Contains(_itemsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(ConfigItem.Key)).stringValue)));

            _selectedCountLabel.text = _selectedKeys.Count > 0 ? $"{_selectedKeys.Count} selected" : string.Empty;
            _deleteSelectedButton.SetEnabled(_selectedKeys.Count > 0);
            _deleteSelectedButton.text = _selectedKeys.Count > 0 ? $"Delete Selected ({_selectedKeys.Count})" : "Delete Selected";

            _listView.RefreshItems();
        }

        #endregion
    }
}
