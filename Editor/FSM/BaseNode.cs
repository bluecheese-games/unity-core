//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Core.FSM.Editor
{
    public abstract class BaseNode : Node
    {
        public event Action OnContentValueChange;

        // Set by FSMGraphView before Draw() so output ports get the right listener
        private FSMGraphView _graphView;
        public void SetGraphView(FSMGraphView gv) => _graphView = gv;

        // ── Output port management ────────────────────────────────────────────

        private readonly List<Port> _outputPorts = new();
        public IReadOnlyList<Port> OutputPorts => _outputPorts;

        public virtual void Initialize(Vector2 position)
        {
            SetPosition(new Rect(position, Vector2.zero));
        }

        public virtual void Draw() { }

        public void DispatchOnContentValueChangeEvent() => OnContentValueChange?.Invoke();

        /// <summary>Add a new free output port with a (hidden) ⏱ icon.</summary>
        public Port AddOutputPort()
        {
            IEdgeConnectorListener listener = _graphView != null
                ? new FSMEdgeConnectorListener(_graphView)
                : null;

            Port port;
            if (listener != null)
                port = FSMPort.CreateOutput(listener);
            else
            {
                port = Port.Create<FSMEdge>(
                    Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            }
            port.portName = "";

            var icon = new Label();
            icon.name = "exit-time-icon";
            icon.style.display   = DisplayStyle.None;
            icon.style.alignSelf = Align.Center;
            icon.style.marginLeft = 2;
            icon.style.color = new StyleColor(new Color(1f, 0.93f, 0.53f));
            port.Add(icon);

            outputContainer.Add(port);
            _outputPorts.Add(port);
            RefreshExpandedState(); // redraw the node so the new port appears immediately
            return port;
        }

        /// <summary>
        /// Returns the first unconnected output port.
        /// Automatically creates one if all ports are in use.
        /// </summary>
        public Port GetFreeOutputPort()
        {
            var free = _outputPorts.FirstOrDefault(p => !p.connected);
            if (free == null) free = AddOutputPort();
            return free;
        }

        /// <summary>Ensures at least one unconnected output port exists.</summary>
        public void EnsureFreeOutputPort()
        {
            if (!_outputPorts.Any(p => !p.connected))
                AddOutputPort();
        }

        /// <summary>Remove a specific output port (e.g. when its edge is deleted).</summary>
        public void RemoveOutputPort(Port port)
        {
            if (!_outputPorts.Contains(port)) return;
            outputContainer.Remove(port);
            _outputPorts.Remove(port);
        }

        /// <summary>Refresh the ⏱ icon on every output port based on its connected edge.</summary>
        public void RefreshExitTimeIcons()
        {
            foreach (var port in _outputPorts)
            {
                var icon = port.Q<Label>("exit-time-icon");
                if (icon == null) continue;
                var edge = port.connections.OfType<FSMEdge>().FirstOrDefault();
                var gt   = edge?.GraphTransition;
                bool show = gt?.UseExitTime == true && gt?.ExitTime > 0f;
                icon.text  = show ? $"{gt.ExitTime:F2}s" : string.Empty;
                icon.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
