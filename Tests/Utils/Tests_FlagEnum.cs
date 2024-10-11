//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core;
using NUnit.Framework;

public class Tests_FlagEnum
{
	private enum TestEnum
	{
		Value1,
		Value2,
		Value3
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
}
