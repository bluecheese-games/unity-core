//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Core.Utils
{
	public class AssetBase : ScriptableObject
	{
		[SerializeField] private string _id;
		[SerializeField] private Tags _tags;
	}
}
