using System;

namespace BlueCheese.Core.DI
{
	/// <summary>
	/// Mark a field or property to receive dependency injection on existing instances.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class InjectableAttribute : Attribute
	{
		public string Key { get; }
		public InjectableAttribute(string key = null) => Key = key;
	}
}
