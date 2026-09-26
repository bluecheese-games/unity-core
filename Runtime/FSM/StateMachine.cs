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
		public string CurrentState { get; private set; } = null;

		/// <summary>
		/// Gets the default state of the state machine.
		/// </summary>
		public string DefaultState { get; private set; } = null;

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
		public ICollection<string> States => _stateHandlers.Keys;

		public IBlackboard Blackboard => _blackboard;

		public event Action<string> OnEnterState;
		public event Action<string> OnExitState;

		private readonly Dictionary<string, CompositeStateHandler> _stateHandlers = new();
		private readonly Dictionary<string, List<ITransition>> _transitions = new();
		private readonly List<ITransition> _anyTransitions = new();
		private readonly IBlackboard _blackboard = new Blackboard();

		private StateMachine() { }

		/// <summary>
		/// Gets the composite state handler with the specified name.
		/// </summary>
		/// <param name="name">The name of the state.</param>
		/// <returns>The state with the specified name, or null if not found.</returns>
		public CompositeStateHandler GetStateHandler(string name) => _stateHandlers.ContainsKey(name) ? _stateHandlers[name] : null;

		private void AddState(string state, IStateHandler handler = null, bool isDefault = false)
		{
			if (IsStarted)
			{
				throw new InvalidOperationException("Cannot add state: StateMachine is already started");
			}

			if (_stateHandlers.ContainsKey(state))
			{
				throw new InvalidOperationException("Cannot add state: state already exists with this name");
			}

			_stateHandlers.Add(state, new(handler));

			if (DefaultState == null || isDefault)
			{
				DefaultState = state;
			}
		}

		private ITransition AddTransitionFromAnyState(string toState, params ICondition[] conditions)
		{
			if (!States.Contains(toState))
				throw new InvalidOperationException($"Cannot add transition: To State not found ({toState})");

			if (conditions == null || conditions.Length == 0)
			{
				throw new InvalidOperationException($"Cannot add transition: From any state transition must have at least one condition (to state {toState})");
			}

			var transition = new Transition(toState, 0f, conditions);
			_anyTransitions.Add(transition);
			return transition;
		}

		private ITransition AddTransition(string fromState, string toState, float exitTime = 0f, params ICondition[] conditions)
		{
			if (!States.Contains(fromState))
				throw new InvalidOperationException($"Cannot add transition: From State not found ({fromState})");

			if (!States.Contains(toState))
				throw new InvalidOperationException($"Cannot add transition: To State not found ({toState})");

			var transition = new Transition(toState, exitTime, conditions);
			if (_transitions.ContainsKey(fromState))
			{
				foreach (var trans in _transitions[fromState])
				{
					if (trans.NextState == toState)
					{
						throw new InvalidOperationException($"Cannot add transition: This transition already exists ({fromState} => {toState})");
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
		/// Starts the state machine, entering <paramref name="initialState"/> (or <see cref="DefaultState"/>
		/// when null) — useful for a caller that wants to skip straight to a known state instead of always
		/// replaying the default one (e.g. an app already sitting in the scene that state maps to).
		/// </summary>
		public void Start(string initialState = null)
		{
			if (IsStarted)
			{
				throw new InvalidOperationException("Cannot start StateMachine: StateMachine is already started");
			}

			if (DefaultState == null)
			{
				throw new InvalidOperationException("Cannot start StateMachine: StateMachine has no state");
			}

			if (initialState != null && !_stateHandlers.ContainsKey(initialState))
			{
				throw new InvalidOperationException($"Cannot start StateMachine: state not found ({initialState})");
			}

			IsStarted = true;

			// Inject context into every handler before any state is entered
			foreach (var (stateName, composite) in _stateHandlers)
			{
				IStateContext ctx = new StateContext(stateName, _blackboard);
				composite.Initialize(ctx);
			}

			SetState(initialState ?? DefaultState);
		}

		/// <summary>Lightweight context passed to state handlers on start.</summary>
		private sealed class StateContext : IStateContext
		{
			public string StateName  { get; }
			public IBlackboard Blackboard { get; }
			public StateContext(string stateName, IBlackboard blackboard)
			{ StateName = stateName; Blackboard = blackboard; }
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

			if (_stateHandlers.TryGetValue(CurrentState, out var handler))
			{
				handler.OnUpdate(deltaTime);
			}

			TryChangeState();
		}

		private void TryChangeState()
		{
			if (!IsStarted || CurrentState == null)
			{
				return;
			}

			if (_transitions.TryGetValue(CurrentState, out var transitions))
			{
				foreach (var transition in transitions)
				{
					if (transition.Evaluate(StateTime, Blackboard, out string nextState, out float overTime))
					{
						SetState(nextState, overTime);
						break;
					}
				}
			}

			if (_anyTransitions.Count > 0)
			{
				foreach (var transition in _anyTransitions)
				{
					// Skip if we're already in the target state — avoids infinite self-transitions.
					if (transition.NextState == CurrentState) continue;

					if (transition.Evaluate(StateTime, Blackboard, out string nextState, out float overTime))
					{
						SetState(nextState, overTime);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Sets the state of the state machine.
		/// </summary>
		/// <param name="stateName">The name of the state to set.</param>
		/// <param name="stateTime">The time spent in the state.</param>
		public void SetState(string nextState, float nextStateTime = 0f)
		{
			if (!IsStarted || nextState == null)
			{
				return;
			}

			// Exit previous state
			if (CurrentState != null && _stateHandlers.TryGetValue(CurrentState, out var handler))
			{
				handler.OnExit();
			}
			OnExitState?.Invoke(CurrentState);

			// Switch current state
			CurrentState = nextState;
			StateTime = 0f;
			Blackboard.ResetTriggers();

			// Enter new state
			if (_stateHandlers.TryGetValue(CurrentState, out handler))
			{
				handler.OnEnter();
			}
			OnEnterState?.Invoke(CurrentState);

			Update(nextStateTime);
		}

		private void Reset()
		{
			_stateHandlers.Clear();
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
			/// Adds states to the state machine from an enum. States are added handler-less — use
			/// <see cref="FromEnum{TEnum}(Func{TEnum, IStateHandler})"/> to attach a handler to each state
			/// inline, or <see cref="GetStateHandler"/> after <see cref="Build"/> otherwise.
			/// </summary>
			public Builder FromEnum<TEnum>() where TEnum : Enum
			{
				foreach (var state in Enum.GetNames(typeof(TEnum)))
				{
					_stateMachine.AddState(state);
				}
				return this;
			}

			/// <summary>
			/// Adds states to the state machine from an enum, resolving each state's handler via
			/// <paramref name="handlerFactory"/> (return null for a handler-less state). The first enum
			/// value becomes the default state, matching <see cref="FromEnum{TEnum}()"/>.
			/// </summary>
			public Builder FromEnum<TEnum>(Func<TEnum, IStateHandler> handlerFactory) where TEnum : Enum
			{
				foreach (TEnum state in Enum.GetValues(typeof(TEnum)))
				{
					_stateMachine.AddState(state.ToString(), handlerFactory?.Invoke(state));
				}
				return this;
			}

			/// <summary>
			/// Adds a state to the state machine.
			/// </summary>
			/// <param name="state">The state to add.</param>
			/// <param name="isDefault">Whether the state should be set as the default state.</param>
			/// <returns>The builder instance.</returns>
			public Builder AddState(string state, bool isDefault = false)
			{
				_stateMachine.AddState(state, null, isDefault);
				return this;
			}

			/// <summary>
			/// Adds a state to the state machine, keyed by an enum value instead of a raw string.
			/// </summary>
			/// <param name="state">The enum value naming the state.</param>
			/// <param name="handler">The handler for the state.</param>
			/// <param name="isDefault">Whether the state should be set as the default state.</param>
			/// <returns>The builder instance.</returns>
			public Builder AddState<TEnum>(TEnum state, IStateHandler handler, bool isDefault = false) where TEnum : Enum
			{
				_stateMachine.AddState(state.ToString(), handler, isDefault);
				return this;
			}

			/// <summary>
			/// Adds a state to the state machine.
			/// </summary>
			/// <param name="state">The state to add.</param>
			/// <param name="stateHandler">The handler for the state.</param>
			/// <param name="isDefault">Whether the state should be set as the default state.</param>
			/// <returns>The builder instance.</returns>
			public Builder AddState(string state, IStateHandler stateHandler, bool isDefault = false)
			{
				_stateMachine.AddState(state, stateHandler, isDefault);
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
