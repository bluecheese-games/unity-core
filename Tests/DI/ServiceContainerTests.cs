//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueCheese.Tests.DI
{
	// --- Test Dummies ---

	public interface IEngine { }
	public class Engine : IEngine { }

	public class Car
	{
		public IEngine Engine { get; }
		public Car(IEngine engine) => Engine = engine;
	}

	public class CircularA { public CircularA(CircularB b) { } }
	public class CircularB { public CircularB(CircularA a) { } }

	public class DisposableService : IDisposable
	{
		private readonly string _name;
		private readonly List<string> _log;
		public DisposableService(string name, List<string> log)
		{
			_name = name;
			_log = log;
		}
		public void Dispose() => _log.Add(_name);
	}

	public class DatabaseService : DisposableService
	{
		public DatabaseService(List<string> log) : base("Database", log) { }
	}

	public class PlayerService : DisposableService
	{
		public PlayerService(List<string> log) : base("PlayerService", log) { }
	}

	public class LazyChecker
	{
		public static bool WasConstructed = false;
		public LazyChecker() => WasConstructed = true;
	}

	public interface ILogger<T> { }

	public class UnityLogger<T> : ILogger<T> { }

	public class QuestService
	{
		public ILogger<QuestService> Logger { get; }
		public QuestService(ILogger<QuestService> logger) => Logger = logger;
	}

	public class AchievementService
	{
		public ILogger<AchievementService> Logger { get; }
		public AchievementService(ILogger<AchievementService> logger) => Logger = logger;
	}

	public class FaultyDisposable : IDisposable
	{
		public void Dispose() => throw new InvalidOperationException("Dispose failed.");
	}

	public class FaultyDisposable2 : IDisposable
	{
		public void Dispose() => throw new InvalidOperationException("Second dispose failed.");
	}

	// --- Unit Tests ---

	[TestFixture]
	public class ServiceContainerTests
	{
		private ServiceContainer _container;

		[SetUp]
		public void Setup()
		{
			_container = new ServiceContainer();
			LazyChecker.WasConstructed = false;
		}

		[Test]
		public void RegisterAndResolve_SimpleType_ReturnsInstance()
		{
			// Arrange
			_container.Register<Engine>();

			// Act
			var engine = _container.Resolve<Engine>();

			// Assert
			Assert.IsNotNull(engine);
			Assert.IsInstanceOf<Engine>(engine);
		}

		[Test]
		public void Resolve_ConstructorInjection_InjectsDependencies()
		{
			// Arrange
			_container.Register<IEngine, Engine>();
			_container.Register<Car>();

			// Act
			var car = _container.Resolve<Car>();

			// Assert
			Assert.IsNotNull(car.Engine);
			Assert.IsInstanceOf<Engine>(car.Engine);
		}

		[Test]
		public void Lifetime_Singleton_ReturnsSameInstance()
		{
			// Arrange
			_container.Register<Engine>().AsSingleton();

			// Act
			var first = _container.Resolve<Engine>();
			var second = _container.Resolve<Engine>();

			// Assert
			Assert.AreSame(first, second);
		}

		[Test]
		public void Lifetime_Transient_ReturnsDifferentInstances()
		{
			// Arrange
			_container.Register<Engine>().AsTransient();

			// Act
			var first = _container.Resolve<Engine>();
			var second = _container.Resolve<Engine>();

			// Assert
			Assert.AreNotSame(first, second);
		}

		[Test]
		public void Initialize_EagerService_InstantiatesImmediately()
		{
			// Arrange
			_container.Register<LazyChecker>().AsEager();

			// Act
			_container.Initialize();

			// Assert
			Assert.IsTrue(LazyChecker.WasConstructed);
		}

		[Test]
		public void Resolve_CircularDependency_ThrowsException()
		{
			// Arrange
			_container.Register<CircularA>();
			_container.Register<CircularB>();

			// Act & Assert
			var ex = Assert.Throws<InvalidOperationException>(() => _container.Resolve<CircularA>());
			Assert.That(ex.Message, Does.Contain("Circular dependency"));
		}

		[Test]
		public void Dispose_DisposalOrder_FollowsLIFOPattern()
		{
			// Arrange
			var disposalLog = new List<string>();

			// 1. Register distinct types as singletons
			_container.Register<DatabaseService>(() => new DatabaseService(disposalLog));
			_container.Register<PlayerService>(() => new PlayerService(disposalLog));

			// 2. Resolve them in order: Database first, Player second
			// This order ensures Database is at the bottom of the stack
			_container.Resolve<DatabaseService>();
			_container.Resolve<PlayerService>();

			// Act
			_container.Dispose();

			// Assert
			// LIFO: The last one resolved (PlayerService) must be the first one disposed
			Assert.AreEqual(2, disposalLog.Count, "Both services should have been disposed.");
			Assert.AreEqual("PlayerService", disposalLog[0], "High-level service should dispose first.");
			Assert.AreEqual("Database", disposalLog[1], "Low-level dependency should dispose last.");
		}

		[Test]
		public void ServiceLocator_InitializeAndResolve_ReturnsService()
		{
			// Arrange
			_container.Register<Engine>();
			BlueCheese.Core.DI.ServiceLocator.Initialize(_container);

			// Act
			var engine = BlueCheese.Core.DI.ServiceLocator.Resolve<Engine>();

			// Assert
			Assert.IsNotNull(engine);

			// Cleanup
			BlueCheese.Core.DI.ServiceLocator.Dispose();
		}

		[Test]
		public void ServiceLocator_ResolveUninitialized_ThrowsException()
		{
			// Arrange
			// Force reset the static container to null for testing
			typeof(BlueCheese.Core.DI.ServiceLocator).GetField("_container",
				System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
				.SetValue(null, null);

			// Act & Assert
			Assert.Throws<InvalidOperationException>(() => BlueCheese.Core.DI.ServiceLocator.Resolve<Engine>());
		}

		[Test]
		public void Resolve_MultipleServices_ReturnsCollection()
		{
			// Arrange
			_container.Register<IEngine, Engine>();
			_container.Register<IEngine, Engine>();

			// Act
			var engines = _container.Resolve<IEnumerable<IEngine>>();

			// Assert
			Assert.AreEqual(2, engines.Count());
		}

		[Test]
		public void Resolve_OpenGeneric_InjectsCorrectClosedType()
		{
			// Arrange
			_container.Register(typeof(ILogger<>), typeof(UnityLogger<>))
					  .AsSingleton();

			_container.Register<AchievementService>();
			_container.Register<QuestService>();

			// Act
			var achievementService = _container.Resolve<AchievementService>();
			var questService = _container.Resolve<QuestService>();

			// Assert
			Assert.IsInstanceOf<UnityLogger<AchievementService>>(achievementService.Logger);
			Assert.IsInstanceOf<UnityLogger<QuestService>>(questService.Logger);

			// Verify singleton behavior on the dynamically created closed type
			var logger2 = _container.Resolve<ILogger<AchievementService>>();
			Assert.AreSame(achievementService.Logger, logger2);
		}

		[Test]
		public void Resolve_TwoIndependentContainers_DoNotShareResolutionState()
		{
			// Arrange
			var containerA = new ServiceContainer();
			containerA.Register<Engine>();

			var containerB = new ServiceContainer();
			containerB.Register<Engine>();

			// Act
			var engineA = containerA.Resolve<Engine>();
			var engineB = containerB.Resolve<Engine>();

			// Assert
			Assert.IsNotNull(engineA);
			Assert.IsNotNull(engineB);
			Assert.AreNotSame(engineA, engineB);
		}

		[Test]
		public void Resolve_ChildDelegatingToParent_DoesNotTriggerCircularDetection()
		{
			// Arrange
			var parent = new ServiceContainer();
			parent.Register<IEngine, Engine>().AsSingleton();

			var child = new ServiceContainer(parent);

			// Act
			var resolved = child.Resolve<IEngine>();

			// Assert
			Assert.IsNotNull(resolved);
			Assert.IsInstanceOf<Engine>(resolved);
		}

		[Test]
		public void Dispose_FaultyService_ThrowsAggregateException()
		{
			// Arrange
			var container = new ServiceContainer();
			container.Register<FaultyDisposable>(new FaultyDisposable());
			container.Resolve<FaultyDisposable>();

			// Act & Assert
			var ex = Assert.Throws<AggregateException>(() => container.Dispose());
			Assert.AreEqual(1, ex.InnerExceptions.Count);
		}

		[Test]
		public void Dispose_MultipleFaultyServices_AllExceptionsCollected()
		{
			// Arrange
			var container = new ServiceContainer();
			container.Register<FaultyDisposable>(new FaultyDisposable());
			container.Register<FaultyDisposable2>(new FaultyDisposable2());
			container.Resolve<FaultyDisposable>();
			container.Resolve<FaultyDisposable2>();

			// Act & Assert
			var ex = Assert.Throws<AggregateException>(() => container.Dispose());
			Assert.AreEqual(2, ex.InnerExceptions.Count);
		}
	}
}