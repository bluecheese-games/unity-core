using BlueCheese.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(AssetRef<>))]
	public class AssetRefPropertyDrawer : PropertyDrawer
	{
		// Caches the dropdown entries per asset type to avoid querying AssetBank on every repaint.
		// Invalidated when the project changes (e.g. after an AssetBank regeneration).
		private static readonly Dictionary<Type, (string[] keys, string[] labels)> _cache = new();

		// Native Unity object-field selector button visual.
		private static GUIStyle _selectButtonStyle;

		static AssetRefPropertyDrawer()
		{
			EditorApplication.projectChanged += _cache.Clear;
		}

		private const float SelectButtonWidth = 19f;
		private const float SelectButtonSpacing = 2f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			=> EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var idProperty  = property.FindPropertyRelative("Guid");
			Type genericType = GetGenericType();

			if (genericType == null)
			{
				EditorGUI.PropertyField(position, idProperty, label);
				return;
			}

			if (!_cache.TryGetValue(genericType, out var entry))
			{
				var items = AssetBank.GetAllAssets()
					.Where(a => a.Type == genericType)
					.Select(a => (a.Guid, a.Name))
					.ToArray();
				// Prepend a "None" entry mapping to an empty Guid so the reference can be cleared.
				entry = (
					items.Select(i => i.Guid).Prepend(string.Empty).ToArray(),
					items.Select(i => i.Name).Prepend("None").ToArray()
				);
				_cache[genericType] = entry;
			}

			string guid = idProperty.stringValue;
			string assetPath = string.IsNullOrEmpty(guid) ? null : AssetDatabase.GUIDToAssetPath(guid);
			bool hasAsset = !string.IsNullOrEmpty(assetPath);

			_selectButtonStyle ??= GUI.skin.FindStyle("ObjectFieldButton") ?? new GUIStyle(EditorStyles.miniButton);

			// Split the line into the searchable field + a trailing "focus" button.
			var fieldRect = new Rect(position.x, position.y, position.width - SelectButtonWidth - SelectButtonSpacing, position.height);
			var buttonRect = new Rect(position.xMax - SelectButtonWidth, position.y, SelectButtonWidth, position.height);

			EditorGUIHelper.DrawSearchableKeyProperty(fieldRect, idProperty, label, entry.keys, entry.labels);

			using (new EditorGUI.DisabledScope(!hasAsset))
			{
				if (GUI.Button(buttonRect, new GUIContent(string.Empty, "Focus asset in Project"), _selectButtonStyle))
				{
					var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
					EditorGUIUtility.PingObject(asset);
					Selection.activeObject = asset;
				}
			}
		}

		private Type GetGenericType()
		{
			// Start from the declared field type (e.g. AssetRef<T>, List<AssetRef<T>>, AssetRefSubclass<T>…)
			Type t = fieldInfo.FieldType;

			if (t.IsArray)
				t = t.GetElementType();
			else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
				t = t.GetGenericArguments()[0];

			// Walk the inheritance chain until we find AssetRef<>
			for (Type cur = t; cur != null; cur = cur.BaseType)
				if (cur.IsGenericType && cur.GetGenericTypeDefinition() == typeof(AssetRef<>))
					return cur.GetGenericArguments()[0];

			return null;
		}
	}
}
