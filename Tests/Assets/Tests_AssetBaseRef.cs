//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using NUnit.Framework;
using System;

// Note: Tests that depend on Resources.Load, ScriptableObject, or AssetDatabase require the
// Unity Test Runner (EditMode/PlayMode) and cannot run as plain NUnit tests.
// This file covers the pure-C# surface of AssetBaseRef: validation and type resolution.

public class Tests_AssetBaseRef
{
	#region IsValid

	[Test]
	public void IsValid_WithGuidAndTypeName_ReturnsTrue()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			Guid = "abc123",
			TypeName = "Some.Assembly.Type, Assembly",
		};

		// Act
		var result = assetRef.IsValid;

		// Assert
		Assert.IsTrue(result);
	}

	[Test]
	public void IsValid_WithEmptyGuid_ReturnsFalse()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			Guid = string.Empty,
			TypeName = "Some.Assembly.Type, Assembly",
		};

		// Act
		var result = assetRef.IsValid;

		// Assert
		Assert.IsFalse(result);
	}

	[Test]
	public void IsValid_WithNullGuid_ReturnsFalse()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			Guid = null,
			TypeName = "Some.Assembly.Type, Assembly",
		};

		// Act
		var result = assetRef.IsValid;

		// Assert
		Assert.IsFalse(result);
	}

	[Test]
	public void IsValid_WithWhitespaceGuid_ReturnsFalse()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			Guid = "   ",
			TypeName = "Some.Assembly.Type, Assembly",
		};

		// Act
		var result = assetRef.IsValid;

		// Assert
		Assert.IsFalse(result);
	}

	[Test]
	public void IsValid_WithEmptyTypeName_ReturnsFalse()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			Guid = "abc123",
			TypeName = string.Empty,
		};

		// Act
		var result = assetRef.IsValid;

		// Assert
		Assert.IsFalse(result);
	}

	[Test]
	public void IsValid_WithNullTypeName_ReturnsFalse()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			Guid = "abc123",
			TypeName = null,
		};

		// Act
		var result = assetRef.IsValid;

		// Assert
		Assert.IsFalse(result);
	}

	[Test]
	public void IsValid_BothFieldsEmpty_ReturnsFalse()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			Guid = string.Empty,
			TypeName = string.Empty,
		};

		// Act
		var result = assetRef.IsValid;

		// Assert
		Assert.IsFalse(result);
	}

	#endregion

	#region Type resolution

	[Test]
	public void Type_WithValidAssemblyQualifiedName_ResolvesCorrectType()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			TypeName = typeof(string).AssemblyQualifiedName,
		};

		// Act
		var resolved = assetRef.Type;

		// Assert
		Assert.AreEqual(typeof(string), resolved);
	}

	[Test]
	public void Type_WithUnknownTypeName_ReturnsNull()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			TypeName = "Totally.Unknown.Type, NonExistentAssembly",
		};

		// Act
		var resolved = assetRef.Type;

		// Assert
		Assert.IsNull(resolved);
	}

	[Test]
	public void Type_CalledTwice_ReturnsSameInstance()
	{
		// Arrange
		var assetRef = new AssetBaseRef
		{
			TypeName = typeof(int).AssemblyQualifiedName,
		};

		// Act
		var first = assetRef.Type;
		var second = assetRef.Type;

		// Assert
		Assert.AreSame(first, second);
	}

	#endregion
}
