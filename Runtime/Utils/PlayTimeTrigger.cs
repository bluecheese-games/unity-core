//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace BlueCheese.Core.Utils
{
	public class PlayTimeTrigger : MonoBehaviour, IRecyclable, IDespawnable
	{
		private PlayTimeEvent _playTime = PlayTimeEvent.None;
		private float _delay = 0f;
		private Action _callback;

		private bool _enabled = true;
		private bool _started = false;

		public static void Setup(GameObject go, PlayTimeEvent playTime, float delay, Action callback)
		{
			if (playTime == PlayTimeEvent.None || callback == null)
			{
				return;
			}

			var trigger = go.AddComponent<PlayTimeTrigger>();
			trigger.hideFlags = HideFlags.HideInInspector;
			trigger._playTime = playTime;
			trigger._delay = delay;
			trigger._callback = callback;

			if (!trigger._started)
			{
				_ = trigger.HandlePlayTimeEventAsync(PlayTimeEvent.OnAwake);
				_ = trigger.HandlePlayTimeEventAsync(PlayTimeEvent.OnEnable);
			}
		}

		private async void Awake() => await HandlePlayTimeEventAsync(PlayTimeEvent.OnAwake);

		private async void Start()
		{
			_started = true;
			await HandlePlayTimeEventAsync(PlayTimeEvent.OnStart);
		}

		private async void OnEnable() => await HandlePlayTimeEventAsync(PlayTimeEvent.OnEnable);

		void IRecyclable.OnRecycle() => HandlePlayTimeEvent(PlayTimeEvent.OnRecycle, true);

		private async void OnDisable() => await HandlePlayTimeEventAsync(PlayTimeEvent.OnDisable, true);

		void IDespawnable.OnDespawn() => HandlePlayTimeEvent(PlayTimeEvent.OnDespawn, true);

		private void OnDestroy()
		{
			HandlePlayTimeEvent(PlayTimeEvent.OnDestroy, true);
			_callback = null;
		}

		private async UniTask HandlePlayTimeEventAsync(PlayTimeEvent playTime, bool requireStarted = false)
		{
			if (_callback == null ||
				!_enabled ||
				(requireStarted && !_started) ||
				!_playTime.HasFlag(playTime))
			{
				return;
			}

			await TriggerAsync(_delay);
		}

		private void HandlePlayTimeEvent(PlayTimeEvent playTime, bool requireStarted = false)
		{
			if (_callback == null ||
				!_enabled ||
				(requireStarted && !_started) ||
				!_playTime.HasFlag(playTime))
			{
				return;
			}

			Trigger(_delay);
		}

		public async UniTask TriggerAsync(float delay = 0f)
		{
			if (delay > 0f)
			{
				await UniTask.Delay(TimeSpan.FromSeconds(delay));
			}

			Trigger();
		}

		public void Trigger(float delay)
		{
			if (delay > 0f)
			{
				UniTask.RunOnThreadPool(() => TriggerAsync(delay));
			}
			else
			{
				Trigger();
			}
		}

		public void Trigger()
		{
			_callback?.Invoke();
		}

		private void OnApplicationQuit()
		{
			_enabled = false;
		}
	}
}
