using System;
using System.Collections.Generic;
using System.Reflection;
using BlueCheese.Core.Attributes;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	[CustomPropertyDrawer(typeof(ValidateInputAttribute))]
	public class ValidateInputDrawer : PropertyDrawer
	{
		// Cached per (declaring type, method name): avoids re-resolving the MethodInfo via
		// reflection on every OnGUI/repaint call.
		private static readonly Dictionary<(Type, string), MethodInfo> _methodCache = new();

		private const float WarningHeight = 20f;
		private const float Spacing = 2f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUI.GetPropertyHeight(property, label, true);
			if (!IsValid(property))
			{
				height += WarningHeight + Spacing;
			}
			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			float fieldHeight = EditorGUI.GetPropertyHeight(property, label, true);
			var fieldRect = new Rect(position.x, position.y, position.width, fieldHeight);
			EditorGUI.PropertyField(fieldRect, property, label, true);

			if (!IsValid(property))
			{
				var validate = (ValidateInputAttribute)attribute;
				string message = string.IsNullOrEmpty(validate.Message)
					? $"{ObjectNames.NicifyVariableName(fieldInfo.Name)} is invalid."
					: validate.Message;

				var warningRect = new Rect(position.x, fieldRect.yMax + Spacing, position.width, WarningHeight);
				EditorGUI.HelpBox(warningRect, message, MessageType.Error);
			}
		}

		private bool IsValid(SerializedProperty property)
		{
			var validate = (ValidateInputAttribute)attribute;
			object target = property.serializedObject.targetObject;
			object value = fieldInfo.GetValue(target);

			var method = GetOrResolveMethod(target.GetType(), validate.CallbackMemberName, fieldInfo.FieldType);
			if (method == null)
			{
				// Fail open: a missing callback shouldn't hide the field behind a permanent error.
				return true;
			}

			return method.Invoke(target, new[] { value }) is true;
		}

		private static MethodInfo GetOrResolveMethod(Type type, string methodName, Type paramType)
		{
			var key = (type, methodName);
			if (_methodCache.TryGetValue(key, out MethodInfo cached))
			{
				return cached;
			}

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			MethodInfo method = type.GetMethod(methodName, flags, null, new[] { paramType }, null);

			if (method == null)
			{
				Debug.LogWarning($"[ValidateInput] Method '{methodName}({paramType.Name})' not found on {type.Name}.");
			}

			_methodCache[key] = method;
			return method;
		}
	}
}
