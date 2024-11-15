//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using NUnit.Framework;

public class Tests_FlagEnum
{
	private enum TestEnum
	{
		Value0,
		Value1,
		Value2,
		Value3
	}

	private enum TestLargeEnum
	{
		Value0,
		Value1,
		Value2,
		Value3,
		Value4 = 4,
		Value36 = 36,
		Value68 = 68,
	}

	[Test]
	public void HasFlag_ReturnsTrueWhenFlagIsSet()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2); // Value1 and Value2 are set

		// Act
		var result = flagTestEnum.HasFlag(TestEnum.Value1);

		// Assert
		Assert.IsTrue(result);
	}

	[Test]
	public void HasFlag_ReturnsFalseWhenFlagIsNotSet()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1); // Only Value1 is set

		// Act
		var result = flagTestEnum.HasFlag(TestEnum.Value2);

		// Assert
		Assert.IsFalse(result);
	}

	[Test]
	public void SetFlag_OverIntLimit()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestLargeEnum>(TestLargeEnum.Value36);

		// Act
		var hasFlag4 = flagTestEnum.HasFlag(TestLargeEnum.Value4);
		var hasFlag36 = flagTestEnum.HasFlag(TestLargeEnum.Value36);

		// Assert
		Assert.IsFalse(hasFlag4);
		Assert.IsTrue(hasFlag36);
	}

	[Test]
	public void SetFlag_OverLongLimit()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestLargeEnum>(TestLargeEnum.Value68);

		// Act
		var hasFlag4 = flagTestEnum.HasFlag(TestLargeEnum.Value4);
		var hasFlag36 = flagTestEnum.HasFlag(TestLargeEnum.Value36);
		var hasFlag68 = flagTestEnum.HasFlag(TestLargeEnum.Value68);

		// Assert
		Assert.IsTrue(hasFlag4);   // the flag 4 is also true
		Assert.IsFalse(hasFlag36);
		Assert.IsTrue(hasFlag68);
	}

	[Test]
	public void AddFlag_SetsTheFlag()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1); // Only Value1 is set

		// Act
		flagTestEnum.AddFlag(TestEnum.Value2);

		// Assert
		Assert.IsTrue(flagTestEnum.HasFlag(TestEnum.Value2));
	}

	[Test]
	public void RemoveFlag_ClearsTheFlag()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2); // Value1 and Value2 are set

		// Act
		flagTestEnum.RemoveFlag(TestEnum.Value1);

		// Assert
		Assert.IsFalse(flagTestEnum.HasFlag(TestEnum.Value1));
	}

	[Test]
	public void ToString_ReturnsCommaSeparatedValues()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2); // Value1 and Value2 are set

		// Act
		var result = flagTestEnum.ToString();

		// Assert
		Assert.AreEqual("Value1, Value2", result);
	}

	[Test]
	public void ToString_ReturnsCachedValue()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2); // Value1 and Value2 are set

		// Act
		var result1 = flagTestEnum.ToString();
		flagTestEnum.AddFlag(TestEnum.Value3);
		var result2 = flagTestEnum.ToString();

		// Assert
		Assert.AreEqual("Value1, Value2", result1);
		Assert.AreEqual("Value1, Value2, Value3", result2);
	}

	[Test]
	public void ImplicitConversion_SetsTheFlag()
	{
		// Arrange
		FlagEnum<TestEnum> flagTestEnum = TestEnum.Value1;

		// Act
		var result = flagTestEnum.HasFlag(TestEnum.Value1);

		// Assert
		Assert.IsTrue(result);
	}

	[Test]
	public void ToBinaryString_ReturnsBinaryString()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2); // Value1 and Value2 are set

		// Act
		var result = flagTestEnum.ToBinaryString();
		var resultWithLeadingZeros = flagTestEnum.ToBinaryString(true);

		// Assert
		Assert.AreEqual("110", result);
		Assert.AreEqual("0000000000000000000000000000000000000000000000000000000000000110", resultWithLeadingZeros);
	}
}
