//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using BlueCheese.Core.FSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Core.FSM.Editor
{
    public class StateNode : BaseNode
    {
        public FSMGraphAsset.GraphState GraphState { get; private set; }

        public string StateName => GraphState.Name;
        public bool IsDefault
        {
            get => GraphState.IsDefault;
            set => GraphState.IsDefault = value;
        }

        public Port InputPort { get; private set; }

        public event Action<StateNode> OnRenamed;

        public StateNode(FSMGraphAsset.GraphState graphState)
        {
            GraphState = graphState;
        }

        public override void Draw()
        {
            title = GraphState.Name;
            AddToClassList("state-node");

            InputPort = Port.Create<FSMEdge>(
                Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "";
            inputContainer.Add(InputPort);

            AddOutputPort(); // one free output port to start

            RefreshExpandedState();
            UpdateDefaultStyle();

            // Register double-click rename after the title Label is in the hierarchy
            schedule.Execute(RegisterTitleDoubleClick).StartingIn(0);
        }

        private void RegisterTitleDoubleClick()
        {
            var titleLabel = titleContainer.Q<Label>();
            var target = titleLabel ?? (VisualElement)titleContainer;
            target.RegisterCallback<MouseDownEvent>(OnTitleMouseDown, TrickleDown.TrickleDown);
        }

        public void RefreshTitle()
        {
            title = GraphState.Name;
            UpdateDefaultStyle();
        }

        public void UpdateDefaultStyle()
        {
            EnableInClassList("state-node--default", GraphState.IsDefault);

            var borderColor = GraphState.IsDefault
                ? new Color(0.95f, 0.78f, 0.10f)
                : new Color(0.23f, 0.48f, 0.84f);

            style.borderTopColor    = borderColor;
            style.borderBottomColor = borderColor;
            style.borderLeftColor   = borderColor;
            style.borderRightColor  = borderColor;
            style.borderTopWidth    = GraphState.IsDefault ? 2f : 1f;
            style.borderBottomWidth = GraphState.IsDefault ? 2f : 1f;
            style.borderLeftWidth   = GraphState.IsDefault ? 2f : 1f;
            style.borderRightWidth  = GraphState.IsDefault ? 2f : 1f;
        }

        public override void OnSelected()   { base.OnSelected();   AddToClassList("state-node--selected"); }
        public override void OnUnselected() { base.OnUnselected(); RemoveFromClassList("state-node--selected"); }

        // ── Inline rename ────────────────────────────────────────────────────

        private void OnTitleMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount != 2 || evt.button != 0) return;
            evt.StopImmediatePropagation();
            StartRename();
        }

        private void StartRename()
        {
            if (titleContainer.Q<TextField>() != null) return;

            var textField = new TextField { value = GraphState.Name };
            textField.AddToClassList("state-node__rename-field");
            titleContainer.Add(textField);

            schedule.Execute(() => { textField.Focus(); textField.SelectAll(); }).StartingIn(1);

            bool committed = false;

            void Commit()
            {
                if (committed) return;
                committed = true;
                var newName = textField.value.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != GraphState.Name)
                {
                    GraphState.Name = newName;
                    title = newName;
                    OnRenamed?.Invoke(this);
                    DispatchOnContentValueChangeEvent();
                }
                textField.RemoveFromHierarchy();
            }

            void Cancel()
            {
                if (committed) return;
                committed = true;
                textField.RemoveFromHierarchy();
            }

            textField.RegisterCallback<FocusOutEvent>(_ => Commit());
            textField.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter) { Commit(); e.StopPropagation(); }
                else if (e.keyCode == KeyCode.Escape)                   { Cancel(); e.StopPropagation(); }
            });
        }
    }
}
