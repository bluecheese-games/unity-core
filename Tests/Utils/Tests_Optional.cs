using BlueCheese.Core.Utils;
using NUnit.Framework;

public class Tests_Optional
{
	#region Resolve

	[Test]
	public void Resolve_Disabled_ReturnsFallback()
	{
		// Arrange
		var optional = new Optional<int>(5, enabled: false);

		// Act
		var result = optional.Resolve(42);

		// Assert
		Assert.AreEqual(42, result);
	}

	[Test]
	public void Resolve_Enabled_ReturnsValue()
	{
		// Arrange
		var optional = new Optional<int>(5, enabled: true);

		// Act
		var result = optional.Resolve(42);

		// Assert
		Assert.AreEqual(5, result);
	}

	[Test]
	public void Resolve_DefaultStruct_IsDisabled()
	{
		// Arrange
		var optional = new Optional<int>();

		// Act
		var result = optional.Resolve(42);

		// Assert
		Assert.AreEqual(42, result);
	}

	#endregion

	#region ImplicitConversion

	[Test]
	public void ImplicitConversion_FromValue_IsEnabledWithThatValue()
	{
		// Arrange
		Optional<string> optional = "hello";

		// Act
		var result = optional.Resolve("fallback");

		// Assert
		Assert.IsTrue(optional.Enabled);
		Assert.AreEqual("hello", result);
	}

	#endregion
}
