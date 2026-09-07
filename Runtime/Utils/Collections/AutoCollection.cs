using System.Collections.Generic;
using System.Linq;

namespace BlueCheese.Core.Utils
{
	/// <summary>
	/// A collection that automatically collects all assets of type T in the project.
	/// </summary>
	public abstract class AutoCollection<T> : Collection<T> where T : UnityEngine.Object
	{
#if UNITY_EDITOR
		// Content is rebuilt from the AssetDatabase on OnRegister, so manual edits would just be overwritten.
		public override bool IsEditable => false;

		public override void OnRegister()
		{
			base.OnRegister();

			// Get all assets of the specific type
			var assets = FindAssets();

			// Cleanup empty or null entries
			if (_items == null)
			{
				_items = new List<T>();
			}
			else
			{
				_items.Clear();
			}

			// Add found assets to the collection
			foreach (var asset in assets)
			{
				if (!_items.Contains(asset))
				{
					_items.Add(asset);
				}
			}
		}

		/// <summary>
		/// Finds all assets of type T in the project.
		/// You can override this method to customize the search behavior.
		/// /!\ This method is only called in the editor, place it inside #if UNITY_EDITOR /!\
		/// </summary>
		protected virtual IEnumerable<T> FindAssets() => UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}")
			.Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
			.Select(UnityEditor.AssetDatabase.LoadAssetAtPath<T>)
			.Where(asset => asset != null)
			.Where(CollectFilter)
			.OrderBy(asset => asset.name);

		/// <summary>
		/// Filters assets to be included in the collection.
		/// </summary>
		protected virtual bool CollectFilter(T asset) => true;
#endif

	}
}
