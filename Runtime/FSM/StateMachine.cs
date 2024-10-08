//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;

namespace BlueCheese.Core.FSM
{
	/// <summary>
	/// Represents a state machine that manages the transitions between different states.
	/// </summary>
	public class StateMachine : IStateMachine
	{
		/// <summary>
		/// Gets the current state of the state machine.
		/// </summary>
		public IState CurrentState { get; private set; } = null;

		/// <summary>
		/// Gets the default state of the state machine.
		/// </summary>
		public IState DefaultState { get; private set; } = null;

		/// <summary>
		/// Gets the time spent in the current state.
		/// </summary>
		public float StateTime { get; private set; } = 0f;

		/// <summary>
		/// Gets a value indicating whether the state machine is started.
		/// </summary>
		public bool IsStarted { get; private set; } = false;

		/// <summary>
		/// Gets all states in the state machine.
		/// </summary>
		public IEnumerable<IState> States => _states.Values;

		public IBlackboard Blackboard => _blackboard;

		public event Action<IState> OnEnterState;
		public event Action<IState> OnExitState;

		private readonly Dictionary<string, IState> _states = new();
		private readonly Dictionary<IState, List<ITransition>> _transitions = new();
		private readonly List<ITransition> _anyTransitions = new();
		private readonly IBlackboard _blackboard = new Blackboard();

		private StateMachine() { }

		/// <summary>
		/// Gets the state with the specified name.
		/// </summary>
		/// <param name="name">The name of the state.</param>
		/// <returns>The state with the specified name, or null if not found.</returns>
		public IState GetState(string name) => _states.ContainsKey(name) ? _states[name] : null;

		/// <summary>
		/// Gets the state with the specified name and type.
		/// </summary>
		/// <param name="name">The name of the state.</param>
		/// <returns>The state with the specified name and type, or null if not found.</returns>
		public T GetState<T>(string name) where T : IState => (T)GetState(name);

		private void AddState(IState state, bool isDefault = false)
		{
			if (IsStarted)
			{
				throw new InvalidOperationException("Cannot add state: StateMachine is already started");
			}

			if (_states.ContainsKey(state.Name))
			{
				throw new InvalidOperationException("Cannot add state: state already exists with this name");
			}

			_states.Add(state.Name, state);

			if (DefaultState == null || isDefault)
			{
				DefaultState = state;
			}
		}

		private ITransition AddTransitionFromAnyState(string toStateName, params ICondition[] conditions)
		{
			IState toState = GetState(toStateName)
				?? throw new InvalidOperationException($"Cannot add transition: To State not found ({toStateName})");

			if (conditions == null || conditions.Length == 0)
			{
				throw new InvalidOperationException($"Cannot add transition: From any state transition must have at least one condition (to state {toStateName})");
			}

			var transition = new Transition(toState, 0f, conditions);
			_anyTransitions.Add(transition);
			return transition;
		}

		private ITransition AddTransition(string fromStateName, string toStateName, float exitTime = 0f, params ICondition[] conditions)
		{
			IState fromState = GetState(fromStateName)
				?? throw new InvalidOperationException($"Cannot add transition: From State not found ({fromStateName})");
			IState toState = GetState(toStateName)
				?? throw new InvalidOperationException($"Cannot add transition: To State not found ({toStateName})");

			var transition = new Transition(toState, exitTime, conditions);
			if (_transitions.ContainsKey(fromState))
			{
				foreach (var trans in _transitions[fromState])
				{
					if (trans.NextState == toState)
					{
						throw new InvalidOperationException($"Cannot add transition: This transition already exists ({fromStateName} => {toStateName})");
					}
				}

				_transitions[fromState].Add(transition);
			}
			else
			{
				_transitions.Add(fromState, new List<ITransition> { transition });
			}
			return transition;
		}

		/// <summary>
		/// Starts the state machine.
		/// </summary>
		public void Start()
		{
			if (IsStarted)
			{
				throw new InvalidOperationException("Cannot start StateMachine: StateMachine is already started");
			}

			if (DefaultState == null)
			{
				throw new InvalidOperationException("Cannot start StateMachine: StateMachine has no state");
			}

			IsStarted = true;

			SetState(DefaultState);
		}

		/// <summary>
		/// Updates the state machine.
		/// </summary>
		/// <param name="deltaTime">The time since the last update.</param>
		public void Update(float deltaTime)
		{
			if (!IsStarted)
			{
				return;
			}

			StateTime += deltaTime;

			CurrentState.OnUpdate(deltaTime);

			TryChangeState();
		}

		/// <summary>
		/// Sets the state of the state machine.
		/// </summary>
		/// <param name="stateName">The name of the state to set.</param>
		/// <param name="stateTime">The time spent in the state.</param>
		public void SetState(string stateName, float stateTime = 0f) => SetState(GetState(stateName), stateTime);

		private void TryChangeState()
		{
			if (!IsStarted || CurrentState == null)
			{
				return;
			}

			if (!_transitions.TryGetValue(CurrentState, out var transitions))
			{
				if (_anyTransitions.Count > 0)
				{
					transitions = _anyTransitions;
				}
				else
				{
					return;
				}
			}

			foreach (var transition in transitions)
			{
				if (transition.Evaluate(StateTime, Blackboard, out IState nextState, out float overTime))
				{
					SetState(nextState, overTime);
					break;
				}
			}
		}

		private void SetState(IState nextState, float nextStateTime = 0f)
		{
			if (!IsStarted || nextState == null)
			{
				return;
			}

			// Exit previous state
			CurrentState?.OnExit();
			OnExitState?.Invoke(CurrentState);

			// Switch current state
			CurrentState = nextState;
			StateTime = 0f;
			Blackboard.ResetTriggers();

			// Enter new state
			CurrentState.OnEnter();
			OnEnterState?.Invoke(CurrentState);

			Update(nextStateTime);
		}

		private void Reset()
		{
			_states.Clear();
			_transitions.Clear();
			_anyTransitions.Clear();
			_blackboard.Reset();
			CurrentState = null;
			DefaultState = null;
			StateTime = 0f;
			IsStarted = false;
		}

		/// <summary>
		/// Disposes the state machine and resets its state.
		/// </summary>
		public void Dispose() => Reset();

		public class Builder
		{
			private readonly StateMachine _stateMachine = new StateMachine();

			/// <summary>
			/// Adds a state to the state machine.
			/// </summary>
			/// <param name="state">The state to add.</param>
			/// <param name="isDefault">Whether the state should be set as the default state.</param>
			/// <returns>The builder instance.</returns>
			public Builder AddState(IState state, bool isDefault = false)
			{
				_stateMachine.AddState(state, isDefault);
				return this;
			}

			/// <summary>
			/// Adds a transition between two states in the state machine.
			/// </summary>
			/// <param name="fromStateName">The name of the state to transition from.</param>
			/// <param name="toStateName">The name of the state to transition to.</param>
			/// <param name="exitTime">The time it takes to exit the current state.</param>
			/// <param name="conditions">The conditions that must be met for the transition to occur.</param>
			/// <returns>The builder instance.</returns>
			public Builder AddTransition(string fromStateName, string toStateName, float exitTime = 0f, params ICondition[] conditions)
			{
				_stateMachine.AddTransition(fromStateName, toStateName, exitTime, conditions);
				return this;
			}

			/// <summary>
			/// Adds a transition between two states in the state machine and returns the created transition.
			/// </summary>
			/// <param name="fromStateName">The name of the state to transition from.</param>
			/// <param name="toStateName">The name of the state to transition to.</param>
			/// <param name="transition">The created transition.</param>
			/// <param name="exitTime">The time it takes to exit the current state.</param>
			/// <param name="conditions">The conditions that must be met for the transition to occur.</param>
			/// <returns>The builder instance.</returns>
			public Builder AddTransition(string fromStateName, string toStateName, out ITransition transition, float exitTime = 0f, params ICondition[] conditions)
			{
				transition = _stateMachine.AddTransition(fromStateName, toStateName, exitTime, conditions);
				return this;
			}

			/// <summary>
			/// Adds a transition from any state to a specific state in the state machine.
			/// </summary>
			/// <param name="toStateName">The name of the state to transition to.</param>
			/// <param name="conditions">The conditions that must be met for the transition to occur.</param>
			/// <returns>The builder instance.</returns>
			public Builder AddTransitionFromAnyState(string toStateName, params ICondition[] conditions)
			{
				_stateMachine.AddTransitionFromAnyState(toStateName, conditions);
				return this;
			}

			/// <summary>
			/// Adds a transition from any state to a specific state in the state machine and returns the created transition.
			/// </summary>
			/// <param name="toStateName">The name of the state to transition to.</param>
			/// <param name="transition">The created transition.</param>
			/// <param name="conditions">The conditions that must be met for the transition to occur.</param>
			/// <returns>The builder instance.</returns>
			public Builder AddTransitionFromAnyState(string toStateName, out ITransition transition, params ICondition[] conditions)
			{
				transition = _stateMachine.AddTransitionFromAnyState(toStateName, conditions);
				return this;
			}

			public Builder AddBoolParameter(string parameterName, bool defaultValue = default)
			{
				_stateMachine.Blackboard.SetBool(parameterName, defaultValue);
				return this;
			}

			public Builder AddIntParameter(string parameterName, int defaultValue = default)
			{
				_stateMachine.Blackboard.SetInt(parameterName, defaultValue);
				return this;
			}

			public Builder AddFloatParameter(string parameterName, float defaultValue = default)
			{
				_stateMachine.Blackboard.SetFloat(parameterName, defaultValue);
				return this;
			}

			/// <summary>
			/// Builds the state machine.
			/// </summary>
			/// <returns>The built state machine.</returns>
			public StateMachine Build()
			{
				if (_stateMachine.DefaultState == null)
				{
					throw new InvalidOperationException("Cannot build StateMachine: StateMachine has no state");
				}

				return _stateMachine;
			}
		}
	}
}
