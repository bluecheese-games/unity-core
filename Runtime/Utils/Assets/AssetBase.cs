//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Core.Utils
{
	public abstract class AssetBase : ScriptableObject
	{
		[HideInInspector] public string Name = string.Empty;
		[HideInInspector] public Tags Tags = new();
		[HideInInspector] public bool RegisterInAssetBank = true;
		[HideInInspector] public AssetLoadMode LoadMode = AssetLoadMode.Resources;

		// Addressables only: assets sharing a bundle key are packed into the same bundle.
		// Empty means the shared default AssetBank group.
		[HideInInspector] public string BundleKey = string.Empty;

		private string _guid;
		private string _typeName;

#if UNITY_EDITOR
		/// <summary>
		/// The asset's GUID as assigned by the AssetDatabase. Available in Editor only.
		/// </summary>
		public string Guid
		{
			get
			{
				if (!string.IsNullOrEmpty(_guid))
				{
					return _guid;
				}
				_guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));
				return _guid;
			}
		}

		/// <summary>
		/// The assembly-qualified name of the concrete type. Used to serialize type information into <see cref="AssetBaseRef"/>. Available in Editor only.
		/// </summary>
		public string TypeName
		{
			get
			{
				if (!string.IsNullOrEmpty(_typeName))
				{
					return _typeName;
				}
				_typeName = GetType().AssemblyQualifiedName;
				return _typeName;
			}
		}

		protected virtual void OnValidate()
		{
			if (string.IsNullOrEmpty(Name))
			{
				Name = name;
			}
		}

		/// <summary>
		/// Called when the asset is registered in the AssetBank.
		/// </summary>
		public virtual void OnRegister() { }
#endif
	}
}
