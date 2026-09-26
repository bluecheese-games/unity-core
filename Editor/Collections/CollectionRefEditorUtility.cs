using System.Collections;
using System.Collections.Generic;
using BlueCheese.Core.Utils;
using UnityEngine;

namespace BlueCheese.Core.Editor
{
	/// <summary>
	/// Shared helpers for <see cref="CollectionItemRefPropertyDrawer"/> and
	/// <see cref="CollectionItemsRefPropertyDrawer"/>.
	/// </summary>
	internal static class CollectionRefEditorUtility
	{
		/// <summary>
		/// Live content of the assigned collection, via Collection&lt;T&gt;'s IEnumerable&lt;T&gt;
		/// implementation -- this lets a non-generic PropertyDrawer read the items without needing
		/// reflection to call the generic Items property.
		/// </summary>
		public static UnityEngine.Object[] GetCandidateItems(UnityEngine.Object collection)
		{
			if (collection is not IEnumerable enumerable)
				return System.Array.Empty<UnityEngine.Object>();

			var result = new List<UnityEngine.Object>();
			foreach (var obj in enumerable)
			{
				if (obj is UnityEngine.Object unityObj && unityObj != null)
					result.Add(unityObj);
			}
			return result.ToArray();
		}

		/// <summary>
		/// Display name for an item chip/dropdown entry: the AssetBase display Name when the item is
		/// one, falling back to the Unity asset file name, or "Missing" for a null/destroyed reference.
		/// </summary>
		public static string GetDisplayName(UnityEngine.Object item)
		{
			if (item == null)
				return "Missing";

			if (item is AssetBase assetBase && !string.IsNullOrEmpty(assetBase.Name))
				return assetBase.Name;

			return item.name;
		}
	}
}
