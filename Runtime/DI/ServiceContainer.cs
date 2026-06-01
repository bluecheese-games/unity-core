//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlueCheese.Core.DI
{
	/// <summary>
	/// The core container responsible for managing service registrations, scopes, and resolution.
	/// </summary>
	public class ServiceContainer : IDisposable
	{
		private readonly ServiceContainer _parent;
		private readonly ConcurrentDictionary<Type, List<ServiceDescriptor>> _services = new();
		private readonly ConcurrentDictionary<(Type, string), ServiceDescriptor> _keyedServices = new();
		private readonly ConcurrentDictionary<Type, Action<object>> _optionsConfigurations = new();
		private readonly ConcurrentStack<IDisposable> _disposalStack = new();

		private List<Type> _resolutionStack;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceContainer"/> class.
		/// </summary>
		/// <param name="parent">An optional parent container for hierarchical resolution.</param>
		public ServiceContainer(ServiceContainer parent = null)
		{
			_parent = parent;
		}

		/// <summary>
		/// Configures options for a specific type using a delegate.
		/// </summary>
		public void Configure<TOptions>(Action<TOptions> configure) where TOptions : class, new()
		{
			_optionsConfigurations[typeof(TOptions)] = obj => configure((TOptions)obj);
		}

		#region Registration

		/// <summary> Registers a service type to be resolved as itself. </summary>
		public ServiceDescriptor Register<TService>() where TService : class => Register(typeof(TService)).UseType(typeof(TService));

		/// <summary> Registers a service type to be resolved as a specific implementation. </summary>
		public ServiceDescriptor Register<TService, TImplementation>() where TImplementation : class, TService => Register(typeof(TService)).UseType(typeof(TImplementation));

		/// <summary> Registers a service using a custom factory method. </summary>
		public ServiceDescriptor Register<TService>(Func<TService> factory) => Register(typeof(TService)).UseFactory(() => factory());

		/// <summary> Registers a service with a pre-existing instance. </summary>
		public ServiceDescriptor Register<TService>(TService instance) => Register(typeof(TService)).UseInstance(instance);

		/// <summary> Registers a service with a specific lookup key. </summary>
		public ServiceDescriptor Register<TService>(string key) where TService : class => RegisterKeyed(typeof(TService), typeof(TService), key);

		/// <summary> Registers a service implementation with a specific lookup key. </summary>
		public ServiceDescriptor Register<TService, TImplementation>(string key) where TImplementation : class, TService => RegisterKeyed(typeof(TService), typeof(TImplementation), key);

		public ServiceDescriptor Register(Type serviceType, Type implementationType)
		{
			var descriptor = Register(serviceType);
			if (serviceType.IsGenericTypeDefinition && implementationType.IsGenericTypeDefinition)
				descriptor.UseOpenGeneric(implementationType);
			else
				descriptor.UseType(implementationType);
			return descriptor;
		}

		private ServiceDescriptor Register(Type serviceType)
		{
			var descriptor = new ServiceDescriptor(this);
			AddAlias(serviceType, descriptor);
			return descriptor;
		}

		private ServiceDescriptor RegisterKeyed(Type serviceType, Type implementationType, string key)
		{
			var descriptor = new ServiceDescriptor(this).UseType(implementationType);
			_keyedServices[(serviceType, key)] = descriptor;
			return descriptor;
		}

		internal void AddAlias(Type aliasType, ServiceDescriptor descriptor)
		{
			var list = _services.GetOrAdd(aliasType, _ => new List<ServiceDescriptor>());
			lock (list) { list.Add(descriptor); }
		}

		/// <summary>
		/// Scans assemblies for <see cref="IServiceModule"/> implementations and loads them.
		/// </summary>
		public void RegisterModules(params Assembly[] assemblies)
		{
			if (assemblies == null) return;
			foreach (var assembly in assemblies)
			{
				var moduleTypes = assembly.GetTypes().Where(t => typeof(IServiceModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
				foreach (var moduleType in moduleTypes)
				{
					var module = (IServiceModule)Activator.CreateInstance(moduleType);
					module.Load(this);
				}
			}
		}

		#endregion

		#region Resolution

		/// <summary> Resolves the requested service type. </summary>
		public TService Resolve<TService>() => (TService)Resolve(typeof(TService));

		/// <summary> Resolves the requested service type using a specific key. </summary>
		public TService Resolve<TService>(string key) => (TService)Resolve(typeof(TService), key);

		public object Resolve(Type serviceType, string key = null)
		{
			// 1. Keyed Resolution
			if (!string.IsNullOrEmpty(key))
			{
				if (_keyedServices.TryGetValue((serviceType, key), out var keyed)) return keyed.GetInstance<object>();
				if (_parent != null) return _parent.Resolve(serviceType, key);
				throw new InvalidOperationException($"No service registered with key '{key}' for {serviceType}");
			}

			// 2. Direct Local Resolution
			if (_services.TryGetValue(serviceType, out var list))
			{
				_resolutionStack ??= new List<Type>();
				if (_resolutionStack.Contains(serviceType))
					throw new InvalidOperationException($"Circular dependency detected for {serviceType}.");

				_resolutionStack.Add(serviceType);
				try { lock (list) { return list.Last().GetInstance<object>(); } }
				finally { _resolutionStack.RemoveAt(_resolutionStack.Count - 1); }
			}

			// 3. Collection Resolution (Aggregates Parent + Local)
			if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				return ResolveAll(serviceType.GetGenericArguments()[0]);
			}

			// 4. Options Pattern
			if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IOptions<>))
			{
				return ResolveOptions(serviceType.GetGenericArguments()[0]);
			}

			// 5. Open Generics
			if (serviceType.IsGenericType && !serviceType.IsGenericTypeDefinition)
			{
				Type genericDef = serviceType.GetGenericTypeDefinition();
				if (_services.TryGetValue(genericDef, out var openList))
				{
					ServiceDescriptor template;
					lock (openList) { template = openList.Last(); }
					if (template.IsOpenGeneric)
					{
						Type closedImpl = template.OpenImplementationType.MakeGenericType(serviceType.GetGenericArguments());
						var closedDesc = Register(serviceType, closedImpl);
						template.ApplyMetadataTo(closedDesc);
						return closedDesc.GetInstance<object>();
					}
				}
			}

			// 6. Parent Scope Delegation
			if (_parent != null) return _parent.Resolve(serviceType);

			throw new InvalidOperationException($"Service of type {serviceType} is not registered.");
		}

		private object ResolveOptions(Type optionsType)
		{
			object instance = Activator.CreateInstance(optionsType);
			ApplyOptionsConfiguration(optionsType, instance);
			Type wrapperType = typeof(OptionsWrapper<>).MakeGenericType(optionsType);
			return Activator.CreateInstance(wrapperType, instance);
		}

		// Walks the container hierarchy root-first, applying each level's configuration delegate to the same instance.
		internal void ApplyOptionsConfiguration(Type optionsType, object instance)
		{
			_parent?.ApplyOptionsConfiguration(optionsType, instance);
			if (_optionsConfigurations.TryGetValue(optionsType, out var config)) config(instance);
		}

		internal object ResolveAll(Type itemType)
		{
			var parentInstances = _parent != null ? ((IEnumerable<object>)_parent.ResolveAll(itemType)) : Enumerable.Empty<object>();
			var localInstances = _services.TryGetValue(itemType, out var list) ? list.Select(d => d.GetInstance<object>()) : Enumerable.Empty<object>();

			var combined = parentInstances.Concat(localInstances).ToArray();
			var array = Array.CreateInstance(itemType, combined.Length);
			for (int i = 0; i < combined.Length; i++) array.SetValue(combined[i], i);
			return array;
		}

		#endregion

		#region Lifecycle & Validation

		/// <summary>
		/// Verifies that all registered services can have their dependencies satisfied.
		/// </summary>
		public void Validate()
		{
			var allDescriptors = _services.Values.SelectMany(x => x).Concat(_keyedServices.Values).Distinct();
			foreach (var descriptor in allDescriptors)
			{
				if (descriptor.IsOpenGeneric) continue;
				foreach (var param in descriptor.ConstructorParameters)
				{
					try { Resolve(param.ParameterType); }
					catch (Exception ex)
					{
						string sourceName = descriptor.ImplementationType?.Name ?? "Descriptor";
						throw new InvalidOperationException($"Validation failed: {sourceName} cannot resolve dependency {param.ParameterType}", ex);
					}
				}
			}
		}

		/// <summary>
		/// Triggers instantiation for eager services and calls <see cref="IInitializable.Initialize"/> on them.
		/// For lazy services, <see cref="IInitializable.Initialize"/> is called at first resolution, not here.
		/// </summary>
		public void Initialize()
		{
			foreach (var list in _services.Values)
				lock (list) { foreach (var d in list) { if (!d.IsOpenGeneric) d.Initialize(); } }

			foreach (var d in _keyedServices.Values) d.Initialize();
		}

		internal void CaptureDisposable(object instance)
		{
			if (instance is IDisposable d) _disposalStack.Push(d);
		}

		/// <summary>
		/// Disposes all created singleton services that implement <see cref="IDisposable"/>.
		/// Throws an <see cref="AggregateException"/> if one or more services fail to dispose.
		/// </summary>
		public void Dispose()
		{
			var exceptions = new List<Exception>();
			while (_disposalStack.TryPop(out var d))
			{
				try { d.Dispose(); }
				catch (Exception ex) { exceptions.Add(ex); }
			}
			_services.Clear();
			_keyedServices.Clear();
			if (exceptions.Count > 0) throw new AggregateException("One or more services failed to dispose.", exceptions);
		}

		#endregion
	}
}