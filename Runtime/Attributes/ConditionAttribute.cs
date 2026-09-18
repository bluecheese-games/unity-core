using UnityEngine;

namespace BlueCheese.Core.Attributes
{
	/// <summary>
	/// Base for attributes that toggle a field's visibility/interactivity based on another
	/// field/property (named <see cref="ConditionMemberName"/>) on the same object equaling
	/// <see cref="ExpectedValue"/>. Only supports top-level members of the serialized object
	/// (not members nested inside a struct/class field).
	/// </summary>
	public abstract class ConditionAttribute : PropertyAttribute
	{
		public string ConditionMemberName { get; }
		public object ExpectedValue { get; }

		protected ConditionAttribute(string conditionMemberName, object expectedValue)
		{
			ConditionMemberName = conditionMemberName;
			ExpectedValue = expectedValue;
		}
	}
}
