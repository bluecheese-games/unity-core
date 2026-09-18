using System;
using UnityEngine;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Draws a Vector2 field as a min/max range slider clamped to [<see cref="MinLimit"/>, <see cref="MaxLimit"/>].
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class MinMaxSliderAttribute : PropertyAttribute
	{
		public float MinLimit { get; }
		public float MaxLimit { get; }

		public MinMaxSliderAttribute(float minLimit, float maxLimit)
		{
			MinLimit = minLimit;
			MaxLimit = maxLimit;
		}
	}
}
