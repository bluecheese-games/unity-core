//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Core.ServiceLocator
{
	public static class Services
	{
		/// <summary>
		/// Register a service using concrete type.
		/// </summary>
		/// <typeparam name="TConcreteService">The concrete Type of the service, it will also be used as a key to store and resolve the service.</typeparam>
		public static IService Register<TConcreteService>() where TConcreteService : class
			=> ServiceContainer.Default.Register<TConcreteService>();

		/// <summary>
		/// Register a service using abstract type.
		/// </summary>
		/// <typeparam name="TAbstractService">The abstact Type of the service, it will be used as a key to store and resolve the service.</typeparam>
		/// <typeparam name="TConcreteService">The concrete Type of the service.</typeparam>
		public static IService Register<TAbstractService, TConcreteService>() where TConcreteService : class, TAbstractService
			=> ServiceContainer.Default.Register<TAbstractService, TConcreteService>();

		/// <summary>
		/// Register a service using abstract type.
		/// </summary>
		/// <param name="abstractType">The abstact Type of the service, it will be used as a key to store and resolve the service.</param>
		/// <param name="concreteType">The concrete Type of the service.</param>
		public static IService Register(Type abstractType, Type concreteType)
			=> ServiceContainer.Default.Register(abstractType, concreteType);

		/// <summary>
		/// Register a service using instance.
		/// </summary>
		/// <typeparam name="TAbstractService">The abstact Type of the service, it will be used as a key to store and resolve the service.</typeparam>
		public static IService Register<TAbstractService>(TAbstractService instance)
			=> ServiceContainer.Default.Register(instance);

		/// <summary>
		/// Register a decorator for a service.
		/// </summary>
		/// <typeparam name="TService">The service type to be decorated.</typeparam>
		/// <typeparam name="TDecorator">The decorator type.</typeparam>
		public static IService RegisterDecorator<TService, TDecorator>()
			where TService : class
			where TDecorator : class, TService 
			=> ServiceContainer.Default.RegisterDecorator<TService, TDecorator>();

		/// <summary>
		/// Call it when all services have been registered.
		/// Singleton services marked as non-lazy will be instantiated immediately.
		/// </summary>
		public static void Startup() 
			=> ServiceContainer.Default.Startup();

		/// <summary>
		/// Call it before the app is closed, to dispose all IDisposable services
		/// </summary>
		public static void Shutdown() 
			=> ServiceContainer.Default.Shutdown();

		/// <summary>
		/// Resolve and return a service that was registered in this container.
		/// </summary>
		/// <typeparam name="TService">
		/// The Type of the service to resolve.
		/// Use the exact Type used to register the service.
		/// </typeparam>
		/// <returns>A service instance</returns>
		public static TService Get<TService>() 
			=> ServiceContainer.Default.Get<TService>();

		/// <summary>
		/// Instantiate and inject an object of the specified type
		/// </summary>
		public static T Instantiate<T>() 
			=> ServiceContainer.Default.Instantiate<T>();

		/// <summary>
		/// Instantiate and inject an object of the specified type
		/// </summary>
		public static object Instantiate(Type type) 
			=> ServiceContainer.Default.Instantiate(type);

		/// <summary>
		/// Inject services in all fields with [Injectable] attribute in the instance
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <param name="instance">The instance that contains the injectable fields</param>
		/// <param name="includeBaseClasses">Should we inject base classes as well</param>
		/// <returns>The instance</returns>
		public static TService Inject<TService>(TService instance, bool includeBaseClasses = false)
			=> ServiceContainer.Default.Inject(instance, includeBaseClasses);
	}

}
