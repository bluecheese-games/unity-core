using BlueCheese.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueCheese.Core.Editor
{
	// Shared filtering logic for the AssetBank inspector and the Asset Bank Browser window,
	// so the two stay behaviorally identical.
	internal static class AssetBankFilterUtility
	{
		// First option is the "no filter" entry; the rest map to AssetLoadMode values.
		public static readonly string[] LoadModeOptions =
			new[] { "All" }.Concat(Enum.GetNames(typeof(AssetLoadMode))).ToArray();

		public static string[] BuildTagOptions(IEnumerable<AssetBaseRef> refs)
		{
			var tags = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var assetRef in refs)
				foreach (string tag in (string[])assetRef.Tags)
					if (!string.IsNullOrEmpty(tag)) tags.Add(tag);

			return new[] { "All" }.Concat(tags).ToArray();
		}

		public static string[] BuildBundleOptions(IEnumerable<AssetBaseRef> refs)
		{
			var bundles = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var assetRef in refs)
				if (!string.IsNullOrEmpty(assetRef.BundleKey)) bundles.Add(assetRef.BundleKey);

			return new[] { "All" }.Concat(bundles).ToArray();
		}

		public static bool Matches(AssetBaseRef assetRef, string search, AssetLoadMode? loadMode, string tag, string bundle)
		{
			if (loadMode.HasValue && assetRef.LoadMode != loadMode.Value)
				return false;

			if (!string.IsNullOrEmpty(tag) && !assetRef.Tags.Contains(tag))
				return false;

			if (!string.IsNullOrEmpty(bundle) && assetRef.BundleKey != bundle)
				return false;

			if (!string.IsNullOrEmpty(search))
			{
				bool hit =
					ContainsIgnoreCase(assetRef.Name, search) ||
					ContainsIgnoreCase(assetRef.Guid, search) ||
					((string[])assetRef.Tags).Any(t => ContainsIgnoreCase(t, search));
				if (!hit) return false;
			}

			return true;
		}

		public static bool ContainsIgnoreCase(string value, string search) =>
			!string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

		// Short display name for a type: the resolved Type's name, or a best-effort parse of the
		// assembly-qualified TypeName when the type could not be resolved (renamed/moved class).
		public static string GetShortTypeName(AssetBaseRef assetRef)
		{
			if (assetRef.Type != null)
				return assetRef.Type.Name;

			string typeName = assetRef.TypeName;
			if (string.IsNullOrEmpty(typeName))
				return "?";

			int comma = typeName.IndexOf(',');
			string withoutAssembly = comma >= 0 ? typeName[..comma] : typeName;
			int dot = withoutAssembly.LastIndexOf('.');
			return dot >= 0 ? withoutAssembly[(dot + 1)..] : withoutAssembly;
		}
	}
}
