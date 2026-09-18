using System;
using UnityEngine;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Draws a string field as a dropdown populated by an instance method or property (named
	/// <see cref="ValuesMemberName"/>) on the same object, returning an IEnumerable&lt;string&gt;.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class DropdownAttribute : PropertyAttribute
	{
		public string ValuesMemberName { get; }

		public DropdownAttribute(string valuesMemberName)
		{
			ValuesMemberName = valuesMemberName;
		}
	}
}
