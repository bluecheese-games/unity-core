using System;
using UnityEngine;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Draws an error box below the field when the given instance method on the same object
	/// returns false for the field's current value. The method must have the signature
	/// <c>bool MethodName(TFieldType value)</c> (public or private).
	/// Example: <c>[SerializeField, ValidateInput(nameof(IsPositive))] private float _speed;</c>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ValidateInputAttribute : PropertyAttribute
	{
		public string CallbackMemberName { get; }
		public string Message { get; }

		public ValidateInputAttribute(string callbackMemberName, string message = null)
		{
			CallbackMemberName = callbackMemberName;
			Message = message;
		}
	}
}
