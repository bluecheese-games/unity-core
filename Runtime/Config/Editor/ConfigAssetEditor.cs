//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System.CodeDom.Compiler;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BlueCheese.Core.Config.Editor
{
    [CustomEditor(typeof(ConfigAsset))]
    public class ConfigAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _itemsProperty;
        private ReorderableList _reorderableList;
        private string _searchString;
        private bool _hasDuplicates;
        private bool _hasInvalidKey;
        private HashSet<string> _keys = new HashSet<string>();

        void OnEnable()
        {
            _itemsProperty = serializedObject.FindProperty(nameof(ConfigAsset.Items));

            _reorderableList = new ReorderableList(serializedObject, _itemsProperty, true, true, true, true);

            _reorderableList.drawHeaderCallback += DrawHeader;
            _reorderableList.drawElementCallback += DrawItem;
            _reorderableList.elementHeightCallback += index => EditorGUIUtility.singleLineHeight + 2;
        }

        private void DrawHeader(Rect rect)
        {
            float totalWidth = rect.width - 22f;
            rect.x += 16f;
            EditorGUI.LabelField(rect, "Type");
            rect.x += totalWidth * .25f + 3f;
            EditorGUI.LabelField(rect, "Key");
            rect.x += totalWidth * .35f + 3f;
            EditorGUI.LabelField(rect, "Value");

            _keys.Clear();
            _hasDuplicates = false;
            _hasInvalidKey = false;
        }

        private void DrawItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);

            SerializedProperty keyProperty = element.FindPropertyRelative(nameof(ConfigItem.Key));
            SerializedProperty typeProperty = element.FindPropertyRelative(nameof(ConfigItem.Type));
            SerializedProperty stringValueProperty = element.FindPropertyRelative(nameof(ConfigItem.StringValue));
            SerializedProperty intValueProperty = element.FindPropertyRelative(nameof(ConfigItem.IntValue));
            SerializedProperty floatValueProperty = element.FindPropertyRelative(nameof(ConfigItem.FloatValue));
            SerializedProperty boolValueProperty = element.FindPropertyRelative(nameof(ConfigItem.BoolValue));
            SerializedProperty objectValueProperty = element.FindPropertyRelative(nameof(ConfigItem.ObjectValue));

            string key = keyProperty.stringValue;
            bool isSearched = !string.IsNullOrWhiteSpace(_searchString) && key.ToLower().Contains(_searchString.ToLower());

            GUI.color = isSearched ? Color.cyan : Color.white;

            // Calculate the rect for the Key field
            float totalWidth = rect.width - 6f;
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            rect.width = totalWidth * .25f;
            EditorGUI.PropertyField(rect, typeProperty, GUIContent.none);

            // check duplicate keys
            if (_keys.Contains(key))
            {
                GUI.color = Color.yellow;
                _hasDuplicates = true;
            }
            if (!CodeGenerator.IsValidLanguageIndependentIdentifier(key))
            {
                GUI.color = Color.red;
                _hasInvalidKey = true;
            }

            rect.x += rect.width + 3f;
            rect.width = totalWidth * .35f;
            EditorGUI.PropertyField(rect, keyProperty, GUIContent.none);

            GUI.color = Color.white;

            rect.x += rect.width + 3f;
            rect.width = totalWidth * .40f;

            ConfigItem.ValueType type = (ConfigItem.ValueType)typeProperty.enumValueIndex;

            switch (type)
            {
                case ConfigItem.ValueType.String:
                    EditorGUI.PropertyField(rect, stringValueProperty, GUIContent.none);
                    break;
                case ConfigItem.ValueType.Int:
                    EditorGUI.PropertyField(rect, intValueProperty, GUIContent.none);
                    break;
                case ConfigItem.ValueType.Float:
                    EditorGUI.PropertyField(rect, floatValueProperty, GUIContent.none);
                    break;
                case ConfigItem.ValueType.Boolean:
                    EditorGUI.PropertyField(rect, boolValueProperty, GUIContent.none);
                    break;
                case ConfigItem.ValueType.Object:
                    EditorGUI.PropertyField(rect, objectValueProperty, GUIContent.none);
                    break;
            }
            _keys.Add(key);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            _searchString = EditorGUILayout.TextField("Search", _searchString);

            EditorGUILayout.Space();
            _reorderableList.DoLayoutList();

            if (_hasDuplicates)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Please remove duplicate keys", MessageType.Warning);
            }

            if (_hasInvalidKey)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Keys must start with a letter, an underscore (_), or at symbol (@)." +
                    "\nIt can't contain any punctuation marks, symbols, or spaces." +
                    "\nIt can't be a C# reserved keyword", MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
