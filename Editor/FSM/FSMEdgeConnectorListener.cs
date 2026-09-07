using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BlueCheese.Core.FSM.Editor
{
    /// <summary>
    /// Custom edge connector listener that:
    /// • Replicates the default OnDrop behaviour (fires graphViewChanged).
    /// • Shows a "Create State" menu when the user drops on empty space.
    /// </summary>
    public class FSMEdgeConnectorListener : IEdgeConnectorListener
    {
        private readonly FSMGraphView       _graphView;
        private readonly List<Edge>         _edgesToCreate = new();
        private readonly List<GraphElement> _edgesToDelete = new();

        public FSMEdgeConnectorListener(FSMGraphView graphView)
        {
            _graphView = graphView;
        }

        // ── Standard drop on a port ──────────────────────────────────────────

        public void OnDrop(GraphView graphView, Edge edge)
        {
            _edgesToCreate.Clear();
            _edgesToCreate.Add(edge);
            _edgesToDelete.Clear();

            // Replace any existing connections on Single-capacity ports
            if (edge.input != null && edge.input.capacity == Port.Capacity.Single)
                foreach (var e in edge.input.connections)
                    if (e != edge) _edgesToDelete.Add(e);

            if (edge.output != null && edge.output.capacity == Port.Capacity.Single)
                foreach (var e in edge.output.connections)
                    if (e != edge) _edgesToDelete.Add(e);

            var change = new GraphViewChange
            {
                edgesToCreate    = _edgesToCreate,
                elementsToRemove = _edgesToDelete
            };

            if (graphView.graphViewChanged != null)
                change = graphView.graphViewChanged(change);

            foreach (var del in _edgesToDelete)
                graphView.RemoveElement(del);

            if (change.edgesToCreate != null)
                foreach (var e in change.edgesToCreate)
                    graphView.AddElement(e);
        }

        // ── Drop on empty space ──────────────────────────────────────────────

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            // Capture the source port NOW — Unity sets edge.output = null during
            // its post-drop cleanup, which happens before the GenericMenu callback fires.
            var sourcePort = edge.output;
            if (sourcePort == null) return;

            // Convert GraphView-local position to content space (zoom + pan aware)
            var p = _graphView.viewTransform.matrix.inverse
                        .MultiplyPoint3x4(new Vector3(position.x, position.y, 0f));
            var graphPos = new Vector2(p.x, p.y);

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("New State"), false, () =>
            {
                _graphView.CreateStateAndConnect(sourcePort, graphPos);
            });
            menu.ShowAsContext();
        }
    }
}
