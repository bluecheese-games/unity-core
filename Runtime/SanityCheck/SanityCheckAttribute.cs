//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Core.SanityCheck
{
	/// <summary>
	/// Marks a static class as a sanity check discoverable by the Sanity Checks editor window.
	/// The class must expose one of:
	/// - <c>public static SanityCheckResult Run()</c>
	/// - <c>public static UniTask&lt;SanityCheckResult&gt; RunAsync(CancellationToken token)</c> (token optional)
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class SanityCheckAttribute : Attribute
	{
		/// <summary>
		/// Display name shown in the Sanity Checks window. Defaults to the class name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Category used to group checks in the Sanity Checks window. Defaults to "General".
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// Optional run order. Lower values run first. Defaults to 0.
		/// </summary>
		public int Priority { get; set; }

		public SanityCheckAttribute() { }

		public SanityCheckAttribute(string name)
		{
			Name = name;
		}
	}
}
