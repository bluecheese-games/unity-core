using BlueCheese.Core.Utils;
using NUnit.Framework;

public class Tests_Tags
{
	#region Count

	[Test]
	public void Count_DefaultTags_ReturnsZero()
	{
		// Arrange
		var tags = new Tags();

		// Act
		var result = tags.Count;

		// Assert
		Assert.AreEqual(0, result);
	}

	[Test]
	public void Count_WithValues_ReturnsLength()
	{
		// Arrange
		Tags tags = new[] { "a", "b", "c" };

		// Act
		var result = tags.Count;

		// Assert
		Assert.AreEqual(3, result);
	}

	#endregion

	#region Contains

	[Test]
	public void Contains_ExistingValue_ReturnsTrue()
	{
		// Arrange
		Tags tags = new[] { "red", "blue" };

		// Act
		var result = tags.Contains("blue");

		// Assert
		Assert.IsTrue(result);
	}

	[Test]
	public void Contains_MissingValue_ReturnsFalse()
	{
		// Arrange
		Tags tags = new[] { "red", "blue" };

		// Act
		var result = tags.Contains("green");

		// Assert
		Assert.IsFalse(result);
	}

	[Test]
	public void Contains_DefaultTags_ReturnsFalse()
	{
		// Arrange
		var tags = new Tags();

		// Act
		var result = tags.Contains("anything");

		// Assert
		Assert.IsFalse(result);
	}

	#endregion

	#region Combine

	[Test]
	public void Combine_MergesUniqueValues()
	{
		// Arrange
		Tags tags = new[] { "a", "b" };
		Tags other = new[] { "c", "d" };

		// Act
		tags.Combine(other);

		// Assert
		Assert.AreEqual(4, tags.Count);
		Assert.IsTrue(tags.Contains("c"));
		Assert.IsTrue(tags.Contains("d"));
	}

	[Test]
	public void Combine_RemovesDuplicates()
	{
		// Arrange
		Tags tags = new[] { "a", "b" };
		Tags other = new[] { "b", "c" };

		// Act
		tags.Combine(other);

		// Assert
		Assert.AreEqual(3, tags.Count);
	}

	[Test]
	public void Combine_IntoDefaultTags_AddsValues()
	{
		// Arrange
		var tags = new Tags();
		Tags other = new[] { "a", "b" };

		// Act
		tags.Combine(other);

		// Assert
		Assert.AreEqual(2, tags.Count);
		Assert.IsTrue(tags.Contains("a"));
	}

	#endregion

	#region Conversions and indexer

	[Test]
	public void ImplicitFromArray_SetsValues()
	{
		// Arrange
		var values = new[] { "x", "y" };

		// Act
		Tags tags = values;

		// Assert
		Assert.AreEqual(2, tags.Count);
	}

	[Test]
	public void ImplicitToArray_ReturnsValues()
	{
		// Arrange
		Tags tags = new[] { "x", "y" };

		// Act
		string[] values = tags;

		// Assert
		Assert.AreEqual(new[] { "x", "y" }, values);
	}

	[Test]
	public void ImplicitToArray_DefaultTags_ReturnsEmptyArray()
	{
		// Arrange
		var tags = new Tags();

		// Act
		string[] values = tags;

		// Assert
		Assert.IsNotNull(values);
		Assert.AreEqual(0, values.Length);
	}

	[Test]
	public void Indexer_ReturnsValueAtIndex()
	{
		// Arrange
		Tags tags = new[] { "first", "second" };

		// Act
		var result = tags[1];

		// Assert
		Assert.AreEqual("second", result);
	}

	#endregion

	#region ToString

	[Test]
	public void ToString_DefaultTags_ReturnsNone()
	{
		// Arrange
		var tags = new Tags();

		// Act
		var result = tags.ToString();

		// Assert
		Assert.AreEqual("None", result);
	}

	[Test]
	public void ToString_WithValues_ReturnsCommaSeparated()
	{
		// Arrange
		Tags tags = new[] { "a", "b", "c" };

		// Act
		var result = tags.ToString();

		// Assert
		Assert.AreEqual("a, b, c", result);
	}

	#endregion
}
