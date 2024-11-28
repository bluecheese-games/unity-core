//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Core.Utils
{
	[Flags]
	public enum PlayTime
	{
		None = 0,
		OnAwake = 1 << 0,
		OnStart = 1 << 1,
		OnEnable = 1 << 2,
		OnDisable = 1 << 3,
		OnDestroy = 1 << 4,
		OnDespawn = 1 << 5,
		OnRecycle = 1 << 6,
	}
}
