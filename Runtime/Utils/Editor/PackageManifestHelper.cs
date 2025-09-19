//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace BlueCheese.Core
{
	public static class PackageManifestHelper
	{
		private static Manifest _manifest;

		private static void EnsureDependenciesInitialized()
		{
			if (_manifest != null && _manifest.dependencies.Count > 0) return;

			_manifest = Manifest.Load();
		}

		public static void AddDependency(string packageName, string version = "latest")
		{
			if (string.IsNullOrEmpty(packageName))
				throw new ArgumentException("Package name cannot be null or empty.", nameof(packageName));

			if (string.IsNullOrEmpty(version))
				throw new ArgumentException("Version cannot be null or empty.", nameof(version));

			EnsureDependenciesInitialized();

			_manifest.dependencies[packageName] = version;
		}

		public static void RemoveDependency(string packageName)
		{
			if (string.IsNullOrEmpty(packageName))
				throw new ArgumentException("Package name cannot be null or empty.", nameof(packageName));

			EnsureDependenciesInitialized();

			_manifest.dependencies.Remove(packageName);
		}

		public static string GetDependencyVersion(string packageName)
		{
			if (string.IsNullOrEmpty(packageName))
				throw new ArgumentException("Package name cannot be null or empty.", nameof(packageName));

			EnsureDependenciesInitialized();

			_manifest.dependencies.TryGetValue(packageName, out var version);
			return version;
		}

		public static bool HasDependency(string packageName, out string version)
		{
			if (string.IsNullOrEmpty(packageName))
				throw new ArgumentException("Package name cannot be null or empty.", nameof(packageName));

			EnsureDependenciesInitialized();

			return _manifest.dependencies.TryGetValue(packageName, out version);
		}

		public static void SaveManifest()
		{
			if (_manifest == null) return;

			Manifest.Save(_manifest);
		}

		[Serializable]
		private class Manifest
		{
			private const string _path = @"..\Packages\manifest.json";

			public readonly Dictionary<string, string> dependencies = new();

			public static Manifest Load()
			{
				var manifestPath = System.IO.Path.Combine(Application.dataPath, _path);
				if (!System.IO.File.Exists(manifestPath))
				{
					Debug.LogError($"Manifest file not found at {manifestPath}. Please ensure the path is correct.");
					return null;
				}

				// Load the manifest file
				var json = System.IO.File.ReadAllText(manifestPath);
				return JsonConvert.DeserializeObject<Manifest>(json);
			}

			public static void Save(Manifest manifest)
			{
				// Save the manifest file
				var manifestPath = System.IO.Path.Combine(Application.dataPath, _path);
				var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
				System.IO.File.WriteAllText(manifestPath, json);
			}
		}
	}
}
