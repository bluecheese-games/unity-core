//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.FSM.Mono;
using TMPro;
using UnityEngine;

namespace BlueCheese.Core.FSM.Sample
{
    public class SampleController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private StateMachineController _fsmController;

        private void Awake()
        {
            _text.text = string.Empty;
        }

        public void HandleEnterState(IState state)
        {
            switch (state.Name)
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
}