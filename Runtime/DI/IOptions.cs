//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.DI
{
	/// <summary>
	/// Provides access to a configured options instance of type <typeparamref name="TOptions"/>.
	/// </summary>
	public interface IOptions<out TOptions> where TOptions : class, new()
	{
		TOptions Value { get; }
	}
}