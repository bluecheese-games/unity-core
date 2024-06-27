//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BlueCheese.Core.ServiceLocator
{
    /// <summary>
    /// The ServiceContainer class main purpose is to provide a simple way to register and resolve services.
    /// Main features are:
    /// - Register service by concrete type, abstract type or concrete implementation
    /// - Lazy instantiation: services are instantiated only when required
    /// - Service dependencies: services can depends on other services
    /// - Dynamic injection: services are injected in constructor at runtime
    /// - Singleton/transient: services can be registered in both modes
    /// - Generic type support: e.g. ILogger<MyClass>
    /// - Service container: services are isolated in containers, you can have multiple containers
    /// - Service decorator: services can decorate other services
    /// - Service options: you can set options for services
    /// </summary>
    /// 
    public partial class ServiceContainer
    {
        private static readonly ServiceContainer _defaultContainer = new();

        private readonly ConcurrentDictionary<Type, Service> _services = new();
        private readonly ConcurrentDictionary<Type, Service> _decoratedServices = new();

        /// <summary>
        /// Reference to the default services container.
        /// </summary>
        public static ServiceContainer Default => _defaultContainer;

        /// <summary>
        /// Reset the registered services list
        /// </summary>
        public void Reset() => _services.Clear();

        /// <summary>
        /// Register a service using concrete type.
        /// </summary>
        /// <typeparam name="TConcreteService">The concrete Type of the service, it will also be used as a key to store and resolve the service.</typeparam>
        public IService Register<TConcreteService>() where TConcreteService : class
        {
            Type concreteType = typeof(TConcreteService);
            if (_services.ContainsKey(concreteType))
            {
                throw new Exception($"A service of type {concreteType} has already been registered");
            }

            return Register(concreteType, concreteType);
        }

        /// <summary>
        /// Register a service using abstract type.
        /// </summary>
        /// <typeparam name="TAbstractService">The abstact Type of the service, it will be used as a key to store and resolve the service.</typeparam>
        /// <typeparam name="TConcreteService">The concrete Type of the service.</typeparam>
        public IService Register<TAbstractService, TConcreteService>() where TConcreteService : class, TAbstractService
            => Register(typeof(TAbstractService), typeof(TConcreteService));

        /// <summary>
        /// Register a service using abstract type.
        /// </summary>
        /// <param name="abstractType">The abstact Type of the service, it will be used as a key to store and resolve the service.</param>
        /// <param name="concreteType">The concrete Type of the service.</param>
        public IService Register(Type abstractType, Type concreteType)
        {
            var service = GetServiceWithConcreteType(concreteType) ?? new Service(this, concreteType);
            _services[abstractType] = service;
            return service;
        }

        /// <summary>
        /// Register a service using instance.
        /// </summary>
        /// <typeparam name="TAbstractService">The abstact Type of the service, it will be used as a key to store and resolve the service.</typeparam>
        public IService Register<TAbstractService>(TAbstractService instance)
        {
            Type abstractType = typeof(TAbstractService);
            if (_services.ContainsKey(abstractType))
            {
                throw new Exception($"A service of type {abstractType} has already been registered");
            }

            var service = GetServiceWithConcreteType(instance.GetType());
            if (service != null)
            {
                service.WithInstance(instance);
            }
            else
            {
                service = new Service(this, instance);
            }
            _services[abstractType] = service;
            return service;
        }

        /// <summary>
        /// Register a decorator for a service.
        /// </summary>
        /// <typeparam name="TService">The service type to be decorated.</typeparam>
        /// <typeparam name="TDecorator">The decorator type.</typeparam>
        public IService RegisterDecorator<TService, TDecorator>()
            where TService : class
            where TDecorator : class, TService
        {
            Type abstractType = typeof(TService);
            if (!_services.ContainsKey(abstractType))
            {
                throw new Exception($"Cannot register decorator for {abstractType} because the service is not registered.");
            }

            Service decoratedService = _services[abstractType];
            var decoratorService = new Service(this, typeof(TDecorator));
            _services[abstractType] = decoratorService;
            _decoratedServices[abstractType] = decoratedService;
            return decoratorService;
        }

        /// <summary>
        /// Call it when all services has been registered.
        /// Singleton services marked as non-lazy will be instantiated immediatly.
        /// </summary>
        public void Startup()
        {
            foreach (var service in _decoratedServices.Values)
            {
                service.Startup();
            }
            foreach (var service in _services.Values)
            {
                service.Startup();
            }
        }

        /// <summary>
        /// Call it before the app is closed, to dispose all IDisposable services
        /// </summary>
        public void Shutdown()
        {
            foreach (var service in _services.Values)
            {
                service.Shutdown();
            }
        }

        /// <summary>
        /// Resolve and return a service that was registered in this container.
        /// </summary>
        /// <typeparam name="TService">
        /// The Type of the service to resolve.
        /// Use the exact Type used to register the service.
        /// </typeparam>
        /// <returns>A service instance</returns>
        public TService Get<TService>()
        {
            return (TService)ResolveService(typeof(TService));
        }

        private object ResolveService(Type abstractType, Type constructedService = null, Service service = null)
        {
            if (typeof(IOptions).IsAssignableFrom(abstractType))
            {
                return service.Options ?? Activator.CreateInstance(abstractType);
            }

            var services = _services;
            if (constructedService != null && abstractType.IsAssignableFrom(constructedService))
            {
                if (_decoratedServices.ContainsKey(abstractType))
                {
                    services = _decoratedServices;
                }
                else
                {
                    throw new Exception($"Constructed service cannot use itself in constructor");
                }
            }

            Type keyType = abstractType;
            Type genericParameterType = null;
            if (abstractType.IsGenericType)
            {
                keyType = abstractType.GetGenericTypeDefinition();
                genericParameterType = abstractType.GetGenericArguments()[0];
            }

            if (services.ContainsKey(keyType) == false)
            {
                throw new Exception($"Service not found: {keyType}");
            }
            return services[keyType].GetInstance(genericParameterType);
        }

        private object[] ResolveParameters(Type concreteType, Service service)
        {
            ConstructorInfo constructor = concreteType.GetConstructors().SingleOrDefault();
            if (constructor == null)
            {
                return default;
            }

            return constructor
                .GetParameters()
                .Select(p => ResolveService(p.ParameterType, concreteType, service))
                .ToArray();
        }

        /// <summary>
        /// Inject services in all fields with [Injectable] attribute in the instance
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="instance">The instance that contains the injectable fields</param>
        /// <param name="includeBaseClasses">Should we inject base classes as well</param>
        /// <returns>The instance</returns>
        public TService Inject<TService>(TService instance, bool includeBaseClasses = false)
        {
            Type type = typeof(TService);
            while (type != null && type != typeof(MonoBehaviour))
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<InjectableAttribute>(true) == null)
                    {
                        continue;
                    }

                    field.SetValue(instance, ResolveService(field.FieldType));
                }

                if (!includeBaseClasses)
                {
                    break;
                }

                type = type.BaseType;
            }
            return instance;
        }

        private Service GetServiceWithConcreteType(Type concreteType)
        {
            return _services.Values.FirstOrDefault(s => s.HasConcreteType(concreteType));
        }
    }

    public class InjectableAttribute : Attribute { }
}
