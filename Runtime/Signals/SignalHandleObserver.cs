//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Core.Signals
{
	[HideInInspector]
	public class SignalHandleObserver : MonoBehaviour
	{
		private object _handle;

		private void Awake()
		{
			hideFlags = HideFlags.HideInInspector;
		}

		public static void TryAddObserver(object handle)
		{
			if (handle == null)
			{
				return;
			}

			SignalHandleObserver observer = null;
			if (handle is GameObject go && !go.GetComponent<SignalHandleObserver>())
			{
				observer = go.AddComponent<SignalHandleObserver>();
			}
			else if (handle is Component component && !component.GetComponent<SignalHandleObserver>())
			{
				observer = component.gameObject.AddComponent<SignalHandleObserver>();
			}

			if (observer != null)
			{
				observer._handle = handle;
			}
		}

		private void OnDestroy()
		{
			if (_handle != null)
			{
				SignalAPI.Unsubscribe(_handle);
				_handle = null;
			}
		}
	}
}
