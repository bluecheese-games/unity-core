//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;

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
		Value4,
		Value5,
		Value6,
		Value7,
		Value8,
		Value9,
		Value10,
		Value11,
		Value12,
		Value13,
		Value14,
		Value15,
		Value16,
		Value17,
		Value18,
		Value19,
		Value20,
		Value21,
		Value22,
		Value23,
		Value24,
		Value25,
		Value26,
		Value27,
		Value28,
		Value29,
		Value30,
		Value31,
		Value32,
		Value33,
		Value34,
		Value35,
		Value36,
		Value37,
		Value38,
		Value39,
		Value40,
		Value41,
		Value42,
		Value43,
		Value44,
		Value45,
		Value46,
		Value47,
		Value48,
		Value49,
		Value50,
		Value51,
		Value52,
		Value53,
		Value54,
		Value55,
		Value56,
		Value57,
		Value58,
		Value59,
		Value60,
		Value61,
		Value62,
		Value63,
		Value64,
		Value65,
		Value66,
		Value67,
		Value68,
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
		Assert.That(flagTestEnum.Count, Is.EqualTo(1));
	}

	[Test]
	public void OverLongLimit_ShouldThrowException()
	{
		// Arrange, Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			var flagTestEnum = new FlagEnum<TestLargeEnum>(TestLargeEnum.Value68);
		});
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

	[Test]
	public void EqualityOperators_WorkCorrectly()
	{
		// Arrange
		var flagTestEnum1 = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2);
		var flagTestEnum2 = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2);
		var flagTestEnum3 = new FlagEnum<TestEnum>(TestEnum.Value1);

		// Act & Assert
		Assert.IsTrue(flagTestEnum1 == flagTestEnum2);
		Assert.IsFalse(flagTestEnum1 != flagTestEnum2);
		Assert.IsFalse(flagTestEnum1 == flagTestEnum3);
		Assert.IsTrue(flagTestEnum1 != flagTestEnum3);
		Assert.IsTrue(flagTestEnum1.Equals(flagTestEnum2));
		Assert.IsFalse(flagTestEnum1.Equals(flagTestEnum3));
	}

	[Test]
	public void FlagEnum_BitwiseOperators_WorkCorrectly()
	{
		// Arrange
		var flagTestEnum1 = new FlagEnum<TestEnum>(TestEnum.Value1);
		var flagTestEnum2 = new FlagEnum<TestEnum>(TestEnum.Value2);

		// Act
		var combined = flagTestEnum1 | flagTestEnum2;
		var intersection = flagTestEnum1 & flagTestEnum2;
		var exclusiveOr = flagTestEnum1 ^ flagTestEnum2;
		var negation = ~flagTestEnum1;
		var mixed = flagTestEnum1 | TestEnum.Value2;

		// Assert
		Assert.IsTrue(combined.HasFlag(TestEnum.Value1));
		Assert.IsTrue(combined.HasFlag(TestEnum.Value2));
		Assert.IsFalse(intersection.HasFlag(TestEnum.Value1));
		Assert.IsFalse(intersection.HasFlag(TestEnum.Value2));
		Assert.IsTrue(exclusiveOr.HasFlag(TestEnum.Value1));
		Assert.IsTrue(exclusiveOr.HasFlag(TestEnum.Value2));
		Assert.IsFalse(negation.HasFlag(TestEnum.Value1));
		Assert.IsTrue(negation.HasFlag(TestEnum.Value2));
		Assert.IsTrue(mixed.HasFlag(TestEnum.Value1));
		Assert.IsTrue(mixed.HasFlag(TestEnum.Value2));
	}

	[Test]
	public void FlagEnum_Empty_IsEmpty()
	{
		// Arrange
		var emptyFlagEnum = new FlagEnum<TestEnum>();

		// Act & Assert
		Assert.IsTrue(emptyFlagEnum.IsEmpty);
		Assert.AreEqual("None", emptyFlagEnum.ToString());
	}

	[Test]
	public void FlagEnum_Count_ReturnsCorrectCount()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2, TestEnum.Value3); // Value1, Value2, and Value3 are set

		// Act
		var count = flagTestEnum.Count;

		// Assert
		Assert.AreEqual(3, count);
	}

	[Test]
	public void FlagEnum_Equality_WithDifferentTypes_ReturnsFalse()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1);
		var anotherFlagTestEnum = new FlagEnum<TestLargeEnum>(TestLargeEnum.Value1);

		// Act
		var areEqual = flagTestEnum.Equals(anotherFlagTestEnum);

		// Assert
		Assert.IsFalse(areEqual);
	}

	[Test]
	public void FlagEnum_GetFlags()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2, TestEnum.Value3);

		// Act
		var flags = flagTestEnum.GetFlags();

		// Assert
		Assert.AreEqual(3, flags.Count());
		Assert.That(flags, Is.EquivalentTo(new[] { TestEnum.Value1, TestEnum.Value2, TestEnum.Value3 }));
	}

	[Test]
	public void FlagEnum_ToList()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2, TestEnum.Value3);

		// Act
		var list = flagTestEnum.ToList();

		// Assert
		Assert.AreEqual(3, list.Count);
		Assert.That(list, Is.EquivalentTo(new[] { TestEnum.Value1, TestEnum.Value2, TestEnum.Value3 }));
	}

	[Test]
	public void FlagEnum_ToArray()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2, TestEnum.Value3);

		// Act
		var array = flagTestEnum.ToArray();

		// Assert
		Assert.AreEqual(3, array.Length);
		Assert.That(array, Is.EquivalentTo(new[] { TestEnum.Value1, TestEnum.Value2, TestEnum.Value3 }));
	}
}
