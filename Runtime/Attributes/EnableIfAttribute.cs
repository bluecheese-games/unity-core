using System;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Keeps the field visible but grays it out (non-editable) unless the condition member equals
	/// the expected value. The single-argument constructor is a shortcut for a bool condition
	/// member (enabled when true).
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class EnableIfAttribute : ConditionAttribute
	{
		public EnableIfAttribute(string conditionMemberName, object expectedValue) : base(conditionMemberName, expectedValue) { }
		public EnableIfAttribute(string conditionMemberName) : base(conditionMemberName, true) { }
	}
}
