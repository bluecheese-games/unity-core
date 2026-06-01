//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BlueCheese.Core.DI
{
	/// <summary>
	/// Metadata and instantiation logic for a registered service.
	/// </summary>
	public class ServiceDescriptor
	{
		private readonly ServiceContainer _container;
		private Func<object> _factory;
		private Func<object[], object> _compiledActivator;
		private ParameterInfo[] _cachedParams;
		private volatile object _instance;
		private ServiceLifetime _lifetime = ServiceLifetime.Singleton;
		private bool _isLazy = true;
		private bool _isInitialized;

		internal Type OpenImplementationType { get; private set; }
		public Type ImplementationType { get; private set; }
		public bool IsOpenGeneric => OpenImplementationType != null && OpenImplementationType.IsGenericTypeDefinition;
		public ParameterInfo[] ConstructorParameters => _cachedParams ?? Array.Empty<ParameterInfo>();

		public ServiceDescriptor(ServiceContainer container) => _container = container;

		internal ServiceDescriptor UseFactory(Func<object> factory)
		{
			_factory = factory;
			return this;
		}

		internal ServiceDescriptor UseType(Type type)
		{
			ImplementationType = type;
			var constructors = type.GetConstructors();
			if (constructors.Length == 0) throw new InvalidOperationException($"{type.Name} has no public constructors.");

			var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
			_cachedParams = constructor.GetParameters();

			// Compiled Expression for high-performance instantiation
			var argsParam = Expression.Parameter(typeof(object[]), "args");
			var argExpressions = _cachedParams.Select((p, i) => Expression.Convert(Expression.ArrayIndex(argsParam, Expression.Constant(i)), p.ParameterType));
			var newExpr = Expression.New(constructor, argExpressions);
			var lambda = Expression.Lambda<Func<object[], object>>(newExpr, argsParam);
			_compiledActivator = lambda.Compile();

			_factory = () =>
			{
				var args = _cachedParams.Select(p => _container.Resolve(p.ParameterType)).ToArray();
				return _compiledActivator(args);
			};
			return this;
		}

		internal ServiceDescriptor UseOpenGeneric(Type implementationType)
		{
			OpenImplementationType = implementationType;
			return this;
		}

		internal void ApplyMetadataTo(ServiceDescriptor other)
		{
			other._lifetime = _lifetime;
			other._isLazy = _isLazy;
		}

		internal ServiceDescriptor UseInstance(object instance)
		{
			_instance = instance;
			_container.CaptureDisposable(instance);
			return this;
		}

		/// <summary> Map the descriptor to an additional interface type. </summary>
		public ServiceDescriptor As<T>() { _container.AddAlias(typeof(T), this); return this; }
		/// <summary> Set lifetime to Singleton. </summary>
		public ServiceDescriptor AsSingleton() { _lifetime = ServiceLifetime.Singleton; return this; }
		/// <summary> Set lifetime to Transient. </summary>
		public ServiceDescriptor AsTransient() { _lifetime = ServiceLifetime.Transient; return this; }
		/// <summary> Service will only be created when requested. </summary>
		public ServiceDescriptor AsLazy() { _isLazy = true; return this; }
		/// <summary> Service will be created during container initialization. </summary>
		public ServiceDescriptor AsEager() { _isLazy = false; return this; }

		// Eager services are instantiated here if they have not been created yet.
		// Lazy services are only initialized here if a prior Resolve() already created the instance;
		// otherwise IInitializable.Initialize() is called at first resolution via ProcessNewInstance().
		internal void Initialize()
		{
			if (!_isLazy && _instance == null) GetInstance<object>();
			else if (!_isInitialized && _instance is IInitializable init)
			{
				lock (this) { if (!_isInitialized) { ServiceInjector.Inject(_instance); init.Initialize(); _isInitialized = true; } }
			}
		}

		public T GetInstance<T>()
		{
			if (_instance != null) return (T)_instance;
			if (_lifetime == ServiceLifetime.Transient) return (T)ProcessNewInstance(_factory(), true);

			lock (this)
			{
				if (_instance == null)
				{
					_instance = _factory();
					_container.CaptureDisposable(_instance);
					ProcessNewInstance(_instance, false);
				}
			}
			return (T)_instance;
		}

		private object ProcessNewInstance(object instance, bool isTransient)
		{
			ServiceInjector.Inject(instance);
			if (instance is IInitializable init && (isTransient || !_isInitialized))
			{
				init.Initialize();
				if (!isTransient) _isInitialized = true;
			}
			return instance;
		}
	}
}