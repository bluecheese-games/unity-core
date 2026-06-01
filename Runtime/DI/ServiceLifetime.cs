//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.DI
{
	/// <summary>
	/// Specifies the lifetime of a registered service.
	/// </summary>
	public enum ServiceLifetime
	{
		/// <summary> A single instance created once and shared. </summary>
		Singleton,
		/// <summary> A new instance created every time it is requested. </summary>
		Transient
	}
}