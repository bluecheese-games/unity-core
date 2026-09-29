using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// A collection that automatically collects all assets of type T in the project.
	/// </summary>
	public abstract class AutoCollection<T> : Collection<T> where T : UnityEngine.Object
	{
#if UNITY_EDITOR
		[Serializable]
		public enum SearchScope
		{
			AllAssets,
			CurrentFolder,
			SpecificFolders,
		}

		[Header("Search Settings")]
		[Tooltip("The scope of the search for assets.\n" +
			"AllAssets will search the entire project\n" +
			"CurrentFolder will search the folder (and subfolders) of this asset\n" +
			"SpecificFolders will search only the specified folders (and subfolders).")]
		[SerializeField] protected SearchScope _searchScope = SearchScope.AllAssets;
		[SerializeField] protected string[] _searchFolders = new string[0];

		// Content is rebuilt from the AssetDatabase on OnRegister, so manual edits would just be overwritten.
		public override bool IsEditable => false;

		public override void OnRegister()
		{
			base.OnRegister();

			// Get all assets of the specific type. Overlapping SpecificFolders paths (a folder and one
			// of its subfolders both listed) can hand back the same asset twice, hence the dedup.
			// A HashSet keeps this O(n) instead of the O(n^2) that List.Contains would cost here.
			var seen = new HashSet<T>();
			var newItems = new List<T>();
			foreach (var asset in FindAssets())
			{
				if (seen.Add(asset))
				{
					newItems.Add(asset);
				}
			}

			// Only touch _items -- and dirty/save this asset -- when the rebuilt content actually
			// changed. OnRegister() runs on every AssetBankGenerator.Regenerate() pass, including the
			// one triggered by this very asset being saved; dirtying unconditionally here would loop
			// forever (regen -> dirty -> save -> reimport -> regen -> ...).
			if (_items != null && _items.SequenceEqual(newItems))
				return;

			_items = newItems;
			UnityEditor.EditorUtility.SetDirty(this);
		}

		/// <summary>
		/// Finds all assets of type T in the specified search scope.
		/// This method uses Unity's AssetDatabase to search for assets.
		/// You can override this method to customize the search behavior.
		/// /!\ This method is only called in the editor, place it inside #if UNITY_EDITOR /!\
		/// </summary>
		protected virtual IEnumerable<T> FindAssets()
		{
			string[] searchFolders = _searchScope switch
			{
				// Scoped to "Assets" only, not AssetDatabase.FindAssets' own default of the whole project
				// (which also sweeps every installed package's Packages/ folder) -- otherwise a package's
				// own Sample/demo assets of type T (e.g. unity-app's SampleAudioBank) leak into every
				// consuming project's collection. Same fix as AssetBankGenerator's own project-asset scan.
				SearchScope.AllAssets => new[] { "Assets" },
				SearchScope.CurrentFolder => new string[] { System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(this)) },
				SearchScope.SpecificFolders => (_searchFolders ?? Array.Empty<string>())
					.Where(folder => !string.IsNullOrWhiteSpace(folder)).ToArray(),
				_ => throw new NotImplementedException($"Search scope {_searchScope} is not implemented."),
			};

			// AssetDatabase.FindAssets treats a null/empty folder list as "search the whole project" --
			// the opposite of what SpecificFolders with no folder configured (yet) should mean here.
			if (_searchScope == SearchScope.SpecificFolders && searchFolders.Length == 0)
				return Enumerable.Empty<T>();

			return UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", searchFolders)
				.Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
				.Select(UnityEditor.AssetDatabase.LoadAssetAtPath<T>)
				.Where(asset => asset != null)
				.Where(CollectFilter)
				.OrderBy(GetSortName);
		}

		// Sorts by the AssetBase display Name when the item is one (matches what's shown in its own
		// inspector/AssetBank), falling back to the Unity asset file name otherwise.
		private static string GetSortName(T asset)
		{
			if (asset is AssetBase assetBase && !string.IsNullOrEmpty(assetBase.Name))
				return assetBase.Name;

			return asset.name;
		}

		/// <summary>
		/// Filters assets to be included in the collection.
		/// </summary>
		protected virtual bool CollectFilter(T asset) => true;
#endif

	}
}
