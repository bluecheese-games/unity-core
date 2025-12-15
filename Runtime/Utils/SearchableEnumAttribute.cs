using UnityEngine;
using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class SearchableEnumAttribute : PropertyAttribute
{
	/// <summary>
	/// Optional: name of the inner enum field when the serialized field is a wrapper.
	/// Example: [SearchableEnum(InnerFieldName = "value")]
	/// </summary>
	public string InnerFieldName { get; set; }

	/// <summary>
	/// Optional: sort enum entries alphabetically by name in the popup.
	/// Example: [SearchableEnum(SortByName = true)]
	/// </summary>
	public bool SortByName { get; set; }

	// Parameterless ctor so everything is configured via named properties
	public SearchableEnumAttribute() { }
}
