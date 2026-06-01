//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using NUnit.Framework;
using System;

namespace BlueCheese.Tests.DI
{
	// --- Test Dummies ---

	public interface IDummyService { }
	public class DummyService : IDummyService { }

	public class InjectionBase
	{
		// Private field in base class
		[Injectable] private IDummyService _baseField;

		public IDummyService GetBaseField() => _baseField;
	}

	public class InjectionDerived : InjectionBase
	{
		// Private property in derived class
		[Injectable] private IDummyService DerivedProperty { get; set; }

		public IDummyService GetDerivedProperty() => DerivedProperty;
	}

	public class NoInjectionClass
	{
		public IDummyService ManualField;
	}

	// --- Unit Tests ---

	[TestFixture]
	public class ServiceInjectorTests
	{
		private ServiceContainer _container;

		[SetUp]
		public void Setup()
		{
			_container = new ServiceContainer();
			_container.Register<IDummyService, DummyService>().AsSingleton();

			// ServiceInjector relies on ServiceLocator being initialized
			BlueCheese.Core.DI.ServiceLocator.Initialize(_container);
		}

		[TearDown]
		public void Teardown()
		{
			BlueCheese.Core.DI.ServiceLocator.Dispose();
		}

		[Test]
		public void Inject_FieldsAndProperties_PopulatesMembers()
		{
			// Arrange
			var instance = new InjectionDerived();

			// Act
			ServiceInjector.Inject(instance);

			// Assert
			Assert.IsNotNull(instance.GetDerivedProperty(), "Property in derived class should be injected.");
			Assert.IsInstanceOf<DummyService>(instance.GetDerivedProperty());
		}

		[Test]
		public void Inject_InheritanceHierarchy_PopulatesBaseMembers()
		{
			// Arrange
			var instance = new InjectionDerived();

			// Act
			ServiceInjector.Inject(instance);

			// Assert
			Assert.IsNotNull(instance.GetBaseField(), "Field in base class should be injected.");
			Assert.IsInstanceOf<DummyService>(instance.GetBaseField());
		}

		[Test]
		public void Inject_PrivateMembers_AccessesSuccessfully()
		{
			// Arrange
			var instance = new InjectionDerived();

			// Act
			ServiceInjector.Inject(instance);

			// Assert
			// Checking both base (private field) and derived (private property)
			Assert.IsNotNull(instance.GetBaseField());
			Assert.IsNotNull(instance.GetDerivedProperty());
		}

		[Test]
		public void Inject_NoAttributes_DoesNotChangeState()
		{
			// Arrange
			var instance = new NoInjectionClass();

			// Act
			ServiceInjector.Inject(instance);

			// Assert
			Assert.IsNull(instance.ManualField, "Fields without [Injectable] should not be touched.");
		}

		[Test]
		public void Inject_NullInstance_DoesNotThrow()
		{
			// Act & Assert
			Assert.DoesNotThrow(() => ServiceInjector.Inject(null));
		}

		[Test]
		public void Inject_MultipleCalls_MaintainsSameReference()
		{
			// Arrange
			var instance = new InjectionDerived();

			// Act
			ServiceInjector.Inject(instance);
			var firstReference = instance.GetDerivedProperty();

			ServiceInjector.Inject(instance);
			var secondReference = instance.GetDerivedProperty();

			// Assert
			Assert.AreSame(firstReference, secondReference, "Subsequent injections should resolve to the same singleton.");
		}
	}
}