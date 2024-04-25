using System;
using System.Collections.Generic;

namespace BlueCheese.Unity.Core.FSM
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
        /// Gets the list of all states in the state machine.
        /// </summary>
        public IReadOnlyList<IState> States => new List<IState>(_states.Values);

        private readonly Dictionary<string, IState> _states = new Dictionary<string, IState>();
        private readonly Dictionary<IState, List<ITransition>> _transitions = new Dictionary<IState, List<ITransition>>();
        private readonly List<ITransition> _anyTransitions = new List<ITransition>();
        private readonly Dictionary<string, bool> _boolParameters = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _intParameters = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _floatParameters = new Dictionary<string, float>();
        private readonly HashSet<string> _triggers = new HashSet<string>();

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

        /// <summary>
        /// Gets the trigger state with the specified name.
        /// </summary>
        /// <param name="name">The name of the trigger state.</param>
        /// <returns>True if the trigger state is active, false otherwise.</returns>
        public bool GetTriggerState(string name) => _triggers.Contains(name);

        /// <summary>
        /// Gets the boolean value with the specified name.
        /// </summary>
        /// <param name="name">The name of the boolean value.</param>
        /// <returns>The boolean value with the specified name, or false if not found.</returns>
        public bool GetBoolValue(string name) => _boolParameters.ContainsKey(name) && _boolParameters[name];

        /// <summary>
        /// Gets the integer value with the specified name.
        /// </summary>
        /// <param name="name">The name of the integer value.</param>
        /// <returns>The integer value with the specified name, or 0 if not found.</returns>
        public int GetIntValue(string name) => _intParameters.ContainsKey(name) ? _intParameters[name] : 0;

        /// <summary>
        /// Gets the float value with the specified name.
        /// </summary>
        /// <param name="name">The name of the float value.</param>
        /// <returns>The float value with the specified name, or 0 if not found.</returns>
        public float GetFloatValue(string name) => _floatParameters.ContainsKey(name) ? _floatParameters[name] : 0;

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

            IState nextState = null;
            float overTime = 0f;
            foreach (var transition in transitions)
            {
                if (transition.Evaluate(this, out nextState, out overTime))
                {
                    break;
                }
            }

            ResetTriggers();
            SetState(nextState, overTime);
        }

        private void SetState(IState nextState, float nextStateTime = 0f)
        {
            if (!IsStarted || nextState == null)
            {
                return;
            }

            CurrentState?.OnExit();
            CurrentState = nextState;
            StateTime = 0f;
            CurrentState.OnEnter();

            Update(nextStateTime);
        }

        private void ResetTriggers() => _triggers.Clear();

        /// <summary>
        /// Sets the specified trigger state.
        /// </summary>
        /// <param name="name">The name of the trigger state.</param>
        public void SetTrigger(string name) => _triggers.Add(name);

        /// <summary>
        /// Sets the specified boolean value.
        /// </summary>
        /// <param name="name">The name of the boolean value.</param>
        /// <param name="value">The value to set.</param>
        public void SetBool(string name, bool value) => _boolParameters[name] = value;

        /// <summary>
        /// Sets the specified integer value.
        /// </summary>
        /// <param name="name">The name of the integer value.</param>
        /// <param name="value">The value to set.</param>
        public void SetInt(string name, int value) => _intParameters[name] = value;

        /// <summary>
        /// Sets the specified float value.
        /// </summary>
        /// <param name="name">The name of the float value.</param>
        /// <param name="value">The value to set.</param>
        public void SetFloat(string name, float value) => _floatParameters[name] = value;

        private void Reset()
        {
            foreach (var state in _states.Values)
            {
                state.Dispose();
            }
            _states.Clear();
            _transitions.Clear();
            _anyTransitions.Clear();
            _triggers.Clear();
            _boolParameters.Clear();
            _intParameters.Clear();
            _floatParameters.Clear();
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
                _stateMachine.SetBool(parameterName, defaultValue);
                return this;
            }

            public Builder AddIntParameter(string parameterName, int defaultValue = default)
            {
                _stateMachine.SetInt(parameterName, defaultValue);
                return this;
            }

            public Builder AddFloatParameter(string parameterName, float defaultValue = default)
            {
                _stateMachine.SetFloat(parameterName, defaultValue);
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
