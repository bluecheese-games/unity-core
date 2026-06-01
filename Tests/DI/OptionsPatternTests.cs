//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using NUnit.Framework;
using System;

namespace BlueCheese.Tests.DI
{
	// --- Test Dummies ---

	public class AudioOptions
	{
		public float Volume { get; set; } = 1.0f;
		public bool UseSpatial { get; set; } = false;
	}

	public class AudioService
	{
		public AudioOptions Settings { get; }
		// Service depends on IOptions<T>
		public AudioService(IOptions<AudioOptions> options)
		{
			Settings = options.Value;
		}
	}

	[TestFixture]
	public class OptionsPatternTests
	{
		private ServiceContainer _container;

		[SetUp]
		public void Setup()
		{
			_container = new ServiceContainer();
		}

		[Test]
		public void Resolve_OptionsWithNoConfig_ReturnsDefaultValues()
		{
			// Arrange
			_container.Register<AudioService>();

			// Act
			var service = _container.Resolve<AudioService>();

			// Assert
			Assert.IsNotNull(service.Settings);
			Assert.AreEqual(1.0f, service.Settings.Volume, "Should use the default value defined in the class.");
		}

		[Test]
		public void Resolve_OptionsWithConfig_AppliesConfiguration()
		{
			// Arrange
			_container.Register<AudioService>();
			_container.Configure<AudioOptions>(opt =>
			{
				opt.Volume = 0.5f;
				opt.UseSpatial = true;
			});

			// Act
			var service = _container.Resolve<AudioService>();

			// Assert
			Assert.AreEqual(0.5f, service.Settings.Volume);
			Assert.IsTrue(service.Settings.UseSpatial);
		}

		[Test]
		public void Resolve_IOptionsDirectly_ReturnsWrapper()
		{
			// Arrange
			_container.Configure<AudioOptions>(opt => opt.Volume = 0.2f);

			// Act
			var options = _container.Resolve<IOptions<AudioOptions>>();

			// Assert
			Assert.IsNotNull(options);
			Assert.AreEqual(0.2f, options.Value.Volume);
		}

		[Test]
		public void Resolve_MultipleServicesWithSameOptions_ShareConfigurationLogic()
		{
			// Arrange
			_container.Register<AudioService>();
			_container.Configure<AudioOptions>(opt => opt.Volume = 0.1f);

			// Act
			var service1 = _container.Resolve<AudioService>();
			var service2 = _container.Resolve<IOptions<AudioOptions>>();

			// Assert
			Assert.AreEqual(0.1f, service1.Settings.Volume);
			Assert.AreEqual(0.1f, service2.Value.Volume);
		}

		[Test]
		public void Resolve_ChildContainerWithNoConfig_InheritsParentConfig()
		{
			// Arrange
			var parent = new ServiceContainer();
			parent.Configure<AudioOptions>(opt => opt.Volume = 0.3f);

			var child = new ServiceContainer(parent);

			// Act
			var options = child.Resolve<IOptions<AudioOptions>>();

			// Assert
			Assert.AreEqual(0.3f, options.Value.Volume);
		}

		[Test]
		public void Resolve_ChildContainerOverridesParentConfig_BothAppliedAdditively()
		{
			// Arrange
			var parent = new ServiceContainer();
			parent.Configure<AudioOptions>(opt =>
			{
				opt.Volume = 0.8f;
				opt.UseSpatial = true;
			});

			var child = new ServiceContainer(parent);
			child.Configure<AudioOptions>(opt => opt.Volume = 0.4f);

			// Act
			var options = child.Resolve<IOptions<AudioOptions>>();

			// Assert
			Assert.AreEqual(0.4f, options.Value.Volume);
			Assert.IsTrue(options.Value.UseSpatial);
		}

		[Test]
		public void Resolve_ThreeLevelContainerChain_AllConfigsAppliedInOrder()
		{
			// Arrange
			var grandparent = new ServiceContainer();
			grandparent.Configure<AudioOptions>(opt => opt.Volume = 1.0f);

			var parent = new ServiceContainer(grandparent);
			parent.Configure<AudioOptions>(opt => opt.UseSpatial = true);

			var child = new ServiceContainer(parent);
			child.Configure<AudioOptions>(opt => opt.Volume = 0.5f);

			// Act
			var options = child.Resolve<IOptions<AudioOptions>>();

			// Assert
			Assert.AreEqual(0.5f, options.Value.Volume);
			Assert.IsTrue(options.Value.UseSpatial);
		}
	}
}