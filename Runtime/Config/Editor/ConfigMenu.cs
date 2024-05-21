//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Core.Config.Editor
{
    public static class ConfigMenu
    {
        [UnityEditor.MenuItem(itemName: "Config/Edit")]
        private static void EditConfigData()
        {
            var configManager = FindConfigAssetsManager();
            if (configManager != null)
            {
                UnityEditor.EditorUtility.FocusProjectWindow();
                UnityEditor.AssetDatabase.OpenAsset(configManager);
            }
        }

        [UnityEditor.MenuItem(itemName: "Config/Update Generated Code")]
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
