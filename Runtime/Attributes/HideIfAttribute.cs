using System;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Hides the field in the inspector when the condition member equals the expected value.
	/// The single-argument constructor is a shortcut for a bool condition member (hides when true).
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class HideIfAttribute : ConditionAttribute
	{
		public HideIfAttribute(string conditionMemberName, object expectedValue) : base(conditionMemberName, expectedValue) { }
		public HideIfAttribute(string conditionMemberName) : base(conditionMemberName, true) { }
	}
}
