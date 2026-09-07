using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace BlueCheese.Core.FSM.Editor
{
    /// <summary>
    /// Output port that uses a custom IEdgeConnectorListener so we can
    /// intercept drops on empty space and offer to create a new state.
    /// </summary>
    public class FSMPort : Port
    {
        protected FSMPort(Orientation orientation, Direction direction, Capacity capacity, Type type)
            : base(orientation, direction, capacity, type) { }

        /// <summary>Create an output port wired to <paramref name="listener"/>.</summary>
        public static FSMPort CreateOutput(IEdgeConnectorListener listener)
        {
            var port = new FSMPort(
                Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = "";
            port.AddManipulator(new EdgeConnector<FSMEdge>(listener));
            return port;
        }
    }
}
