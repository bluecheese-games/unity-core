//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace BlueCheese.Core.Utils
{
	public class ObjectCaller : MonoBehaviour, IRecyclable, IDespawnable
	{
		private PlayTime _playTime = PlayTime.None;
		private float _delay = 0f;
		private Action _callback;

		private bool _enabled = true;
		private bool _started = false;

		public static void Setup(GameObject go, PlayTime playTime, float delay, Action callback)
		{
			var caller = go.AddComponent<ObjectCaller>();
			caller.hideFlags = HideFlags.HideInInspector;
			caller._playTime = playTime;
			caller._delay = delay;
			caller._callback = callback;

			if (!caller._started)
			{
				_ = caller.HandlePlayTimeAsync(PlayTime.OnAwake);
				_ = caller.HandlePlayTimeAsync(PlayTime.OnEnable);
			}
		}

		private async void Awake() => await HandlePlayTimeAsync(PlayTime.OnAwake);

		private async void Start()
		{
			_started = true;
			await HandlePlayTimeAsync(PlayTime.OnStart);
		}

		private async void OnEnable() => await HandlePlayTimeAsync(PlayTime.OnEnable);

		void IRecyclable.OnRecycle() => HandlePlayTime(PlayTime.OnRecycle, true);

		private async void OnDisable() => await HandlePlayTimeAsync(PlayTime.OnDisable, true);

		void IDespawnable.OnDespawn() => HandlePlayTime(PlayTime.OnDespawn, true);

		private void OnDestroy()
		{
			HandlePlayTime(PlayTime.OnDestroy, true);
			_callback = null;
		}

		private async UniTask HandlePlayTimeAsync(PlayTime playTime, bool requireStarted = false)
		{
			if (_callback == null ||
				!_enabled ||
				(requireStarted && !_started) ||
				!_playTime.HasFlag(playTime))
			{
				return;
			}

			await CallAsync(_delay);
		}

		private void HandlePlayTime(PlayTime playTime, bool requireStarted = false)
		{
			if (_callback == null ||
				!_enabled ||
				(requireStarted && !_started) ||
				!_playTime.HasFlag(playTime))
			{
				return;
			}

			Call(_delay);
		}

		public async UniTask CallAsync(float delay = 0f)
		{
			if (delay > 0f)
			{
				await UniTask.Delay(TimeSpan.FromSeconds(delay));
			}

			Call();
		}

		public void Call(float delay)
		{
			if (delay > 0f)
			{
				UniTask.RunOnThreadPool(() => CallAsync(delay));
			}
			else
			{
				Call();
			}
		}

		public void Call()
		{
			_callback?.Invoke();
		}

		private void OnApplicationQuit()
		{
			_enabled = false;
		}
	}
}
