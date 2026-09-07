namespace BlueCheese.Core.DI
{
	/// <summary>
	/// Defines a module for grouping and discovering service registrations.
	/// </summary>
	public interface IServiceModule
	{
		/// <summary>
		/// Registers services into the provided container.
		/// </summary>
		void Load(ServiceContainer container);
	}
}
