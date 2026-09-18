using System;
using System.Collections.Generic;
using System.Reflection;
using BlueCheese.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// Shared reflection logic for ShowIf/HideIf/EnableIf/DisableIf drawers: resolves the
	/// condition member (cached per type+name) and compares it against the expected value.
	/// </summary>
	public abstract class ConditionDrawerBase : PropertyDrawer
	{
		private static readonly Dictionary<(Type, string), MemberInfo> _memberCache = new();

		protected bool EvaluateCondition(SerializedProperty property)
		{
			var condition = (ConditionAttribute)attribute;
			object target = property.serializedObject.targetObject;
			object value = GetMemberValue(target, condition.ConditionMemberName);

			if (value == null)
			{
				return condition.ExpectedValue == null;
			}

			return value.Equals(condition.ExpectedValue);
		}

		private static object GetMemberValue(object target, string memberName)
		{
			var type = target.GetType();
			var member = GetOrResolveMember(type, memberName);

			return member switch
			{
				FieldInfo field => field.GetValue(target),
				PropertyInfo property => property.GetValue(target),
				_ => null
			};
		}

		private static MemberInfo GetOrResolveMember(Type type, string memberName)
		{
			var key = (type, memberName);
			if (_memberCache.TryGetValue(key, out MemberInfo cached))
			{
				return cached;
			}

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			MemberInfo member = type.GetField(memberName, flags);
			member ??= type.GetProperty(memberName, flags);

			if (member == null)
			{
				Debug.LogWarning($"[Condition] Member '{memberName}' not found on {type.Name}.");
			}

			_memberCache[key] = member;
			return member;
		}
	}
}
