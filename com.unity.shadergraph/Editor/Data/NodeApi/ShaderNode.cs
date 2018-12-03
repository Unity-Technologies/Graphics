using System;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    // TODO: Consider whether it should be possible to keep this around between calls to OnChange
    // Maybe it could just be valid only during OnChange for that IShaderNode?
    public struct ShaderNode
    {
        readonly AbstractMaterialGraph m_Graph;
        readonly int m_CurrentSetupContextId;
        internal readonly ProxyShaderNode node;

        internal ShaderNode(AbstractMaterialGraph graph, int currentSetupContextId, ProxyShaderNode node)
        {
            m_Graph = graph;
            m_CurrentSetupContextId = currentSetupContextId;
            this.node = node;
        }

        public object data
        {
            get
            {
                Validate();
                return node.data;
            }
            set
            {
                Validate();

                var type = value.GetType();
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    throw new ArgumentException($"{type.FullName} cannot be used as node data because it doesn't have a public, parameterless constructor.");
                }

                // TODO: Maybe do a proper check for whether the type is serializable?
                node.data = value;
            }
        }

        void Validate()
        {
            if (m_CurrentSetupContextId != m_Graph.currentStateId)
            {
                throw new InvalidOperationException($"A {nameof(ShaderNode)} is only valid in the {nameof(ShaderNodeType)} it was provided for.");
            }
        }
    }
}
