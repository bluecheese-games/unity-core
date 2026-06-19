//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.Core.Config
{
	[CreateAssetMenu(fileName = "Config_New", menuName = "Config/Asset")]
	public class ConfigAsset : AssetBase
	{
		public ConfigItem[] Items;

		private Dictionary<string, ConfigItem> _itemDictionary;

		public override void OnRegister()
		{
			foreach (var item in Items)
			{
				item.Cleanup();
			}
		}

		private void EnsureDictionary()
		{
			if (_itemDictionary == null)
			{
				_itemDictionary = new Dictionary<string, ConfigItem>();
				foreach (var item in Items)
				{
					_itemDictionary[item.Key] = item;
				}
			}
		}

		public T Get<T>(string key, T defaultValue = default)
		{
			EnsureDictionary();
			if (_itemDictionary.TryGetValue(key, out var item))
			{
				return item.GetValue(defaultValue);
			}
			return defaultValue;
		}
	}
}
