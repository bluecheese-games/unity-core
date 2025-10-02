//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.Utils
{
	class AssetBankAssetPostprocessor : UnityEditor.AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			AssetBankGenerator.Regenerate();
		}
	}
}
