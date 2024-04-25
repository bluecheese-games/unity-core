using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Unity.Core.FSM.Graph
{
    public class FSMGraphView : GraphView
    {
        private FSMGraphAsset _asset;

        public FSMGraphView()
        {
            AddManipulators();
            AddGridBackground();
            AddStyles();
        }

        private T CreateNode<T>(Vector2 position) where T : BaseNode
        {
            var node = Activator.CreateInstance<T>();

            node.Initialize(position);
            node.Draw();
            node.OnContentValueChange += OnNodeContentValueChange;

            AddElement(node);

            return node;
        }

        private void AddGridBackground()
        {
            GridBackground gridBackground = new GridBackground();

            gridBackground.StretchToParentSize();

            Insert(0, gridBackground);
        }

        private void AddStyles()
        {
            styleSheets.Add((StyleSheet)EditorGUIUtility.Load("FSM/GraphViewStyles.uss"));
            styleSheets.Add((StyleSheet)EditorGUIUtility.Load("FSM/GraphNodeStyles.uss"));
        }

        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            this.AddManipulator(CreateNodeContextualMenu());
        }

        private IManipulator CreateNodeContextualMenu()
        {
            var manipulator = new ContextualMenuManipulator(
                menuEvent =>
                {
                    menuEvent.menu.AppendAction("Create State", actionEvent => AddElement(CreateNode<StateNode>(actionEvent.eventInfo.localMousePosition)));
                    menuEvent.menu.AppendAction("Create Transition", actionEvent => AddElement(CreateNode<TransitionNode>(actionEvent.eventInfo.localMousePosition)));
                });

            return manipulator;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort == port) return;
                if (startPort.node == port.node) return;
                if (startPort.direction == port.direction) return;
                if (startPort.portType == port.portType) return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        private void OnNodeContentValueChange()
        {
            graphViewChanged?.Invoke(default);
        }

        public void Load(FSMGraphAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            _asset = asset;

            var stateNodes = new Dictionary<string, StateNode>();
            foreach (var state in _asset.States)
            {
                var node = CreateNode<StateNode>(state.Position);
                node.Name = state.Name;
                node.IsDefault = state.IsDefault;
                node.Redraw();
                stateNodes.Add(state.Name, node);
            }

            foreach (var transition in _asset.Transitions)
            {
                var node = CreateNode<TransitionNode>(transition.Position);
                node.ExitTime = transition.ExitTime;
                if (!string.IsNullOrEmpty(transition.FromState) && stateNodes.TryGetValue(transition.FromState, out var fromStateNode))
                {
                    fromStateNode.OutputPort.ConnectTo(node.InputPort);
                    Add(new Edge() { output = fromStateNode.OutputPort, input = node.InputPort });
                }
                if (!string.IsNullOrEmpty(transition.ToState) && stateNodes.TryGetValue(transition.ToState, out var toStateNode))
                {
                    node.OutputPort.ConnectTo(toStateNode.InputPort);
                    Add(new Edge() { output = node.OutputPort, input = toStateNode.InputPort });
                }

                node.Redraw();
            }
        }

        public void Save()
        {
            if (_asset == null)
            {
                return;
            }

            _asset.States.Clear();
            _asset.Transitions.Clear();
            nodes.ForEach(node =>
            {
                if (node is StateNode stateNode)
                {
                    _asset.States.Add(new FSMGraphAsset.GraphState()
                    {
                        Name = stateNode.Name,
                        IsDefault = stateNode.IsDefault,
                        Position = stateNode.GetPosition().position
                    });
                }
                else if (node is TransitionNode transitionNode)
                {
                    _asset.Transitions.Add(new FSMGraphAsset.GraphTransition()
                    {
                        FromState = transitionNode.FromNode?.Name,
                        ToState = transitionNode.ToNode?.Name,
                        ExitTime = transitionNode.ExitTime,
                        Position = transitionNode.GetPosition().position
                    });
                }
            });
        }
    }
}
