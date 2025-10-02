//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Core.Utils
{
	public class AssetBase : ScriptableObject
	{
		[HideInInspector] public string Name = string.Empty;
		[HideInInspector] public Tags Tags = new();
		[HideInInspector] public bool RegisterInAssetBank = true;

		public int Id => GetInstanceID();

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (string.IsNullOrEmpty(Name))
			{
				Name = name;
			}
			AssetBankGenerator.Regenerate();
		}

		public virtual void OnRegister() { }
#endif
	}
}
