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
		[HideInInspector] public AssetLoadMode LoadMode = AssetLoadMode.Resources;

#if UNITY_EDITOR
		public string Guid => UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));

		public string TypeName => this.GetType().AssemblyQualifiedName;

		protected void OnValidate()
		{
			if (string.IsNullOrEmpty(Name))
			{
				Name = name;
			}
		}

		public virtual void OnRegister() { }
#endif
	}
}
