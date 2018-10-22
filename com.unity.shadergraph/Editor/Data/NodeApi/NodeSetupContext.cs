using System;

namespace UnityEditor.ShaderGraph
{
    public struct NodeSetupContext
    {
        readonly AbstractMaterialGraph m_Graph;
        readonly int m_CurrentSetupContextId;
        readonly IShaderNode m_ShaderNode;

        NodeType? m_Type;

        internal NodeType? type => m_Type;

        internal NodeSetupContext(AbstractMaterialGraph graph, int currentSetupContextId, IShaderNode shaderNode)
        {
            m_Graph = graph;
            m_CurrentSetupContextId = currentSetupContextId;
            m_ShaderNode = shaderNode;
            m_Type = null;
        }

        public void RegisterType(NodeTypeDescriptor typeDescriptor)
        {
            Validate();

            // We might allow multiple types later on, or maybe it will go via another API point. For now, we only allow
            // a single node type to be provided.
            if (m_Type.HasValue)
            {
                throw new InvalidOperationException($"Only 1 node type may be provided in {nameof(IShaderNode)}.{nameof(IShaderNode.Setup)}.");
            }

            var validatedType = new NodeType();

            // Provide auto-generated name if one is not provided.
            if (string.IsNullOrWhiteSpace(typeDescriptor.name))
            {
                validatedType.name = m_ShaderNode.GetType().Name;

                // Strip "Node" from the end of the name. We also make sure that we don't strip it to an empty string,
                // in case someone decided that `Node` was a good name for a class.
                const string nodeSuffix = "Node";
                if (validatedType.name.Length > nodeSuffix.Length && validatedType.name.EndsWith(nodeSuffix))
                {
                    validatedType.name = validatedType.name.Substring(0, validatedType.name.Length - nodeSuffix.Length);
                }

                validatedType.path = string.IsNullOrWhiteSpace(typeDescriptor.path) ? "Uncategorized" : typeDescriptor.path;
            }
            else
            {
                validatedType.name = typeDescriptor.name;
            }

            m_Type = validatedType;
        }

        void Validate()
        {
            if (m_CurrentSetupContextId != m_Graph.currentSetupContextId)
            {
                throw new InvalidOperationException($"{nameof(NodeSetupContext)} is only valid during the call to {nameof(IShaderNode)}.{nameof(IShaderNode.Setup)} it was provided for.");
            }
        }
    }
}
