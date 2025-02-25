//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using NaughtyAttributes;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace BlueCheese.Core.FSM.Mono
{
	public class StateBehaviour : MonoBehaviour
	{
		[SerializeField] private StateMachineController _stateMachineController;
		[SerializeField, Dropdown(nameof(GetStateNames))] private string _stateName;
		[Header("Behaviour")]
		[SerializeField] private Behaviour _initialBehaviour = Behaviour.Hide;
		[SerializeField] private Behaviour _onEnterBehaviour = Behaviour.Show;
		[ShowIf(nameof(_onEnterBehaviour), Behaviour.Event)]
		[SerializeField] private UnityEvent _onEnter;
		[SerializeField] private Behaviour _onExitBehaviour = Behaviour.Hide;
		[ShowIf(nameof(_onExitBehaviour), Behaviour.Event)]
		[SerializeField] private UnityEvent _onExit;

		private void Start()
		{
			ApplyBehaviour(_initialBehaviour, null);
			SubscriveToStateMachine();
		}

		private void SubscriveToStateMachine()
		{
			if (_stateMachineController == null || _stateMachineController.StateMachine == null)
			{
				return;
			}

			_stateMachineController.StateMachine.OnEnterState += HandleEnterState;
			_stateMachineController.StateMachine.OnExitState += HandleExitState;

			if (_stateMachineController.StateMachine.IsStarted)
			{
				HandleEnterState(_stateMachineController.StateMachine.CurrentState);
			}
		}

		private void UnsubscribeFromStateMachine()
		{
			if (_stateMachineController == null)
			{
				return;
			}

			_stateMachineController.StateMachine.OnEnterState -= HandleEnterState;
			_stateMachineController.StateMachine.OnExitState -= HandleExitState;
		}

		private void HandleEnterState(string state)
		{
			if (state == _stateName)
			{
				ApplyBehaviour(_onEnterBehaviour, _onEnter);
			}
		}

		private void HandleExitState(string state)
		{
			if (state == _stateName)
			{
				ApplyBehaviour(_onExitBehaviour, _onExit);
			}
		}

		private void ApplyBehaviour(Behaviour behaviour, UnityEvent unityEvent)
		{
			switch (behaviour)
			{
				case Behaviour.Show: Show(); break;
				case Behaviour.Hide: Hide(); break;
			}

			unityEvent?.Invoke();
		}

		protected virtual void Show()
		{
			gameObject.SetActive(true);
		}

		protected virtual void Hide()
		{
			gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			UnsubscribeFromStateMachine();
		}

		private string[] GetStateNames()
		{
			if (_stateMachineController == null)
			{
				return new string[] { "" };
			}

			return _stateMachineController.GetStateNames().ToArray();
		}

		[Serializable]
		public enum Behaviour
		{
			Ignore,
			Show,
			Hide,
			Event
		}
	}
}
