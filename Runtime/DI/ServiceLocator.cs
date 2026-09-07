using System;

namespace BlueCheese.Core.DI
{
	/// <summary>
	/// Static entry point for service resolution when constructor injection is not possible.
	/// </summary>
	public static class ServiceLocator
	{
		private static ServiceContainer _container;

		/// <summary> Initializes the locator with the primary container. </summary>
		public static void Initialize(ServiceContainer container) { _container = container; _container.Initialize(); }

		/// <summary> Disposes the underlying container. </summary>
		public static void Dispose() { _container?.Dispose(); _container = null; }

		/// <summary> Resolves a service from the container. </summary>
		public static T Resolve<T>(string key = null) => (T)Resolve(typeof(T), key);

		public static object Resolve(Type t, string key = null)
		{
			if (_container == null) throw new InvalidOperationException("ServiceLocator not initialized.");
			return _container.Resolve(t, key);
		}
	}
}
