namespace BlueCheese.Core.DI
{
	/// <summary>
	/// Defines a lifecycle hook for services that require manual setup after instantiation.
	/// </summary>
	public interface IInitializable
	{
		/// <summary>
		/// Called once when the service is created or when the container is initialized.
		/// </summary>
		void Initialize();
	}
}
