using System;
using UnityEngine;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Draws a warning box below the field when it's left unassigned (null object reference,
	/// or an empty/whitespace string). Doesn't block anything — purely a visual reminder.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class RequiredAttribute : PropertyAttribute
	{
		public string Message { get; }

		public RequiredAttribute(string message = null)
		{
			Message = message;
		}
	}
}
