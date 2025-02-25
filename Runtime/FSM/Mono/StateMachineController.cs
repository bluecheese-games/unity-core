//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using BlueCheese.Core.FSM.Graph;
using UnityEngine;
using UnityEngine.Events;

namespace BlueCheese.Core.FSM.Mono
{
	public enum UpdateMode
	{
		None, // Manual update
		DeltaTime, // Update
		UnscaledDeltaTime, // Update (unscaled)
		FixedDeltaTime, // FixedUpdate
	}

	[Serializable]
	public class EnterStateEvent : UnityEvent<string> { }

	[Serializable]
	public class ExitStateEvent : UnityEvent<string> { }

	public class StateMachineController : MonoBehaviour
	{
		[SerializeField] private FSMGraphAsset _graph;
		[SerializeField] private bool _autoStart = true;
		[SerializeField] private UpdateMode _updateMode = UpdateMode.DeltaTime;

		[Space]
		[SerializeField] private EnterStateEvent _onEnterState;
		[SerializeField] private ExitStateEvent _onExitState;

		private StateMachine _stateMachine;

		public StateMachine StateMachine => _stateMachine;

		private void Awake()
		{
			Load(_graph);
		}

		public void Load(FSMGraphAsset graph)
		{
			if (graph == null)
			{
				return;
			}

			_stateMachine = graph.ToStateMachine();
			_stateMachine.OnEnterState += _onEnterState.Invoke;
			_stateMachine.OnExitState += _onExitState.Invoke;
		}

		public IEnumerable<string> GetStateNames()
		{
			if (_graph == null)
			{
				return Enumerable.Empty<string>();
			}
			return _graph.States.Select(s => s.Name);
		}

		private void Start()
		{
			if (_autoStart && _stateMachine != null)
			{
				_stateMachine.Start();
			}
		}

		private void Update()
		{
			if (_stateMachine == null || (_updateMode != UpdateMode.FixedDeltaTime && _updateMode != UpdateMode.DeltaTime))
			{
				return;
			}

			_stateMachine.Update(_updateMode == UpdateMode.DeltaTime ? Time.deltaTime : Time.unscaledDeltaTime);
		}

		private void FixedUpdate()
		{
			if (_stateMachine == null || _updateMode != UpdateMode.FixedDeltaTime)
			{
				return;
			}

			_stateMachine.Update(Time.fixedDeltaTime);
		}
	}
}
