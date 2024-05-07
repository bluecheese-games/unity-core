//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Unity.Core.FSM.Graph
{
    public class TransitionNode : BaseNode
    {
        private FloatField _exitTimeInput;

        public float ExitTime { get; set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            ExitTime = 0f;
        }

        override public void Draw()
        {
            _exitTimeInput = new FloatField("ExitTime");
            _exitTimeInput.RegisterValueChangedCallback(OnExitTimeValueChanged);
            titleContainer.Add(_exitTimeInput);

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(StateNode));
            InputPort.portName = "In";

            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(StateNode));
            OutputPort.portName = "Out";

            outputContainer.Add(OutputPort);

            RefreshExpandedState();
        }

        public StateNode FromNode => InputPort.connected ? InputPort.connections.First().output.node as StateNode : null;
        public StateNode ToNode => OutputPort.connected ? OutputPort.connections.First().input.node as StateNode : null;

        private void OnExitTimeValueChanged(ChangeEvent<float> evt)
        {
            ExitTime = evt.newValue;
            DispatchOnContentValueChangeEvent();
        }

        public void Redraw()
        {
            _exitTimeInput.value = ExitTime;

            RefreshExpandedState();
        }
    }
}
