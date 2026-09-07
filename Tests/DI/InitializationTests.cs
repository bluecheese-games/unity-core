using BlueCheese.Core.DI;
using NUnit.Framework;
using System;

namespace BlueCheese.Tests.DI
{
	// --- Test Dummies ---

	public class InitializableService : IInitializable
	{
		public int InitCount { get; private set; }

		public void Initialize()
		{
			InitCount++;
		}
	}

	public class EagerInitializableService : InitializableService { }
	public class LazyInitializableService : InitializableService { }

	[TestFixture]
	public class InitializationTests
	{
		private ServiceContainer _container;

		[SetUp]
		public void Setup()
		{
			_container = new ServiceContainer();
		}

		[Test]
		public void Initialize_EagerService_CallsInitializeOnContainerStartup()
		{
			// Arrange
			_container.Register<EagerInitializableService>().AsEager();

			// Act
			_container.Initialize();
			var service = _container.Resolve<EagerInitializableService>();

			// Assert
			Assert.AreEqual(1, service.InitCount, "Initialize should be called exactly once during container startup.");
		}

		[Test]
		public void Resolve_LazyService_CallsInitializeOnFirstResolution()
		{
			// Arrange
			_container.Register<LazyInitializableService>().AsLazy();

			// Act
			var service = _container.Resolve<LazyInitializableService>();

			// Assert
			Assert.AreEqual(1, service.InitCount, "Initialize should be called when a lazy service is first resolved.");
		}

		[Test]
		public void Initialize_ServiceResolvedBeforeStartup_DoesNotCallInitializeTwice()
		{
			// Arrange
			// We register an eager service but resolve it MANUALLY before the container starts
			_container.Register<EagerInitializableService>().AsEager();

			// Act
			var service = _container.Resolve<EagerInitializableService>(); // First call (lazy-style resolution)
			_container.Initialize(); // Second potential call (startup-style resolution)

			// Assert
			Assert.AreEqual(1, service.InitCount, "Initialize should not be called again if the service was already initialized via resolution.");
		}

		[Test]
		public void Resolve_ServiceResolvedAfterStartup_DoesNotCallInitializeTwice()
		{
			// Arrange
			_container.Register<EagerInitializableService>().AsEager();

			// Act
			_container.Initialize(); // First call
			var service = _container.Resolve<EagerInitializableService>(); // Second potential call

			// Assert
			Assert.AreEqual(1, service.InitCount, "Initialize should not be called again when resolving an already eager-loaded service.");
		}

		[Test]
		public void Resolve_TransientService_CallsInitializeEveryTime()
		{
			// Arrange
			_container.Register<InitializableService>().AsTransient();

			// Act
			var instance1 = _container.Resolve<InitializableService>();
			var instance2 = _container.Resolve<InitializableService>();

			// Assert
			Assert.AreEqual(1, instance1.InitCount);
			Assert.AreEqual(1, instance2.InitCount);
			Assert.AreNotSame(instance1, instance2);
		}
	}
}
