//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Core.Utils
{
	public class AssetBase : ScriptableObject
	{
		public string Name = string.Empty;
		public Tags Tags = new();
		public bool RegisterInAssetBank = true;

		public int Id => GetInstanceID();

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (string.IsNullOrEmpty(Name))
			{
				Name = name;
			}
			AssetBank.Refresh();
		}

		public virtual void OnRegister() { }
#endif
	}
}
