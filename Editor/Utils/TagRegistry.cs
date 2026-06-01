//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// Maintains a cached, sorted list of all tag values found across every <see cref="AssetBase"/>
	/// asset in the project. Refreshes automatically when the AssetDatabase changes.
	/// </summary>
	[InitializeOnLoad]
	public static class TagRegistry
	{
		private static string[] _tags = Array.Empty<string>();
		private static bool _dirty = true;

		static TagRegistry()
		{
			EditorApplication.projectChanged += Invalidate;
		}

		/// <summary>
		/// Marks the registry as stale so it rescans on the next <see cref="GetKnownTags"/> call.
		/// Call this whenever a tag is added or removed without triggering a project change event.
		/// </summary>
		public static void Invalidate() => _dirty = true;

		/// <summary>
		/// Returns all known tag values sorted alphabetically.
		/// Triggers a project scan if the registry is stale.
		/// </summary>
		public static string[] GetKnownTags()
		{
			if (_dirty)
				Refresh();
			return _tags;
		}

		private static void Refresh()
		{
			var set = new HashSet<string>(StringComparer.Ordinal);
			foreach (var asset in AssetBankGenerator.FindAssets())
			{
				string[] values = asset.Tags;
				if (values == null) continue;
				foreach (var v in values)
					if (!string.IsNullOrWhiteSpace(v))
						set.Add(v.Trim());
			}
			_tags = set.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToArray();
			_dirty = false;
		}
	}
}
