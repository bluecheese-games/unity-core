//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using NUnit.Framework;
using System;

namespace BlueCheese.Tests.ServiceLocator
{
    public class Tests_ServiceContainer
    {
        readonly ServiceContainer _container = new();

        [SetUp]
        public void Setup()
        {
            _container.Reset();
            CountableService.Count = 0;
        }

        [Test]
        public void Test_RegisterService_ByType()
        {
            _container.Register<FooService>();
            _container.Startup();

            Assert.That(_container.Get<FooService>(), Is.Not.Null);

            // the service has not been registered by interface
            // so we can't get it by interface
            Assert.Throws(typeof(Exception), () =>
            {
                _container.Get<IFooService>();
            });
        }

        [Test]
        public void Test_RegisterService_ByConcreteType_WithInstance()
        {
            _container.Register<FooService>().WithInstance(new FooService());
			_container.Startup();

			Assert.That(_container.Get<FooService>(), Is.Not.Null);

            // We can't get it by abstract type
            Assert.Throws(typeof(Exception), () =>
            {
                _container.Get<IFooService>();
            });
        }

        [Test]
        public void Test_RegisterService_ByAbstractType_WithInstance()
        {
            _container.Register<IFooService>().WithInstance(new FooService());
			_container.Startup();

			Assert.That(_container.Get<IFooService>(), Is.Not.Null);

            // We can't get it by concrete type
            Assert.Throws(typeof(Exception), () =>
            {
                _container.Get<FooService>();
            });
        }

        [Test]
        public void Test_RegisterService_WithInstance_Null()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                _container.Register<FooService>().WithInstance(null);
            });
        }

        [Test]
        public void Test_RegisterService_WithInstance_WrongType()
        {
            Assert.Throws(typeof(Exception), () =>
            {
                _container.Register<FooService>().WithInstance(new BarService());
            });
        }

        [Test]
        public void Test_RegisterService_AsLazy()
        {
            _container.Register<CountableService>().AsLazy();

            Assert.That(CountableService.Count, Is.EqualTo(0));

            // Lazy services are not affected by startup
            _container.Startup();

            Assert.That(CountableService.Count, Is.EqualTo(0));

            // Should instantiate the lazy service
            _container.Get<CountableService>();

            Assert.That(CountableService.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_RegisterService_AsNonLazy()
        {
            _container.Register<CountableService>().AsNonLazy();

            Assert.That(CountableService.Count, Is.EqualTo(0));

            // Non lazy services are instantiated during startup
            _container.Startup();

            Assert.That(CountableService.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_RegisterService_ByAbstractType()
        {
            _container.Register<IFooService, FooService>();
			_container.Startup();

			Assert.That(_container.Get<IFooService>(), Is.Not.Null);

            // the service has been registered by interface
            // so we can't get it by type
            Assert.Throws(typeof(Exception), () =>
            {
                _container.Get<FooService>();
            });
        }

        [Test]
        public void Test_RegisterService_Twice()
        {
            _container.Register<FooService>();

            // we can't register the same service twice
            Assert.Throws(typeof(Exception), () =>
            {
                _container.Register<FooService>();
            });
        }

        [Test]
        public void Test_RegisterServices_ByType_WithSameAbstractType()
        {
            _container.Register<FooService>();
            _container.Register<BarService>();
			_container.Startup();

			// Register 2 services with the same interface is allowed
			// as long as they are not accessed by interface
			Assert.That(_container.Get<FooService>(), Is.Not.Null);
            Assert.That(_container.Get<BarService>(), Is.Not.Null);

            // the service has been registered by type
            // so we can't get it by interface
            Assert.Throws(typeof(Exception), () =>
            {
                _container.Get<IFooService>();
            });
        }

        [Test]
        public void Test_RegisterServices_Mixed()
        {
            _container.Register<FooService>();
            _container.Register<IBarService, BarService>();
			_container.Startup();

			var service1 = _container.Get<FooService>();
            var service2 = _container.Get<IBarService>();

            Assert.That(service1.Print(), Is.EqualTo("foo"));
            Assert.That(service2.Print(), Is.EqualTo("bar"));
        }

        [Test]
        public void Test_RegisterServices_WithDependencies()
        {
            _container.Register<FooService>();
            _container.Register<IBarService, BarService>();
            _container.Register<FooBarService>();
			_container.Startup();

			var service = _container.Get<FooBarService>();
            Assert.That(service.Print(), Is.EqualTo("foo bar"));
        }

        [Test]
        public void Test_RegisterServices_WithMissingDependencies()
        {
            _container.Register<FooBarService>();

			// FooBarService depends on FooService and BarService
			// They haven't been registered, so it should fail
			Assert.Throws(typeof(Exception), () =>
			{
				_container.Startup();
			});
        }

        [Test]
        public void Test_RegisterServices_AsSingleton()
        {
            _container.Register<FooService>().AsSingleton();
			_container.Startup();

			var service1 = _container.Get<FooService>();
            var service2 = _container.Get<FooService>();
            Assert.That(service1, Is.SameAs(service2));
        }

        [Test]
        public void Test_RegisterServices_AsTransient()
        {
            _container.Register<FooService>().AsTransient();
			_container.Startup();

			var service1 = _container.Get<FooService>();
            var service2 = _container.Get<FooService>();
            Assert.That(service1, Is.Not.SameAs(service2));
        }

        [Test]
        public void Test_RegisterServices_Instance()
        {
            FooService service = new FooService();

            _container.Register(service);
			_container.Startup();

			var service1 = _container.Get<FooService>();
            var service2 = _container.Get<FooService>();

            // Ensure that this is a singleton
            Assert.That(service1, Is.SameAs(service2));
        }

        [Test]
        public void Test_RegisterServices_GenericType_AsSingleton()
        {
            _container.Register(typeof(IGenericService<>), typeof(GenericService<>)).AsSingleton();
			_container.Startup();

			var service1 = _container.Get<IGenericService<string>>();
            var service2 = _container.Get<IGenericService<string>>();
            var service3 = _container.Get<IGenericService<int>>();

            // Ensure that this is a singleton per generic type
            Assert.That(service1, Is.SameAs(service2));
            Assert.That(service2, Is.Not.SameAs(service3));
            Assert.That(service1, Is.Not.SameAs(service3));
        }

        [Test]
        public void Test_RegisterServices_GenericType_AsTransient()
        {
            _container.Register(typeof(IGenericService<>), typeof(GenericService<>)).AsTransient();
			_container.Startup();

			var service1 = _container.Get<IGenericService<string>>();
            var service2 = _container.Get<IGenericService<string>>();

            // Ensure that this is a singleton per generic type
            Assert.That(service1, Is.Not.SameAs(service2));
        }

        [Test]
        public void Test_RegisterServices_Decorator()
        {
            _container.Register<IFooService, FooService>();
            _container.RegisterDecorator<IFooService, DecoratorFooService>();
			_container.Startup();

			var service = _container.Get<IFooService>();

            // Ensure that the decorator service is returned
            Assert.That(service, Is.TypeOf<DecoratorFooService>());

            // Ensure that the decorator service is using the decorated service
            Assert.That(service.Print(), Is.EqualTo("decorated foo"));
        }

        [Test]
        public void Test_RegisterSubContainer()
        {
            // Arrange
            ServiceContainer subContainer = new();
            subContainer.Register<FooService>();

            // Act
            _container.RegisterSubContainer(subContainer);
			_container.Startup();

			// Assert
			Assert.That(_container.Get<FooService>(), Is.Not.Null);
        }

        [Test]
        public void Test_RegisterSubContainer_WithSameService()
		{
			// Arrange
			var service1 = new CountableService();
			var service2 = new CountableService();

			_container.Register(service1);
			ServiceContainer subContainer = new();
			subContainer.Register(service2);

			// Act
			_container.RegisterSubContainer(subContainer);
			_container.Startup();

			// Assert
			var service = _container.Get<CountableService>();
			Assert.That(service, Is.Not.Null);
			Assert.That(service, Is.SameAs(service1));
		}

        [Test]
        public void Test_RegisterSubContainer_Null()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                _container.RegisterSubContainer(null);
            });
        }

        [Test]
        public void Test_RegisterSubContainer_WithItself()
		{
			Assert.Throws(typeof(ArgumentException), () =>
			{
				_container.RegisterSubContainer(_container);
			});
		}

		[Test]
		public void Test_RegisterSubContainer_Twice()
		{
			// Arrange
			ServiceContainer subContainer = new();
			subContainer.Register<FooService>();
			_container.RegisterSubContainer(subContainer);

			// Act / Assert
			Assert.Throws(typeof(ArgumentException), () =>
			{
				_container.RegisterSubContainer(subContainer);
			});
		}

		[Test]
        public void Test_InjectServices()
        {
            _container.Register<IFooService, FooService>();
            _container.Register<IBarService, BarService>();
			_container.Startup();

			var testedObject = new InjectableObject();
            Assert.That(testedObject.Foo, Is.Null);
            Assert.That(testedObject.Bar, Is.Null);

            _container.Inject(testedObject);

            Assert.That(testedObject.Foo, Is.Not.Null);
            Assert.That(testedObject.Bar, Is.Not.Null);
        }

        [Test]
        public void Test_InjectServices_InChildClassOnly()
        {
            _container.Register<IChildService, ChildService>();
            _container.Register<IBaseService, BaseService>();
			_container.Startup();
			var child = new InjectableChildObject();

            _container.Inject(child, false);

            Assert.That(child.ChildService, Is.Not.Null);
            Assert.That(child.BaseService, Is.Null);
        }

        [Test]
        public void Test_InjectServices_InChildClassAndBaseClass()
        {
            _container.Register<IChildService, ChildService>();
            _container.Register<IBaseService, BaseService>();
			_container.Startup();
			var child = new InjectableChildObject();

            _container.Inject(child, true);

            Assert.That(child.ChildService, Is.Not.Null);
            Assert.That(child.BaseService, Is.Not.Null);
        }

        [Test]
        public void Test_InjectServices_InBaseClass()
        {
            _container.Register<IChildService, ChildService>();
            _container.Register<IBaseService, BaseService>();
			_container.Startup();
			var child = new InjectableChildObject();
            var parent = (InjectableBaseObject)child;

            _container.Inject(parent);

            Assert.That(child.ChildService, Is.Null);
            Assert.That(child.BaseService, Is.Not.Null);
        }

        [Test]
        public void Test_RegisterService_WithOptions()
        {
            _container.Register<OptionnableService>()
                .WithOptions(() => new OptionnableService.Options { Foo = "Bar" });
			_container.Startup();

			var service = _container.Get<OptionnableService>();

            Assert.That(service.Print(), Is.EqualTo("Bar"));
        }

        [Test]
        public void Test_RegisterService_WithOptions_Default()
        {
            _container.Register<OptionnableService>();
			_container.Startup();

			var service = _container.Get<OptionnableService>();

            Assert.That(service.Print(), Is.Null);
        }

        [Test]
        public void Test_RegisterService_AsTransient_WithOptions()
        {
            _container.Register<OptionnableService>()
                .WithOptions(() => new OptionnableService.Options { Foo = "Bar" })
                .AsTransient();
			_container.Startup();

			var service1 = _container.Get<OptionnableService>();
            var service2 = _container.Get<OptionnableService>();

            Assert.That(service1.Print(), Is.EqualTo("Bar"));
            Assert.That(service2.Print(), Is.EqualTo("Bar"));
        }

        [Test]
        public void Test_RegisterService_WithMultipleInterfaces()
        {
            _container.Register<IFooService, CountableService>();
            _container.Register<IBarService, CountableService>();
			_container.Startup();

			var fooService = _container.Get<IFooService>();
            var barService = _container.Get<IBarService>();

            Assert.That(CountableService.Count, Is.EqualTo(1));
            Assert.That(fooService.Print(), Is.EqualTo(barService.Print()));
        }

        [Test]
        public void Test_RegisterService_WithInterfaceThenConcreteType()
        {
            _container.Register<IFooService, CountableService>();
            _container.Register<CountableService>();
			_container.Startup();

			var fooService = _container.Get<IFooService>();
            var countableService = _container.Get<CountableService>();

            Assert.That(CountableService.Count, Is.EqualTo(1));
            Assert.That(fooService.Print(), Is.EqualTo(countableService.Print()));
        }

        [Test]
        public void Test_RegisterService_WithConcreteTypeThenInterface()
        {
            _container.Register<CountableService>();
            _container.Register<IFooService, CountableService>();
			_container.Startup();

			var fooService = _container.Get<IFooService>();
            var countableService = _container.Get<CountableService>();

            Assert.That(CountableService.Count, Is.EqualTo(1));
            Assert.That(fooService.Print(), Is.EqualTo(countableService.Print()));
        }

        [Test]
        public void Test_RegisterService_WithInstanceThenInterface()
        {
            CountableService service = new();
            _container.Register(service);
            _container.Register<IFooService, CountableService>();
			_container.Startup();

			var fooService = _container.Get<IFooService>();
            var countableService = _container.Get<CountableService>();

            Assert.That(CountableService.Count, Is.EqualTo(1));
            Assert.That(fooService.Print(), Is.EqualTo(countableService.Print()));
        }

        [Test]
        public void Test_RegisterService_WithInterfaceThenInstance()
        {
            CountableService service = new();
            _container.Register<IFooService, CountableService>();
            _container.Register(service);
			_container.Startup();

			var fooService = _container.Get<IFooService>();
            var countableService = _container.Get<CountableService>();

            Assert.That(CountableService.Count, Is.EqualTo(1));
            Assert.That(fooService.Print(), Is.EqualTo(countableService.Print()));
		}

		[Test]
		public void Test_Instantiate()
		{
			_container.Register<FooService>();
			_container.Register<IBarService, BarService>();
			_container.Startup();

			var service = _container.Instantiate<FooBarService>();
			Assert.That(service.Print(), Is.EqualTo("foo bar"));
		}

		public interface IFooService
        {
            string Print();
        }

        public interface IBarService
        {
            string Print();
        }

        public interface IChildService { }

        public class ChildService : IChildService { }

        public interface IBaseService { }

        public class BaseService : IBaseService { }

        public interface IGenericService<T>
        {
            string Print();
        }

        public class FooService : IFooService
        {
            public string Print() => "foo";
        }

        public class DecoratorFooService : IFooService
        {
            public IFooService _decoratedFooService;

            public DecoratorFooService(IFooService decoratedFooService)
            {
                _decoratedFooService = decoratedFooService;
            }

            public string Print() => "decorated " + _decoratedFooService.Print();
        }

        public class BarService : IBarService
        {
            public string Print() => "bar";
        }

        public class GenericService<T> : IGenericService<T>
        {
            public string Print() => "generic " + typeof(T);
        }

        public class FooBarService
        {
            private readonly FooService _fooService;
            private readonly IBarService _barService;

            public FooBarService(FooService fooService, IBarService barService)
            {
                _fooService = fooService;
                _barService = barService;
            }

            public string Print() => $"{_fooService.Print()} {_barService.Print()}";
        }

        public class CountableService : IFooService, IBarService
        {
            public static int Count = 0;

            public int InstanceId { get; private set; }

            public CountableService()
            {
                Count++;
                InstanceId = Count;
            }

            public string Print() => $"CountableService #{InstanceId}";
        }

        public class InjectableObject
        {
            [Injectable] private readonly IFooService _fooService;
            [Injectable] private readonly IBarService _barService;

            public IFooService Foo => _fooService;
            public IBarService Bar => _barService;
        }

        public class InjectableBaseObject
        {
            [Injectable] protected readonly IBaseService _baseService;

            public IBaseService BaseService => _baseService;
        }

        public class InjectableChildObject : InjectableBaseObject
        {
            [Injectable] private readonly IChildService _childService;

            public IChildService ChildService => _childService;
        }

        public class OptionnableService
        {
            public struct Options : IOptions
            {
                public string Foo;
            }

            private readonly Options _options;

            public OptionnableService(Options options)
            {
                _options = options;
            }

            public string Print() => _options.Foo;
        }
    }
}