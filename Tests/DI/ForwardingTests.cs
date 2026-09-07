using BlueCheese.Core.DI;
using NUnit.Framework;

namespace BlueCheese.Tests.DI
{
	[TestFixture]
	public class ForwardingTests
	{
		public interface IServiceA { }
		public interface IServiceB { }
		public class DualService : IServiceA, IServiceB { }

		[Test]
		public void Resolve_MultipleInterfaces_ReturnsSameSingletonInstance()
		{
			// Arrange
			var container = new ServiceContainer();

			// Register under multiple types using the .As<T> fluent method
			container.Register<DualService>()
					 .AsSingleton()
					 .As<IServiceA>()
					 .As<IServiceB>();

			// Act
			var instanceA = container.Resolve<IServiceA>();
			var instanceB = container.Resolve<IServiceB>();
			var concrete = container.Resolve<DualService>();

			// Assert
			Assert.AreSame(instanceA, instanceB, "IServiceA and IServiceB should point to the same instance.");
			Assert.AreSame(instanceA, concrete, "The concrete type resolution should also match.");
		}
	}
}
