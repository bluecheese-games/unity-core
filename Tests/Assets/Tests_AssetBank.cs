//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// EditMode tests for AssetBank. Real .asset files are created on disk so AssetBaseRef can
// resolve them through the AssetDatabase (the editor load path).
public class Tests_AssetBank
{
	private const string TempFolder = "Assets/__AssetBankTests__";

	private readonly List<AssetBase> _createdAssets = new();

	[SetUp]
	public void SetUp()
	{
		if (!AssetDatabase.IsValidFolder(TempFolder))
			AssetDatabase.CreateFolder("Assets", "__AssetBankTests__");
	}

	[TearDown]
	public void TearDown()
	{
		AssetBank.ResetForTests();
		_createdAssets.Clear();
		if (AssetDatabase.IsValidFolder(TempFolder))
			AssetDatabase.DeleteAsset(TempFolder);
	}

	#region Helpers

	private T CreateAsset<T>(string assetName, params string[] tags) where T : AssetBase =>
		CreateAsset<T>(assetName, assetName, tags);

	// fileName must be unique on disk; displayName is the logical Name stored in the asset.
	private T CreateAsset<T>(string fileName, string displayName, string[] tags) where T : AssetBase
	{
		var asset = ScriptableObject.CreateInstance<T>();
		asset.Name = displayName;
		asset.Tags = tags;
		AssetDatabase.CreateAsset(asset, $"{TempFolder}/{fileName}.asset");
		_createdAssets.Add(asset);
		return asset;
	}

	private static AssetBaseRef RefOf(AssetBase asset) => AssetBaseRef.FromAsset(asset);

	// A ref pointing to a non-existent GUID: indexable but impossible to load.
	private static AssetBaseRef GhostRef<T>(string name, params string[] tags) where T : AssetBase => new()
	{
		Name = name,
		Guid = "ffffffffffffffffffffffffffffffff",
		TypeName = typeof(T).AssemblyQualifiedName,
		Tags = tags,
		LoadMode = AssetLoadMode.Resources,
	};

	#endregion

	#region Initialization

	[Test]
	public void InitializeForTests_WithNullAssets_DoesNotThrow()
	{
		// Arrange, Act & Assert
		Assert.DoesNotThrow(() => AssetBank.InitializeForTests(null));
		Assert.IsEmpty(AssetBank.GetAllAssets());
	}

	[Test]
	public void GetAllAssets_ReturnsFedAssets()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		var b = CreateAsset<DummyAssetB>("Beta");
		AssetBank.InitializeForTests(new[] { RefOf(a), RefOf(b) });

		// Act
		var result = AssetBank.GetAllAssets().ToList();

		// Assert
		Assert.AreEqual(2, result.Count);
	}

	#endregion

	#region Lookup by name

	[Test]
	public void GetAssetByName_ExistingName_ReturnsAsset()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act
		var result = AssetBank.GetAssetByName<DummyAssetA>("Alpha");

		// Assert
		Assert.AreEqual(a, result);
	}

	[Test]
	public void GetAssetByName_UnknownName_ReturnsNull()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act
		var result = AssetBank.GetAssetByName<DummyAssetA>("Missing");

		// Assert
		Assert.IsNull(result);
	}

	[Test]
	public void TryGetAssetByName_ExistingName_ReturnsTrueAndAsset()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act
		var found = AssetBank.TryGetAssetByName<DummyAssetA>("Alpha", out var result);

		// Assert
		Assert.IsTrue(found);
		Assert.AreEqual(a, result);
	}

	[Test]
	public void TryGetAssetByName_UnknownName_ReturnsFalse()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act
		var found = AssetBank.TryGetAssetByName<DummyAssetA>("Missing", out var result);

		// Assert
		Assert.IsFalse(found);
		Assert.IsNull(result);
	}

	[Test]
	public async Task GetAssetByNameAsync_ExistingName_ReturnsAsset()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act
		var result = await AssetBank.GetAssetByNameAsync<DummyAssetA>("Alpha");

		// Assert
		Assert.AreEqual(a, result);
	}

	#endregion

	#region Lookup by guid and type

	[Test]
	public void GetAssetByGuid_ExistingGuid_ReturnsAsset()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		var assetRef = RefOf(a);
		AssetBank.InitializeForTests(new[] { assetRef });

		// Act
		var result = AssetBank.GetAssetByGuid<DummyAssetA>(assetRef.Guid);

		// Assert
		Assert.AreEqual(a, result);
	}

	[Test]
	public void GetAssetOfType_ReturnsAssetOfThatType()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		var b = CreateAsset<DummyAssetB>("Beta");
		AssetBank.InitializeForTests(new[] { RefOf(a), RefOf(b) });

		// Act
		var result = AssetBank.GetAssetOfType<DummyAssetB>();

		// Assert
		Assert.AreEqual(b, result);
	}

	[Test]
	public void GetAssetsOfType_ReturnsAllOfType()
	{
		// Arrange
		var a1 = CreateAsset<DummyAssetA>("Alpha1");
		var a2 = CreateAsset<DummyAssetA>("Alpha2");
		var b = CreateAsset<DummyAssetB>("Beta");
		AssetBank.InitializeForTests(new[] { RefOf(a1), RefOf(a2), RefOf(b) });

		// Act
		var result = AssetBank.GetAssetsOfType<DummyAssetA>().ToList();

		// Assert
		Assert.AreEqual(2, result.Count);
		Assert.Contains(a1, result);
		Assert.Contains(a2, result);
	}

	#endregion

	#region Lookup by tag

	[Test]
	public void GetAssetsByTag_ReturnsTaggedAssets()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha", "shared");
		var b = CreateAsset<DummyAssetB>("Beta", "shared");
		var c = CreateAsset<DummyAssetA>("Gamma", "other");
		AssetBank.InitializeForTests(new[] { RefOf(a), RefOf(b), RefOf(c) });

		// Act
		var result = AssetBank.GetAssetsByTag<AssetBase>("shared").ToList();

		// Assert
		Assert.AreEqual(2, result.Count);
		Assert.Contains(a, result);
		Assert.Contains(b, result);
	}

	[Test]
	public void GetAssetsByTag_MultiTagAsset_RetrievableByEachTag()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha", "red", "round");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act
		var byRed = AssetBank.GetAssetsByTag<DummyAssetA>("red").ToList();
		var byRound = AssetBank.GetAssetsByTag<DummyAssetA>("round").ToList();

		// Assert
		Assert.Contains(a, byRed);
		Assert.Contains(a, byRound);
	}

	[Test]
	public void GetAssetsByTag_UnknownTag_ReturnsEmpty()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha", "shared");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act
		var result = AssetBank.GetAssetsByTag<AssetBase>("missing").ToList();

		// Assert
		Assert.IsEmpty(result);
	}

	[Test]
	public void GetAssetsByTag_NullOrEmptyTag_ReturnsEmpty()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha", "shared");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act
		var nullResult = AssetBank.GetAssetsByTag<AssetBase>(null).ToList();
		var emptyResult = AssetBank.GetAssetsByTag<AssetBase>(string.Empty).ToList();

		// Assert
		Assert.IsEmpty(nullResult);
		Assert.IsEmpty(emptyResult);
	}

	#endregion

	#region Unload by tag

	[Test]
	public void UnloadAssetsByTag_NullOrEmptyTag_DoesNotThrow()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha", "shared");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act & Assert
		Assert.DoesNotThrow(() => AssetBank.UnloadAssetsByTag(null));
		Assert.DoesNotThrow(() => AssetBank.UnloadAssetsByTag(string.Empty));
	}

	[Test]
	public void UnloadAssetsByTag_UnknownTag_DoesNotThrow()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha", "shared");
		AssetBank.InitializeForTests(new[] { RefOf(a) });

		// Act & Assert
		Assert.DoesNotThrow(() => AssetBank.UnloadAssetsByTag("missing"));
	}

	[Test]
	public void UnloadAssetsByTag_KnownTag_ThenReload_StillReturnsAsset()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha", "shared");
		AssetBank.InitializeForTests(new[] { RefOf(a) });
		_ = AssetBank.GetAssetByName<DummyAssetA>("Alpha"); // load into cache

		// Act
		AssetBank.UnloadAssetsByTag("shared");
		var reloaded = AssetBank.GetAssetByName<DummyAssetA>("Alpha");

		// Assert
		Assert.IsNotNull(reloaded);
	}

	#endregion

	#region Async null filtering

	[Test]
	public async Task GetAssetsOfTypeAsync_FiltersOutFailedLoads()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		AssetBank.InitializeForTests(new[] { RefOf(a), GhostRef<DummyAssetA>("Ghost") });
		LogAssert.Expect(LogType.Error, new Regex("Failed to load asset"));

		// Act
		var result = await AssetBank.GetAssetsOfTypeAsync<DummyAssetA>();

		// Assert
		Assert.AreEqual(1, result.Length);
		Assert.IsFalse(result.Any(asset => asset == null));
	}

	[Test]
	public async Task GetAssetsByTagAsync_FiltersOutFailedLoads()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha", "shared");
		AssetBank.InitializeForTests(new[] { RefOf(a), GhostRef<DummyAssetA>("Ghost", "shared") });
		LogAssert.Expect(LogType.Error, new Regex("Failed to load asset"));

		// Act
		var result = await AssetBank.GetAssetsByTagAsync<AssetBase>("shared");

		// Assert
		Assert.AreEqual(1, result.Length);
		Assert.IsFalse(result.Any(asset => asset == null));
	}

	#endregion

	#region Index integrity

	[Test]
	public void RebuildIndex_DuplicateName_LogsWarning()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("DupA", "Dup", System.Array.Empty<string>());
		var b = CreateAsset<DummyAssetB>("DupB", "Dup", System.Array.Empty<string>());
		LogAssert.Expect(LogType.Warning, new Regex("Duplicate name 'Dup'"));

		// Act
		AssetBank.InitializeForTests(new[] { RefOf(a), RefOf(b) });

		// Assert
		Assert.AreEqual(2, AssetBank.GetAllAssets().Count());
	}

	[Test]
	public void RebuildIndex_UnresolvableType_LogsWarningAndKeepsOtherAssets()
	{
		// Arrange
		var a = CreateAsset<DummyAssetA>("Alpha");
		var brokenRef = new AssetBaseRef
		{
			Name = "Broken",
			Guid = "ffffffffffffffffffffffffffffffff",
			TypeName = "Totally.Unknown.Type, NonExistentAssembly",
			LoadMode = AssetLoadMode.Resources,
		};
		LogAssert.Expect(LogType.Warning, new Regex("Could not resolve type"));

		// Act
		AssetBank.InitializeForTests(new[] { RefOf(a), brokenRef });

		// Assert
		Assert.AreEqual(a, AssetBank.GetAssetOfType<DummyAssetA>());
	}

	#endregion
}
