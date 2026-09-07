namespace BlueCheese.Core.DI
{
	/// <summary>
	/// Wrapper for the Options pattern providing the underlying instance.
	/// </summary>
	public class OptionsWrapper<TOptions> : IOptions<TOptions> where TOptions : class, new()
	{
		public TOptions Value { get; }
		public OptionsWrapper(TOptions value) => Value = value;
	}
}
