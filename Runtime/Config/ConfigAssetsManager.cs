//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using NaughtyAttributes;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core.Config
{
    [CreateAssetMenu(fileName = "ConfigManager", menuName = "Config/Manager")]
    public class ConfigAssetsManager : ScriptableObject
    {
        public string GeneratedScriptsFolder = "Scripts/Configs";

        public ConfigAsset[] ConfigAssets;

        [Button]
        public void FindConfigAssets()
		{
#if UNITY_EDITOR
			ConfigAssets = UnityEditor.AssetDatabase.FindAssets(string.Format("t:{0}", typeof(ConfigAsset)))
                .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<ConfigAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();
#endif
		}

		[Button]
        public void GenerateCode()
        {
            foreach (var config in ConfigAssets)
            {
                ConfigCodeGen.Generate(config, GeneratedScriptsFolder);
            }
        }
    }
}
