using System;
using UnityEngine;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Displays the field normally but grays it out (non-editable). Useful for exposing
	/// computed/debug values in the inspector.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ReadOnlyAttribute : PropertyAttribute
	{
	}
}
