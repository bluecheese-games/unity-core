//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.Editor
{
	class AssetBankAssetPostprocessor : UnityEditor.AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			AssetBankGenerator.Regenerate();
		}
	}
}
