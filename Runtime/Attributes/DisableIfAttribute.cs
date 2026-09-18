using System;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Keeps the field visible but grays it out (non-editable) when the condition member equals
	/// the expected value. The single-argument constructor is a shortcut for a bool condition
	/// member (disabled when true).
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class DisableIfAttribute : ConditionAttribute
	{
		public DisableIfAttribute(string conditionMemberName, object expectedValue) : base(conditionMemberName, expectedValue) { }
		public DisableIfAttribute(string conditionMemberName) : base(conditionMemberName, true) { }
	}
}
