//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

public class Tests_FlagEnum
{
	public enum TestEnum
	{
		Value0,
		Value1,
		Value2,
		Value3,
		Value4,
	}

	public enum TestLargeEnum
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

	// Sparse enum to ensure underlying-value indexing is respected
	public enum Sparse
	{
		A = 0,
		B = 2,
		C = 5,
		D = 7,
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

	private static long ComputeMask(params TestEnum[] flags)
	{
		long mask = 0;
		foreach (var f in flags)
			mask |= FlagEnum<TestEnum>.GetFlagValue(f);
		return mask;
	}

	[TestCase(new TestEnum[0], "0000000000000000000000000000000000000000000000000000000000000000",
		TestName = "Empty_Flags_ReturnAllZeroes")]

	[TestCase(new[] { TestEnum.Value0 }, "0000000000000000000000000000000000000000000000000000000000000001",
		TestName = "Single_Flag_Value0")]

	[TestCase(new[] { TestEnum.Value1, TestEnum.Value2 }, "0000000000000000000000000000000000000000000000000000000000000110",
		TestName = "Two_Flags_Value1_Value2")]

	[TestCase(new[] { TestEnum.Value0, TestEnum.Value1, TestEnum.Value2, TestEnum.Value3, TestEnum.Value4 },
		null, // will be computed dynamically
		TestName = "All_Flags_Set")]

	public void ToBinaryString_VariousCases(TestEnum[] flags, string expected)
	{
		// Arrange
		long mask = ComputeMask(flags);
		var flagEnum = new FlagEnum<TestEnum>(mask);

		// Act
		var result = flagEnum.ToBinaryString();

		// Assert
		if (expected == null)
		{
			// compute dynamic mask string for "All"
			expected = Convert.ToString(mask, 2).PadLeft(64, '0');
		}

		Assert.AreEqual(expected, result);
	}

	[Test]
	public void ToBinaryString_ToggleAndMaskCases()
	{
		// Toggle case: flip Value1 off, Value3 on
		var toggled = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2);
		toggled.ToggleFlag(TestEnum.Value1);
		toggled.AddFlag(TestEnum.Value3);

		long expectedMask = ComputeMask(TestEnum.Value2, TestEnum.Value3);
		string expected = Convert.ToString(expectedMask, 2).PadLeft(64, '0');
		Assert.AreEqual(expected, toggled.ToBinaryString(), "Toggle/Set should reflect correct bits.");

		// Masking: negative internal value (via ~)
		var all = new FlagEnum<TestEnum>(
			Enum.GetValues(typeof(TestEnum)).Cast<TestEnum>().ToArray());
		var not = ~all;
		string s = not.ToBinaryString();
		Assert.AreEqual(64, s.Length, "Binary string should always be 64 characters.");
		Assert.IsFalse(s.Contains('-'), "Binary string should not contain '-' even if underlying long is negative.");
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
		var mixedReverse = TestEnum.Value2 | flagTestEnum1;

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
		Assert.IsTrue(mixedReverse.HasFlag(TestEnum.Value1));
		Assert.IsTrue(mixedReverse.HasFlag(TestEnum.Value2));
	}

	[Test]
	public void FlagEnum_Empty_IsEmpty()
	{
		// Arrange
		var emptyFlagEnum = new FlagEnum<TestEnum>();

		// Act & Assert
		Assert.AreEqual(0, emptyFlagEnum.Count());
		Assert.AreEqual("None", emptyFlagEnum.ToString());
	}

	[Test]
	public void FlagEnum_Count_ReturnsCorrectCount()
	{
		// Arrange
		var flagTestEnum = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2, TestEnum.Value3); // Value1, Value2, and Value3 are set

		// Act
		var count = flagTestEnum.Count();

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

	[Test]
	public void FlagEnum_SparseEnum_SetHasAndAll()
	{
		var f = new FlagEnum<Sparse>(Sparse.B, Sparse.D);
		Assert.IsTrue(f.HasFlag(Sparse.B));
		Assert.IsFalse(f.HasFlag(Sparse.C));

		// Toggle
		f.ToggleFlag(Sparse.C);
		Assert.IsTrue(f.HasFlag(Sparse.C));

		// All via bitwise OR
		var all = new FlagEnum<Sparse>(Sparse.A, Sparse.B, Sparse.C, Sparse.D);
		Assert.AreEqual(all, (f | new FlagEnum<Sparse>(Sparse.A, Sparse.D)));
	}

	[Test]
	public void FlagEnum_NotOperator_IsMaskedToKnownBits()
	{
		var f = new FlagEnum<TestEnum>(TestEnum.Value0, TestEnum.Value2, TestEnum.Value4);
		var not = ~f;

		// The NOT should only flip within known enum bits (0..N of TestEnum)
		foreach (var v in Enum.GetValues(typeof(TestEnum)).Cast<TestEnum>())
		{
			bool expect = !f.HasFlag(v);
			Assert.AreEqual(expect, not.HasFlag(v), $"~ operator mismatch for {v}");
		}
	}

	[Test]
	public void Drawer_Display_UsesUnderlyingValues()
	{
		// Editor-only drawer isn't executed in tests; this is a smoke test ensuring
		// the runtime can represent sparse enums and produce display names.
		var f = new FlagEnum<Sparse>(Sparse.B, Sparse.D);
		string s = f.ToString();
		Assert.That(s.Contains("B") && s.Contains("D"));
	}

	[Test]
	public void MixedBitwise_And_Or_Xor_Work_With_Single_Enum()
	{
		var a = new FlagEnum<TestEnum>(TestEnum.Value0, TestEnum.Value2);
		var b = a | TestEnum.Value4;
		Assert.IsTrue(b.HasFlag(TestEnum.Value4));

		var c = b & TestEnum.Value2;
		Assert.AreEqual(new FlagEnum<TestEnum>(TestEnum.Value2), c);

		var d = a ^ TestEnum.Value2; // toggles Value2 off
		Assert.IsFalse(d.HasFlag(TestEnum.Value2));
	}

	[Test]
	public void Equality_With_Single_Enum_Works()
	{
		var f = new FlagEnum<TestEnum>(TestEnum.Value3);
		Assert.IsTrue(f == TestEnum.Value3);
		Assert.IsFalse(f != TestEnum.Value3);
		Assert.IsTrue(TestEnum.Value3 == f);
		Assert.IsFalse(TestEnum.Value1 == f);
	}

	[Test]
	public void Implicit_From_T_And_Explicit_To_Long_Work()
	{
		FlagEnum<TestEnum> f = TestEnum.Value1; // implicit
		Assert.IsTrue(f.HasFlag(TestEnum.Value1));

		long raw = (long)f; // explicit to long
		Assert.AreEqual(FlagEnum<TestEnum>.GetFlagValue(TestEnum.Value1), raw);
	}

	private static long Mask(params Sparse[] flags)
	{
		long m = 0;
		foreach (var f in flags)
			m |= FlagEnum<Sparse>.GetFlagValue(f);
		return m;
	}

	private static string Bin64(long mask) => Convert.ToString(mask, 2).PadLeft(64, '0');

	// Helper: build expected string by specifying which bit indices are 1 (e.g., 2,5 → "...00100100")
	private static string Bin64FromSetBits(params int[] setBits)
	{
		ulong v = 0;
		foreach (int i in setBits) v |= (1UL << i);
		return Convert.ToString((long)v, 2).PadLeft(64, '0');
	}

	[TestCase(new Sparse[0],
		"0000000000000000000000000000000000000000000000000000000000000000",
		TestName = "Sparse_Empty")]

	[TestCase(new[] { Sparse.A }, // bit 0
		"0000000000000000000000000000000000000000000000000000000000000001",
		TestName = "Sparse_A_bit0")]

	[TestCase(new[] { Sparse.B }, // bit 2
		"0000000000000000000000000000000000000000000000000000000000000100",
		TestName = "Sparse_B_bit2")]

	[TestCase(new[] { Sparse.C }, // bit 5
		"0000000000000000000000000000000000000000000000000000000000100000",
		TestName = "Sparse_C_bit5")]

	[TestCase(new[] { Sparse.D }, // bit 7
		"0000000000000000000000000000000000000000000000000000000010000000",
		TestName = "Sparse_D_bit7")]

	[TestCase(new[] { Sparse.B, Sparse.D }, // bits 2 & 7
		null, // compute dynamically
		TestName = "Sparse_BD_bits2_7")]

	[TestCase(new[] { Sparse.A, Sparse.B, Sparse.C, Sparse.D }, // "all"
		null, // compute dynamically
		TestName = "Sparse_All_ABCD")]
	public void ToBinaryString_Sparse_VariousCases(Sparse[] flags, string expected)
	{
		// Arrange
		long mask = Mask(flags);
		var f = new FlagEnum<Sparse>(mask);

		// Act
		var result = f.ToBinaryString();

		// Assert
		if (expected == null)
		{
			// dynamic expectations for multi-flag sets
			if (flags.Length == 2 && flags[0] == Sparse.B && flags[1] == Sparse.D)
				expected = Bin64FromSetBits(2, 7);
			else if (flags.Length == 4)
				expected = Bin64FromSetBits(0, 2, 5, 7);
			else
				expected = Bin64(mask); // fallback (shouldn’t hit)
		}

		Assert.AreEqual(expected, result);
	}

	[Test]
	public void ToBinaryString_Sparse_Toggle_And_Not_Masked()
	{
		// Start with B (2) and D (7)
		var f = new FlagEnum<Sparse>(Sparse.B, Sparse.D);
		Assert.AreEqual(Bin64FromSetBits(2, 7), f.ToBinaryString());

		// Toggle B off, add C (5)
		f.ToggleFlag(Sparse.B);
		f.AddFlag(Sparse.C);
		Assert.AreEqual(Bin64FromSetBits(5, 7), f.ToBinaryString(), "Toggle B off and add C should set bits 5 & 7.");

		// NOT is masked to known bits: {A(0),B(2),C(5),D(7)} flip → {A(0),B(2)} when current is {C(5),D(7)}
		var not = ~f;
		Assert.IsTrue(not.HasFlag(Sparse.A));
		Assert.IsTrue(not.HasFlag(Sparse.B));
		Assert.IsFalse(not.HasFlag(Sparse.C));
		Assert.IsFalse(not.HasFlag(Sparse.D));

		string s = not.ToBinaryString();
		Assert.AreEqual(64, s.Length, "Binary string should always be 64 chars.");
		Assert.IsFalse(s.Contains('-'), "Binary string should never contain a minus sign.");
		Assert.AreEqual(Bin64FromSetBits(0, 2), s, "NOT should flip only known sparse bits.");
	}

	[Test]
	public void IEnumerable_Foreach_OnlySetFlags_AreYielded_InEnumOrder()
	{
		// Arrange: set Value4, Value1, Value3 (out of order)
		var f = new FlagEnum<TestEnum>(TestEnum.Value4, TestEnum.Value1, TestEnum.Value3);

		// Act: foreach should yield in enum declaration / underlying-value order
		var iterated = new List<TestEnum>();
		foreach (var v in f) iterated.Add(v);

		// Assert: order is Value1, Value3, Value4 (NOT insertion order)
		CollectionAssert.AreEqual(
			new[] { TestEnum.Value1, TestEnum.Value3, TestEnum.Value4 },
			iterated
		);
	}

	[Test]
	public void IEnumerable_Empty_YieldsNothing()
	{
		var f = new FlagEnum<TestEnum>(); // no flags
		Assert.IsFalse(f.Any(), "Empty FlagEnum should enumerate zero elements.");
		CollectionAssert.IsEmpty(f.ToList(), "ToList on empty should be empty.");
	}

	[Test]
	public void IEnumerable_Linq_Basics_Work()
	{
		var f = new FlagEnum<TestEnum>(TestEnum.Value0, TestEnum.Value2, TestEnum.Value4);

		// Any/All
		Assert.IsTrue(f.Any());
		Assert.IsTrue(f.All(x => x.ToString().StartsWith("Value")));

		// Where + Select + Count
		var evens = f.Where(x =>
		{
			// parse the trailing digit from "ValueN"
			var s = x.ToString();
			int n = int.Parse(s.Substring("Value".Length));
			return n % 2 == 0;
		}).ToList();

		CollectionAssert.AreEquivalent(
			new[] { TestEnum.Value0, TestEnum.Value2, TestEnum.Value4 },
			evens
		);
		Assert.AreEqual(3, evens.Count);

		// Contains
		Assert.IsTrue(f.Contains(TestEnum.Value2));
		Assert.IsFalse(f.Contains(TestEnum.Value3));
	}

	[Test]
	public void IEnumerable_ReflectsCurrentState_AfterMutations()
	{
		var f = new FlagEnum<TestEnum>(TestEnum.Value1, TestEnum.Value2);

		// snapshot A
		var snapA = f.ToList();
		CollectionAssert.AreEqual(new[] { TestEnum.Value1, TestEnum.Value2 }, snapA);

		// mutate
		f.ToggleFlag(TestEnum.Value1);  // off
		f.AddFlag(TestEnum.Value3);     // on

		// snapshot B reflects new state
		var snapB = f.ToList();
		CollectionAssert.AreEqual(new[] { TestEnum.Value2, TestEnum.Value3 }, snapB);
	}

	[Test]
	public void IEnumerable_Sparse_OnlySetFlags_InEnumOrder_NotInsertionOrder()
	{
		// Insert in a scrambled order: D, B, A
		var f = new FlagEnum<Sparse>(Sparse.D, Sparse.B, Sparse.A);

		// Iterate
		var seq = f.ToList();

		// Expect enum order: A(0), B(2), D(7) — note C(5) is not set
		CollectionAssert.AreEqual(new[] { Sparse.A, Sparse.B, Sparse.D }, seq);
	}

	[Test]
	public void IEnumerable_Sparse_WithToggles()
	{
		// Start with B and D
		var f = new FlagEnum<Sparse>(Sparse.B, Sparse.D);

		// Toggle B off, add C → expect C, D
		f.ToggleFlag(Sparse.B);
		f.AddFlag(Sparse.C);

		var seq = f.ToList();
		CollectionAssert.AreEqual(new[] { Sparse.C, Sparse.D }, seq);

		// Add A → expect A, C, D
		f.AddFlag(Sparse.A);
		seq = f.ToList();
		CollectionAssert.AreEqual(new[] { Sparse.A, Sparse.C, Sparse.D }, seq);
	}

	[Test]
	public void IEnumerable_WorksInForeach_Syntax()
	{
		var f = new FlagEnum<TestEnum>(TestEnum.Value0, TestEnum.Value4);
		int count = 0;
		foreach (var _ in f) count++;
		Assert.AreEqual(2, count, "Foreach should iterate exactly the set flags.");
	}
}
