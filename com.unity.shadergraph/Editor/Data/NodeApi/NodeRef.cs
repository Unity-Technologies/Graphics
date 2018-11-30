using System;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    // TODO: Consider whether it should be possible to keep this around between calls to OnChange
    // Maybe it could just be valid only during OnChange for that IShaderNode?
    public struct NodeRef
    {
        readonly AbstractMaterialGraph m_Graph;
        readonly int m_CurrentSetupContextId;
        internal readonly ProxyShaderNode node;

        internal NodeRef(AbstractMaterialGraph graph, int currentSetupContextId, ProxyShaderNode node)
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
        }

        void Validate()
        {
            if (m_CurrentSetupContextId != m_Graph.currentContextId)
            {
                throw new InvalidOperationException($"{nameof(NodeRef)} is only valid during the {nameof(ShaderNodeType)} it was provided for.");
            }
        }
    }
}
