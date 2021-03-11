using System;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class ShaderPort : Port
    {
        ShaderPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type)
            : base(portOrientation, portDirection, portCapacity, type)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderPort"));
        }

        MaterialSlot m_Slot;

        public static ShaderPort Create(MaterialSlot slot, IEdgeConnectorListener connectorListener)
        {
            var port = new ShaderPort(Orientation.Horizontal, slot.isInputSlot ? Direction.Input : Direction.Output,
                slot.isInputSlot ? Capacity.Single : Capacity.Multi, null)
            {
                m_EdgeConnector = new EdgeConnector<Edge>(connectorListener),
            };
            port.AddManipulator(port.m_EdgeConnector);
            port.slot = slot;
            port.portName = slot.displayName;
            port.visualClass = slot.concreteValueType.ToClassName();
            return port;
        }

        public MaterialSlot slot
        {
            get { return m_Slot; }
            set
            {
                if (ReferenceEquals(value, m_Slot))
                    return;
                if (value == null)
                    throw new NullReferenceException();
                if (m_Slot != null && value.isInputSlot != m_Slot.isInputSlot)
                    throw new ArgumentException("Cannot change direction of already created port");
                m_Slot = value;
                portName = slot.displayName;
                visualClass = slot.concreteValueType.ToClassName();
            }
        }

        public Action<Port> OnDisconnect;

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);
            OnDisconnect?.Invoke(this);
        }
    }

    static class ShaderPortExtensions
    {
        public static MaterialSlot GetSlot(this Port port)
        {
            var shaderPort = port as ShaderPort;
            return shaderPort != null ? shaderPort.slot : null;
        }
    }
}
