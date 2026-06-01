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
	}
}