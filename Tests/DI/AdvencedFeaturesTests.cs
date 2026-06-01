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

	public class DisposableTracker : IDisposable
	{
		public bool IsDisposed { get; private set; }
		public void Dispose() => IsDisposed = true;
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

		#region Scoped Containers (Hierarchy)

		[Test]
		public void Scope_ChildCanResolveFromParent()
		{
			// Arrange
			_globalContainer.Register<IGlobalService, GlobalService>().AsSingleton();
			var sceneContainer = new ServiceContainer(_globalContainer);

			// Act
			var resolved = sceneContainer.Resolve<IGlobalService>();

			// Assert
			Assert.IsNotNull(resolved);
			Assert.IsInstanceOf<GlobalService>(resolved);
		}

		[Test]
		public void Scope_ChildOverridesParent()
		{
			// Arrange
			_globalContainer.Register<IGlobalService, GlobalService>().AsSingleton();

			var sceneContainer = new ServiceContainer(_globalContainer);
			// Local override of the same interface
			var localOverride = new GlobalService();
			sceneContainer.Register<IGlobalService>(localOverride);

			// Act
			var globalRes = _globalContainer.Resolve<IGlobalService>();
			var sceneRes = sceneContainer.Resolve<IGlobalService>();

			// Assert
			Assert.AreNotSame(globalRes, sceneRes, "Child should return its own registration.");
			Assert.AreSame(localOverride, sceneRes);
		}

		[Test]
		public void Scope_ResolveAll_CombinesParentAndChild()
		{
			// Arrange
			_globalContainer.Register<IGlobalService, GlobalService>();
			var sceneContainer = new ServiceContainer(_globalContainer);
			sceneContainer.Register<IGlobalService, GlobalService>();

			// Act
			var all = sceneContainer.Resolve<IEnumerable<IGlobalService>>();

			// Assert
			Assert.AreEqual(2, all.Count(), "Should find one from parent and one from child.");
		}

		[Test]
		public void Scope_DisposeChild_DoesNotDisposeParent()
		{
			// Arrange
			var globalDisc = new DisposableTracker();
			var sceneDisc = new DisposableTracker();

			_globalContainer.Register<DisposableTracker>(globalDisc);
			var sceneContainer = new ServiceContainer(_globalContainer);
			sceneContainer.Register<DisposableTracker>(sceneDisc);

			// Force instantiation
			sceneContainer.Resolve<DisposableTracker>();
			_globalContainer.Resolve<DisposableTracker>();

			// Act
			sceneContainer.Dispose();

			// Assert
			Assert.IsTrue(sceneDisc.IsDisposed, "Child service should be disposed.");
			Assert.IsFalse(globalDisc.IsDisposed, "Parent service should remain active.");
		}

		#endregion

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