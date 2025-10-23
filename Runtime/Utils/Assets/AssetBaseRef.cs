//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.IO;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	[Serializable]
	public class AssetBaseRef
	{
		public string Name;
		public string Guid;
		public string TypeName;
		public Tags Tags;
		public AssetLoadMode Mode = AssetLoadMode.Resources;

		private Type _type;
		private AssetBase _loadedAsset;

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
				Guid = asset.Guid,
				TypeName = asset.TypeName,
				Tags = asset.Tags,
				Mode = asset.LoadMode,
			};
		}

		public bool TryCopyToResourcesFolder(string destResourcesAssetBankDir, out string destPath)
		{
			destPath = null;

			if (Mode != AssetLoadMode.Resources)
				return false;

			if (string.IsNullOrEmpty(Guid))
			{
				Debug.LogError($"[AssetBank] Asset '{Name}' has no GUID.");
				return false;
			}

			// Resolve source path from GUID
			string srcPath = UnityEditor.AssetDatabase.GUIDToAssetPath(Guid);
			if (string.IsNullOrEmpty(srcPath))
			{
				Debug.LogError($"[AssetBank] Could not resolve source path for GUID '{Guid}' ({Name}).");
				return false;
			}

			// Ensure destination exists
			Directory.CreateDirectory(destResourcesAssetBankDir);

			// Keep original extension, but enforce GUID-based filename
			string ext = Path.GetExtension(srcPath);
			string fileName = string.IsNullOrEmpty(ext) ? Guid : (Guid + ext);

			destPath = Path.Combine(destResourcesAssetBankDir, fileName);

			try
			{
				File.Copy(srcPath, destPath, overwrite: true);
				return true;
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[AssetBank] Copy failed for '{Name}' ({Guid}) → {destPath}\n{ex}");
				destPath = null;
				return false;
			}
		}
#endif

		public bool TryLoad<T>(out T asset) where T : AssetBase
		{
			if (_loadedAsset is T cachedAsset)
			{
				asset = cachedAsset;
				return true;
			}

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
					var fullPath = $"{AssetBank.AssetsResourcePath}/{Guid}";
					asset = Resources.Load<T>(fullPath);
					break;
				case AssetLoadMode.Addressables:
					Debug.LogError("Addressables loading not implemented yet.");
					asset = null;
					break;
			}

			_loadedAsset = asset;

			return asset != null;
		}
	}
}
