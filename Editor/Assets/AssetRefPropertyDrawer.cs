//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

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

		static AssetRefPropertyDrawer()
		{
			EditorApplication.projectChanged += _cache.Clear;
		}

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
				entry = (items.Select(i => i.Guid).ToArray(), items.Select(i => i.Name).ToArray());
				_cache[genericType] = entry;
			}

			EditorGUIHelper.DrawSearchableKeyProperty(idProperty, label, entry.keys, entry.labels);
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
