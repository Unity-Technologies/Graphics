using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class ShaderPort : Port
    {
        ShaderPort(Orientation portOrientation, Direction portDirection, Type type)
            : base(portOrientation, portDirection, type) { }

        public static Port Create(Orientation orientation, Direction direction, Type type, IEdgeConnectorListener connectorListener)
        {
            var port = new ShaderPort(orientation, direction, type)
            {
                m_EdgeConnector = new EdgeConnector<Edge>(connectorListener),
            };
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }
    }
}
