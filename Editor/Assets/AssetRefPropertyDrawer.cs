//
// Copyright (c) 2025 BlueCheese Games All rights reserved
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
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var idProperty = property.FindPropertyRelative("Guid");
			Type genericType = GetGenericType();
			var keyValues = AssetBank.GetAllAssets()
				.Where(asset => asset.Type == genericType)
				.Select(asset => (asset.Guid, asset.Name)).ToArray();

			string[] keys = keyValues.Select(kv => kv.Guid).ToArray();
			string[] labels = keyValues.Select(kv => kv.Name).ToArray();

			EditorGUIHelper.DrawSearchableKeyProperty(idProperty, label, keys, labels);
		}

		private Type GetGenericType()
		{
			// Start from the declared field type (e.g., AssetRef<T>, List<AssetRef<T>>, AssetRefSubclass<T>, etc.)
			Type t = fieldInfo.FieldType;

			// If the field is an array or a List<>, get the element type
			if (t.IsArray)
			{
				t = t.GetElementType();
			}
			else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
			{
				t = t.GetGenericArguments()[0];
			}

			// Walk the inheritance chain until we find AssetRef<>
			Type cur = t;
			while (cur != null)
			{
				if (cur.IsGenericType && cur.GetGenericTypeDefinition() == typeof(AssetRef<>))
				{
					return cur.GetGenericArguments()[0];
				}
				cur = cur.BaseType;
			}

			// If we couldn't resolve, return null (caller can handle this as an error)
			return null;
		}
	}
}
