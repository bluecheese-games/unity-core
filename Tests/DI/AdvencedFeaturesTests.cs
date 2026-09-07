using BlueCheese.Core.DI;
using NUnit.Framework;
using System;

namespace BlueCheese.Tests.DI
{
	// --- Test Dummies ---

	public interface IGlobalService { }
	public class GlobalService : IGlobalService { }

	public interface ILocalService { }
	public class LocalService : ILocalService { }

	public class DependentService
	{
		public IGlobalService Global { get; }
		public DependentService(IGlobalService global) => Global = global;
	}

	public class MissingDepService
	{
		// Requires a service that we won't register
		public MissingDepService(IDisposable nonExistent) { }
	}

	[TestFixture]
	public class AdvancedFeatureTests
	{
		private ServiceContainer _globalContainer;

		[SetUp]
		public void Setup()
		{
			_globalContainer = new ServiceContainer();
		}

		#region Validation

		[Test]
		public void Validate_CorrectSetup_DoesNotThrow()
		{
			// Arrange
			_globalContainer.Register<IGlobalService, GlobalService>();
			_globalContainer.Register<DependentService>();

			// Act & Assert
			Assert.DoesNotThrow(() => _globalContainer.Validate());
		}

		[Test]
		public void Validate_MissingDependency_ThrowsHelpfulException()
		{
			// Arrange
			// We register the service, but NOT its dependency (IDisposable)
			_globalContainer.Register<MissingDepService>();

			// Act & Assert
			var ex = Assert.Throws<InvalidOperationException>(() => _globalContainer.Validate());
			Assert.That(ex.Message, Does.Contain("MissingDepService cannot resolve dependency"));
		}

		#endregion

		#region Compiled Expressions (Turbo)

		[Test]
		public void Turbo_CompiledExpression_InstantiatesCorrectly()
		{
			// Arrange
			_globalContainer.Register<IGlobalService, GlobalService>().AsSingleton();
			_globalContainer.Register<DependentService>().AsTransient();

			// Act
			// This triggers the compiled lambda created in UseType()
			var instance = _globalContainer.Resolve<DependentService>();

			// Assert
			Assert.IsNotNull(instance);
			Assert.IsNotNull(instance.Global);
			Assert.IsInstanceOf<GlobalService>(instance.Global);
		}

		#endregion
	}
}
