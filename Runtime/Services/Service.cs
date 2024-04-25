//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.Unity.Core.Services
{

    public partial class ServiceContainer
    {
        private sealed class Service : IService
        {
            private readonly ServiceContainer _container;
            private readonly Type _concreteType;
            private bool _isLazy;
            private Scope _scope;
            private readonly Dictionary<Type, object> _singletons;
            private Func<IOptions> _optionFunc;

            IOptions IService.Options => _optionFunc != null ? _optionFunc() : null;

            /// <summary>
            /// Create a service in the specified container using a concrete type
            /// </summary>
            internal Service(ServiceContainer container, Type concreteType)
            {
                _container = container;
                _concreteType = concreteType;
                _isLazy = false;
                _scope = Scope.Singleton;
                _singletons = new Dictionary<Type, object>();
            }

            /// <summary>
            /// Create a service in the specified container using an instance
            /// </summary>
            internal Service(ServiceContainer container, object instance)
            {
                _container = container;
                _concreteType = instance.GetType();
                _singletons = new Dictionary<Type, object>
                {
                    [_concreteType] = instance
                };
                _isLazy = false;
                _scope = Scope.Singleton;
            }

            /// <summary>
            /// Declare this service as lazy, it will only be instantiated when required
            /// </summary>
            public IService AsLazy()
            {
                if (_singletons.ContainsKey(_concreteType))
                {
                    throw new InvalidOperationException("Services registered using instance should be non-lazy.");
                }
                _isLazy = true;
                return this;
            }

            /// <summary>
            /// Declare this service as non-lazy, it will be instantiated during startup
            /// </summary>
            public IService AsNonLazy()
            {
                _isLazy = false;
                return this;
            }

            /// <summary>
            /// Declare this service as singleton, only one instance will be created
            /// </summary>
            public void AsSingleton()
            {
                _scope = Scope.Singleton;
            }

            /// <summary>
            /// Declare this service as transient, a new instance will be created each time it is resolved
            /// </summary>
            public void AsTransient()
            {
                if (_singletons.ContainsKey(_concreteType))
                {
                    throw new InvalidOperationException("Services registered using instance should be singletons.");
                }
                _scope = Scope.Transient;
            }

            /// <summary>
            /// Inject an existing instance of the service to be used.
            /// This service can only be a non lazy singleton.
            /// </summary>
            public void WithInstance(object instance)
            {
                if (instance == null)
                {
                    throw new ArgumentNullException(nameof(instance));
                }

                if (!_concreteType.IsAssignableFrom(instance.GetType()))
                {
                    throw new Exception($"Provided instance should be of type {_concreteType} (was {instance.GetType()})");
                }

                _scope = Scope.Singleton;
                _singletons[_concreteType] = instance;
                _isLazy = false;
            }

            public IService UseOptions(Func<IOptions> optionsFunc)
            {
                _optionFunc = optionsFunc;
                return this;
            }

            void IService.Startup()
            {
                if (_isLazy) return;
                if (_scope != Scope.Singleton) return;
                if (_singletons.ContainsKey(_concreteType)) return;
                if (_concreteType.IsGenericType) return;

                // Force singleton instantiation
                ((IService)this).GetInstance();
            }

            object IService.GetInstance(Type genericParameterType)
            {
                if (_scope == Scope.Singleton)
                {
                    Type keyType = genericParameterType ?? _concreteType;
                    if (_singletons.ContainsKey(keyType))
                    {
                        return _singletons[keyType];
                    }
                    _singletons.Add(keyType, CreateNewInstance(genericParameterType));
                    return _singletons[keyType];
                }
                else
                {
                    return CreateNewInstance(genericParameterType);
                }
            }

            private object CreateNewInstance(Type genericParameterType = null)
            {
                Type instanceType = _concreteType;
                if (genericParameterType != null)
                {
                    instanceType = instanceType.MakeGenericType(genericParameterType);
                }
                try
                {
                    object instance = Activator.CreateInstance(instanceType, _container.ResolveParameters(instanceType, this))!;
                    if (instance is IInitializable initializable)
                    {
                        initializable.Initialize();
                    }
                    return instance;
                }
                catch (Exception e)
                {
                    throw new Exception($"Unable to instanciate service: {instanceType}", e);
                }
            }

            void IService.Shutdown()
            {
                foreach (var instance in _singletons.Values)
                {
                    if (instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _singletons.Clear();
            }

            private enum Scope
            {
                Singleton,
                Transient
            }
        }
    }
}
