using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    public struct NodeSetupContext
    {
        readonly AbstractMaterialGraph m_Graph;
        readonly int m_CurrentSetupContextId;
        readonly ShaderNodeState m_State;

        bool m_NodeTypeCreated;

        internal bool nodeTypeCreated => m_NodeTypeCreated;

        internal NodeSetupContext(AbstractMaterialGraph graph, int currentSetupContextId, ShaderNodeState state)
        {
            m_Graph = graph;
            m_CurrentSetupContextId = currentSetupContextId;
            m_State = state;
            m_NodeTypeCreated = false;
        }

        public void CreateType(NodeTypeDescriptor typeDescriptor)
        {
            Validate();

            // Before doing anything, we perform validation on the provided NodeTypeDescriptor.

            // We might allow multiple types later on, or maybe it will go via another API point. For now, we only allow
            // a single node type to be provided.
            if (m_NodeTypeCreated)
            {
                throw new InvalidOperationException($"An {nameof(IShaderNode)} can only have 1 type.");
            }

            var i = 0;
            foreach (var portRef in typeDescriptor.inputs)
            {
                // PortRef can be 0 if the user created an instance themselves. We cannot remove the default constructor
                // in C#, so instead we let the default value represent an invalid state.
                if (!portRef.isValid || portRef.isInput && portRef.index >= m_State.inputPorts.Count || !portRef.isInput && portRef.index >= m_State.outputPorts.Count)
                {
                    throw new InvalidOperationException($"{nameof(NodeTypeDescriptor)}.{nameof(NodeTypeDescriptor.inputs)} contains an invalid port at index {i}.");
                }

                if (!portRef.isInput)
                {
                    var port = m_State.outputPorts[portRef.index];
                    throw new InvalidOperationException($"{nameof(NodeTypeDescriptor)}.{nameof(NodeTypeDescriptor.inputs)} contains an output port at index {i} ({port.ToString()}).");
                }

                i++;
            }

            i = 0;
            foreach (var portRef in typeDescriptor.outputs)
            {
                if (!portRef.isValid || portRef.isInput && portRef.index >= m_State.inputPorts.Count || !portRef.isInput && portRef.index >= m_State.outputPorts.Count)
                {
                    throw new InvalidOperationException($"{nameof(NodeTypeDescriptor)}.{nameof(NodeTypeDescriptor.inputs)} contains an invalid port at index {i}.");
                }

                if (portRef.isInput)
                {
                    var port = m_State.inputPorts[portRef.index];
                    throw new InvalidOperationException($"{nameof(NodeTypeDescriptor)}.{nameof(NodeTypeDescriptor.outputs)} contains an input port at index {i} ({port.ToString()}).");
                }

                i++;
            }

            m_State.type.name = typeDescriptor.name;
            // Provide auto-generated name if one is not provided.
            if (string.IsNullOrWhiteSpace(m_State.type.name))
            {
                m_State.type.name = m_State.GetType().Name;

                // Strip "Node" from the end of the name. We also make sure that we don't strip it to an empty string,
                // in case someone decided that `Node` was a good name for a class.
                const string nodeSuffix = "Node";
                if (m_State.type.name.Length > nodeSuffix.Length && m_State.type.name.EndsWith(nodeSuffix))
                {
                    m_State.type.name = m_State.type.name.Substring(0, m_State.type.name.Length - nodeSuffix.Length);
                }
            }

            m_State.type.path = typeDescriptor.path;
            // Don't want nodes showing up at the root and cluttering everything.
            if (string.IsNullOrWhiteSpace(m_State.type.path))
            {
                m_State.type.path = "Uncategorized";
            }

            m_State.type.inputs = new List<PortRef>(typeDescriptor.inputs);
            m_State.type.outputs = new List<PortRef>(typeDescriptor.outputs);

            m_NodeTypeCreated = true;
        }

        public PortRef CreateInputPort(int id, string displayName, PortValue value)
        {
            if (m_State.inputPorts.Any(x => x.id == id) || m_State.outputPorts.Any(x => x.id == id))
            {
                throw new ArgumentException($"A port with id {id} already exists.", nameof(id));
            }

            m_State.inputPorts.Add(new InputPortDescriptor { id = id, displayName = displayName, value = value });
            return new PortRef(m_State.inputPorts.Count, true);
        }

        public PortRef CreateOutputPort(int id, string displayName, PortValueType type)
        {
            if (m_State.inputPorts.Any(x => x.id == id) || m_State.outputPorts.Any(x => x.id == id))
            {
                throw new ArgumentException($"A port with id {id} already exists.", nameof(id));
            }

            m_State.outputPorts.Add(new OutputPortDescriptor { id = id, displayName = displayName, type = type });
            return new PortRef(m_State.outputPorts.Count, false);
        }

        void Validate()
        {
            if (m_CurrentSetupContextId != m_Graph.currentContextId)
            {
                throw new InvalidOperationException($"{nameof(NodeSetupContext)} is only valid during the call to {nameof(IShaderNode)}.{nameof(IShaderNode.Setup)} it was provided for.");
            }
        }
    }
}
