using System;
using UnityEngine;

namespace BlueCheese.Core.Attributes
{
	public enum InfoBoxType
	{
		Info,
		Warning,
		Error
	}

	/// <summary>
	/// Draws a static help box above the field. Can be combined freely with any other attribute
	/// on the same field (it's a decorator, not a value drawer).
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public sealed class InfoBoxAttribute : PropertyAttribute
	{
		public string Text { get; }
		public InfoBoxType Type { get; }

		public InfoBoxAttribute(string text, InfoBoxType type = InfoBoxType.Info)
		{
			Text = text;
			Type = type;
		}
	}
}
