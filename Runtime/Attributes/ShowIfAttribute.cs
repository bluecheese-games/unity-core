using System;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Shows the field in the inspector only when the condition member equals the expected value.
	/// The single-argument constructor is a shortcut for a bool condition member (shows when true).
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ShowIfAttribute : ConditionAttribute
	{
		public ShowIfAttribute(string conditionMemberName, object expectedValue) : base(conditionMemberName, expectedValue) { }
		public ShowIfAttribute(string conditionMemberName) : base(conditionMemberName, true) { }
	}
}
