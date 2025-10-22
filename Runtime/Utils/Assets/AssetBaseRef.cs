//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	[Serializable]
	public class AssetBaseRef
	{
		public string Name;
		public string Path; // Path relative to Resources folder when using Resources load mode or Addressable address when using Addressables
		public string Guid;
		public string TypeName;
		public Tags Tags;
		public AssetLoadMode Mode = AssetLoadMode.Resources;

		private Type _type;

		public Type Type
		{
			get
			{
				if (_type != null)
				{
					return _type;
				}
				_type = Type.GetType(TypeName);
				return _type;
			}
		}

#if UNITY_EDITOR
		public static AssetBaseRef FromAsset(AssetBase asset)
		{
			return new AssetBaseRef
			{
				Name = asset.Name,
				Path = "", // TODO: Implement path extraction based on load mode
				Guid = asset.Guid,
				TypeName = asset.TypeName,
				Tags = asset.Tags,
				Mode = asset.LoadMode,
			};
		}
#endif

		public bool TryLoad<T>(out T asset) where T : AssetBase
		{
			asset = null;
#if UNITY_EDITOR
			// In the editor, always load via AssetDatabase for performance and correctness
			string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(Guid);
			asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
			if (asset != null)
			{
				return true;
			}
#endif

			switch (Mode)
			{
				case AssetLoadMode.Resources:
					var fullPath = System.IO.Path.Combine(AssetBank.ResourcesPath, Path);
					asset = Resources.Load<T>(fullPath);
					break;
				case AssetLoadMode.Addressables:
					Debug.LogError("Addressables loading not implemented yet.");
					asset = null;
					break;
			}
			return asset != null;
		}
	}
}
