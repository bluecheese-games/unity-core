using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueCheese.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(DropdownAttribute))]
	public class DropdownDrawer : PropertyDrawer
	{
		// Cached per (type, member name), same rationale as ShowIfDrawer: avoid re-resolving
		// the MethodInfo/PropertyInfo via reflection on every OnGUI/repaint call.
		private static readonly Dictionary<(Type, string), MemberInfo> _memberCache = new();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var dropdown = (DropdownAttribute)attribute;

			if (property.propertyType != SerializedPropertyType.String)
			{
				EditorGUI.PropertyField(position, property, label);
				return;
			}

			string[] values = GetValues(property.serializedObject.targetObject, dropdown.ValuesMemberName);
			if (values == null || values.Length == 0)
			{
				EditorGUI.PropertyField(position, property, label);
				return;
			}

			int currentIndex = Array.IndexOf(values, property.stringValue);
			if (currentIndex < 0)
			{
				currentIndex = 0;
			}

			EditorGUI.BeginProperty(position, label, property);
			int newIndex = EditorGUI.Popup(position, label.text, currentIndex, values);
			if (newIndex != currentIndex)
			{
				property.stringValue = values[newIndex];
			}
			EditorGUI.EndProperty();
		}

		private static string[] GetValues(object target, string memberName)
		{
			var type = target.GetType();
			var member = GetOrResolveMember(type, memberName);

			object result = member switch
			{
				MethodInfo method => method.Invoke(target, null),
				PropertyInfo property => property.GetValue(target),
				_ => null
			};

			return (result as IEnumerable<string>)?.ToArray();
		}

		private static MemberInfo GetOrResolveMember(Type type, string memberName)
		{
			var key = (type, memberName);
			if (_memberCache.TryGetValue(key, out MemberInfo cached))
			{
				return cached;
			}

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			MemberInfo member = type.GetMethod(memberName, flags, null, Type.EmptyTypes, null);
			member ??= type.GetProperty(memberName, flags);

			if (member == null)
			{
				Debug.LogWarning($"[Dropdown] Member '{memberName}' not found on {type.Name}.");
			}

			_memberCache[key] = member;
			return member;
		}
	}
}
