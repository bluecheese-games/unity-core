//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Core.FSM.Graph
{
    public class StateNode : BaseNode
    {
        private TextField _nameInput;
        private RadioButton _defaultToggle;

        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        override public void Initialize(Vector2 position)
        {
            base.Initialize(position);

            Name = "State Name";
            IsDefault = false;
        }

        override public void Draw()
        {
            _nameInput = new TextField()
            {
                value = Name,
            };
            _nameInput.RegisterValueChangedCallback(OnNameValueChanged);
            titleContainer.Add(_nameInput);

            _defaultToggle = new RadioButton("Default")
            {
                value = IsDefault
            };
            _defaultToggle.RegisterValueChangedCallback(OnDefaultValueChanged);
            titleContainer.Add(_defaultToggle);

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(TransitionNode));
            InputPort.portName = "In";

            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(TransitionNode));
            OutputPort.portName = "Out";

            outputContainer.Add(OutputPort);

            RefreshExpandedState();
        }

        private void OnNameValueChanged(ChangeEvent<string> evt)
        {
            Name = evt.newValue;
            DispatchOnContentValueChangeEvent();
        }

        private void OnDefaultValueChanged(ChangeEvent<bool> evt)
        {
            IsDefault = evt.newValue;
            DispatchOnContentValueChangeEvent();
        }

        public void Redraw()
        {
            _nameInput.value = Name;
            _defaultToggle.value = IsDefault;

            RefreshExpandedState();
        }
    }
}
