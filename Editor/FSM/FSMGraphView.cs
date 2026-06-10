//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using System.Linq;
using BlueCheese.Core.FSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Core.FSM.Editor
{
    public class FSMGraphView : GraphView
    {
        private FSMGraphAsset _asset;
        private AnyStateNode  _anyStateNode;

        public FSMGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

            var grid = new GridBackground { name = "Grid" };
            grid.StretchToParentSize();
            Insert(0, grid);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/unity-core/Editor/FSM/Styles/GraphView.uss");
            if (styleSheet != null) styleSheets.Add(styleSheet);

            graphViewChanged += OnGraphViewChanged;
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void Load(FSMGraphAsset asset)
        {
            _asset = asset;

            // Unsubscribe during clear so that DeleteElements doesn't fire
            // OnGraphViewChanged and strip States/Transitions from the asset.
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            graphViewChanged += OnGraphViewChanged;

            // Any State node
            _anyStateNode = new AnyStateNode();
            _anyStateNode.Initialize(asset.ViewState.AnyStatePosition);
            _anyStateNode.SetGraphView(this);
            _anyStateNode.Draw();
            AddElement(_anyStateNode);

            // State nodes
            var nodeMap = new Dictionary<string, StateNode>();
            foreach (var state in asset.States)
            {
                var node = AddStateNode(state);
                nodeMap[state.Name] = node;
            }

            // Edges — each source node hands out one free port per transition
            foreach (var trans in asset.Transitions)
            {
                if (string.IsNullOrEmpty(trans.ToState)) continue;
                if (!nodeMap.TryGetValue(trans.ToState, out var toNode)) continue;

                BaseNode sourceNode = string.IsNullOrEmpty(trans.FromState)
                    ? (BaseNode)_anyStateNode
                    : nodeMap.TryGetValue(trans.FromState, out var fn) ? fn : null;
                if (sourceNode == null) continue;

                var outputPort = sourceNode.GetFreeOutputPort();

                var edge = new FSMEdge { GraphTransition = trans };
                edge.output = outputPort;
                edge.input  = toNode.InputPort;
                edge.output.Connect(edge);
                edge.input.Connect(edge);
                AddElement(edge);
                // GetFreeOutputPort already created this port; next call will create another
            }

            // After all edges are wired, ensure every source node still has a free port
            nodes.ForEach(n => { if (n is BaseNode bn) bn.EnsureFreeOutputPort(); });

            // Restore view transform
            viewTransform.position = asset.ViewState.ViewPosition;
            viewTransform.scale    = asset.ViewState.ViewScale;

            RefreshAllExitTimeIcons();
        }

        public void Save(FSMGraphAsset asset)
        {
            nodes.ForEach(n =>
            {
                if (n is StateNode sn)
                    sn.GraphState.Position = sn.GetPosition().position;
                else if (n is AnyStateNode any)
                    asset.ViewState.AnyStatePosition = any.GetPosition().position;
            });

            // Update FromState/ToState from current port connections (handles renames)
            edges.ForEach(e =>
            {
                if (e is not FSMEdge fsmEdge || fsmEdge.GraphTransition == null) return;
                var fromNode = fsmEdge.output?.node;
                var toNode   = fsmEdge.input?.node as StateNode;
                fsmEdge.GraphTransition.FromState = fromNode is AnyStateNode ? null
                    : (fromNode as StateNode)?.GraphState.Name;
                fsmEdge.GraphTransition.ToState = toNode?.GraphState.Name;
            });

            asset.ViewState.ViewPosition = viewTransform.position;
            asset.ViewState.ViewScale    = viewTransform.scale;
        }

        // ── Port compatibility ───────────────────────────────────────────────

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(p =>
                p != startPort &&
                p.node != startPort.node &&
                p.direction != startPort.direction &&
                !(p.node is AnyStateNode && p.direction == Direction.Input) &&
                !(p.capacity == Port.Capacity.Single && p.connected) // skip occupied single ports
            ).ToList();
        }

        // ── Exit time icons ──────────────────────────────────────────────────

        public void RefreshAllExitTimeIcons()
        {
            nodes.ForEach(n => { if (n is BaseNode bn) bn.RefreshExitTimeIcons(); });
        }

        // ── Default state management ─────────────────────────────────────────

        public void SetDefaultState(StateNode newDefault)
        {
            nodes.ForEach(n =>
            {
                if (n is StateNode sn && sn != newDefault && sn.GraphState.IsDefault)
                {
                    sn.GraphState.IsDefault = false;
                    sn.UpdateDefaultStyle();
                }
            });
        }

        // ── Create state from edge drop ──────────────────────────────────────

        /// <summary>
        /// Called by FSMEdgeConnectorListener when a dragged edge is dropped on
        /// empty space. Creates a new state node and completes the connection.
        /// </summary>
        public void CreateStateAndConnect(Port sourcePort, Vector2 graphPosition)
        {
            if (sourcePort == null || _asset == null) return;

            // Create the new state node
            var graphState = new FSMGraphAsset.GraphState
            {
                Name      = GenerateStateName(),
                IsDefault = _asset.States.Count == 0,
                Position  = graphPosition,
            };
            _asset.States.Add(graphState);
            var newNode = AddStateNode(graphState);

            // Build a fresh FSMEdge — never reuse the dirty candidate from OnDropOutsidePort
            var edge = new FSMEdge();
            edge.output = sourcePort;
            edge.input  = newNode.InputPort;
            sourcePort.Connect(edge);
            newNode.InputPort.Connect(edge);

            // Create and register the transition
            var fromNode = sourcePort.node;
            var trans = new FSMGraphAsset.GraphTransition
            {
                FromState  = fromNode is AnyStateNode ? null : (fromNode as StateNode)?.GraphState?.Name,
                ToState    = newNode.GraphState.Name,
                Conditions = new()
            };
            _asset.Transitions.Add(trans);
            edge.GraphTransition = trans;

            AddElement(edge);

            // Ensure the source node still has a free output port
            if (fromNode is BaseNode bn) bn.EnsureFreeOutputPort();
        }

        // ── Context menu ─────────────────────────────────────────────────────

        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (_asset == null) return;
            var mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendAction("Create State", _ => CreateNewState(mousePos));
        }

        private void CreateNewState(Vector2 position)
        {
            var graphState = new FSMGraphAsset.GraphState
            {
                Name      = GenerateStateName(),
                IsDefault = _asset.States.Count == 0,
                Position  = position,
            };
            _asset.States.Add(graphState);
            AddStateNode(graphState);
        }

        private string GenerateStateName()
        {
            var existing = new HashSet<string>(_asset.States.Select(s => s.Name));
            for (int i = 1; ; i++)
            {
                var name = $"State {i}";
                if (!existing.Contains(name)) return name;
            }
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private StateNode AddStateNode(FSMGraphAsset.GraphState state)
        {
            var node = new StateNode(state);
            node.Initialize(state.Position);
            node.SetGraphView(this); // must be before Draw() so ports get the right listener
            node.Draw();
            node.OnContentValueChange += () => graphViewChanged?.Invoke(default);
            AddElement(node);
            return node;
        }

        // ── Graph change handler ─────────────────────────────────────────────

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    if (edge is not FSMEdge fsmEdge) continue;

                    var trans    = new FSMGraphAsset.GraphTransition { Conditions = new() };
                    var fromNode = fsmEdge.output?.node;
                    var toNode   = fsmEdge.input?.node as StateNode;
                    trans.FromState = fromNode is AnyStateNode ? null
                        : (fromNode as StateNode)?.GraphState?.Name;
                    trans.ToState = toNode?.GraphState?.Name;

                    _asset?.Transitions.Add(trans);
                    fsmEdge.GraphTransition = trans;

                    // Add a fresh free port on the source node
                    if (fromNode is BaseNode bn) bn.EnsureFreeOutputPort();
                }
            }

            if (change.elementsToRemove != null)
            {
                var removedSet = new HashSet<GraphElement>(
                    change.elementsToRemove.OfType<GraphElement>());

                foreach (var elem in change.elementsToRemove)
                {
                    if (elem is StateNode sn)
                    {
                        _asset?.States.Remove(sn.GraphState);
                    }
                    else if (elem is FSMEdge fsmEdge)
                    {
                        _asset?.Transitions.Remove(fsmEdge.GraphTransition);

                        // Remove the dedicated port and restore a free one,
                        // but only if the source node itself is not being deleted.
                        var sourcePort = fsmEdge.output;
                        var sourceNode = sourcePort?.node as BaseNode;
                        if (sourceNode != null && !removedSet.Contains(sourceNode))
                        {
                            sourceNode.RemoveOutputPort(sourcePort);
                            sourceNode.EnsureFreeOutputPort();
                        }
                    }
                }
            }

            return change;
        }
    }
}
