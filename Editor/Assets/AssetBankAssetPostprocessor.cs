using BlueCheese.Core.Utils;
using System.Linq;
using UnityEditor;

namespace BlueCheese.Core.Editor
{
	class AssetBankAssetPostprocessor : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			// Only regenerate when an .asset file is affected.
			// Checking the extension avoids scanning the entire project on every texture, audio, or script import.
			// Deleted assets can no longer be loaded, so the extension check is the best available heuristic.
			bool affectsAssetBank =
				importedAssets.Concat(deletedAssets).Concat(movedAssets)
					.Any(p => p.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase)
						   && IsOrCouldBeAssetBase(p));

			if (affectsAssetBank)
				AssetBankGenerator.Regenerate();
		}

		private static bool IsOrCouldBeAssetBase(string assetPath)
		{
			// For existing assets, verify the type. For deleted ones (no longer on disk), allow any .asset through.
			var obj = AssetDatabase.LoadAssetAtPath<AssetBase>(assetPath);
			return obj != null || !AssetDatabase.AssetPathExists(assetPath);
		}
	}
}
