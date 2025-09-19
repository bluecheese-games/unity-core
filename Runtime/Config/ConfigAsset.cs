//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Core.Config
{
    [CreateAssetMenu(fileName = "Config_New", menuName = "Config/Asset")]
    public class ConfigAsset : ScriptableObject
    {
        public ConfigItem[] Items;

        private void OnValidate()
        {
            foreach (var item in Items)
            {
                item.Cleanup();
            }
        }
    }
}
