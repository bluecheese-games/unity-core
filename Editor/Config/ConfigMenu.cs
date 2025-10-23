//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;
using UnityEditor;

namespace BlueCheese.Core.Config.Editor
{
    public static class ConfigMenu
    {
        [MenuItem(itemName: "Config/Edit")]
        private static void EditConfigData()
        {
            var configManager = FindConfigAssetsManager();
            if (configManager != null)
            {
                EditorUtility.FocusProjectWindow();
                AssetDatabase.OpenAsset(configManager);
            }
        }

        [MenuItem(itemName: "Config/Update Generated Code")]
        private static void GenerateConfigCode()
        {
            var configManager = FindConfigAssetsManager();
            if (configManager != null)
            {
                configManager.GenerateCode();
            }
        }

        private static ConfigAssetsManager FindConfigAssetsManager()
        {
            var configManager = Resources.Load<ConfigAssetsManager>("ConfigManager");
            if (!configManager)
            {
                Debug.LogWarning("You first need to create a Config Manager in a Resources folder");
            }
            return configManager;
        }
    }
}
