//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Core
{
	public static class GameObjectExtensions
	{
		public static T AddOrGetComponent<T>(this GameObject go) where T : Component
		{
			if (go.TryGetComponent(out T component))
			{
				return component;
			}
			return go.AddComponent<T>();
		}

		public static T AddOrGetComponent<T>(this Component component) where T : Component
			=> component.gameObject.AddOrGetComponent<T>();
	}
}
