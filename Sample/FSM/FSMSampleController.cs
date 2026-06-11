//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.FSM.Mono;
using TMPro;
using UnityEngine;

namespace BlueCheese.Core.FSM.Sample
{
    public class FSMSampleController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private StateMachineController _fsmController;

        private void Awake()
        {
            _text.text = string.Empty;
        }

        public void HandleEnterState(string state)
        {
            switch (state)
            {
                case "intro":
                    Log("Enter Intro State");
                    Log("-- wait 3 seconds --");
                    break;
                case "run":
                    Log("Enter Run State");
                    Log("-- click to continue --");
                    break;
                case "over":
                    Log("Enter Over State");
                    break;
            }
        }

        private void Log(string msg)
        {
            _text.text += msg + "\n";
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _fsmController.StateMachine.Blackboard.SetTrigger("click");
            }
        }
    }

    public class SampleStateHandler : IStateHandler
    {
        private string _stateName;

        public void Initialize(IStateContext context)
        {
            _stateName = context.StateName;
        }

        public void OnEnter()
        {
            Debug.Log($"Enter State: {_stateName}");
        }

        public void OnExit()
        {
            Debug.Log($"Exit State: {_stateName}");
        }

        public void OnUpdate(float deltaTime)
        {
        }
    }

	public class SpaceKeyPressed : IPredicate
	{
		public bool Evaluate(IStateContext context)
		{
            return Input.GetKeyDown(KeyCode.Space);
		}
	}

	public class KeyPressed : IPredicate
	{
		public KeyCode key = KeyCode.None;

		public bool Evaluate(IStateContext context)
			=> UnityEngine.Input.GetKey(key);
	}
}