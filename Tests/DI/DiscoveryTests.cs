using BlueCheese.Core.DI;
using NUnit.Framework;
using System.Reflection;

namespace BlueCheese.Tests.DI
{

	[TestFixture]
	public class DiscoveryTests
	{
		public class MockModule : IServiceModule
		{
			public static bool Loaded = false;
			public void Load(ServiceContainer container)
			{
				Loaded = true;
				container.Register<IEngine, Engine>();
			}
		}

		[Test]
		public void RegisterModules_ScansAssembly_LoadsImplementations()
		{
			// Arrange
			var container = new ServiceContainer();
			MockModule.Loaded = false;

			// Act
			// Scanning the current assembly for MockModule
			container.RegisterModules(Assembly.GetExecutingAssembly());

			// Assert
			Assert.IsTrue(MockModule.Loaded, "The module should have been instantiated and loaded.");
			Assert.IsNotNull(container.Resolve<IEngine>(), "Services registered in the module should be resolvable.");
		}
	}

}
