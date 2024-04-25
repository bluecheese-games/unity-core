using System;
using BlueCheese.Unity.Core.FSM.Graph;
using UnityEngine;
using UnityEngine.Events;

namespace BlueCheese.Unity.Core.FSM.Mono
{
    public enum UpdateMode
    {
        Normal,
        Physics,
        UnscaledTime,
    }

    [Serializable]
    public class EnterStateEvent : UnityEvent<IState> { }

    [Serializable]
    public class UpdateStateEvent : UnityEvent<IState, float> { }

    [Serializable]
    public class ExitStateEvent : UnityEvent<IState> { }

    public class StateMachineController : MonoBehaviour
    {
        [SerializeField] private FSMGraphAsset _graph;
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private UpdateMode _updateMode = UpdateMode.Normal;

        [Space]
        [SerializeField] private EnterStateEvent _onEnterState;
        [SerializeField] private UpdateStateEvent _onUpdateState;
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

            foreach (var iState in _stateMachine.States)
            {
                var state = (State)iState;
                state.OnEnterState += _onEnterState.Invoke;
                state.OnUpdateState += _onUpdateState.Invoke;
                state.OnExitState += _onExitState.Invoke;
            }
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
            if (_stateMachine == null || _updateMode == UpdateMode.Physics)
            {
                return;
            }

            _stateMachine.Update(_updateMode == UpdateMode.Normal ? Time.deltaTime : Time.unscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            if (_stateMachine == null || _updateMode != UpdateMode.Physics)
            {
                return;
            }

            _stateMachine.Update(Time.fixedDeltaTime);
        }
    }
}
