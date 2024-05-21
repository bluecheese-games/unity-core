//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.Core.Config
{
    public class ConfigRegistry
    {
        private readonly Dictionary<string, ConfigItem> _items = new();

        private bool _isLoaded = false;

        private static ConfigRegistry _instance;
        public static ConfigRegistry Instance
        {
            get
            {
                _instance ??= new ConfigRegistry();
                return _instance;
            }
        }

        private ConfigRegistry() { }

        private void EnsureItemsAreLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            var assetsManager = Resources.FindObjectsOfTypeAll<ConfigAssetsManager>().FirstOrDefault();
            if (assetsManager != null)
            {
                Load(assetsManager.ConfigAssets);
            }

            _isLoaded = true;
        }

        public void Load(params ConfigAsset[] assets)
        {
            foreach (var asset in assets)
            {
                LoadAsset(asset);
            }
        }

        private void LoadAsset(ConfigAsset asset) => AddItems(asset.Items);

        private void AddItems(params ConfigItem[] items)
        {
            foreach (var item in items)
            {
                AddItem(item);
            }
        }

        private void AddItem(ConfigItem item)
        {
            if (!_items.ContainsKey(item.Key))
            {
                _items[item.Key] = item;
            }
        }

        public string GetString(string key, string defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.StringValue;
        }

        public int GetInt(string key, int defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.IntValue;
        }

        public float GetFloat(string key, float defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.FloatValue;
        }

        public bool GetBool(string key, bool defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.BoolValue;
        }

        public Object GetObject(string key, Object defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.ObjectValue;
        }

        public void SetString(string key, string value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.String;
            item.StringValue = value;
        }

        public void SetInt(string key, int value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Int;
            item.IntValue = value;
        }

        public void SetFloat(string key, float value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Float;
            item.FloatValue = value;
        }

        public void SetBool(string key, bool value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Boolean;
            item.BoolValue = value;
        }

        public void SetObject(string key, Object value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Object;
            item.ObjectValue = value;
        }

        private ConfigItem GetItem(string key, bool createIfNotExists = false)
        {
            EnsureItemsAreLoaded();

            if (!_items.ContainsKey(key))
            {
                if (createIfNotExists == false)
                {
                    return null;
                }
                _items[key] = new ConfigItem(key, ConfigItem.ValueType.String);
            }
            return _items[key];
        }
    }
}
