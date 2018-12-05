using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    public struct NodeSetupContext
    {
        readonly AbstractMaterialGraph m_Graph;
        readonly int m_CurrentSetupContextId;
        readonly NodeTypeState m_TypeState;

        internal NodeSetupContext(AbstractMaterialGraph graph, int currentSetupContextId, NodeTypeState typeState)
        {
            m_Graph = graph;
            m_CurrentSetupContextId = currentSetupContextId;
            m_TypeState = typeState;
        }

        void Validate()
        {
            if (m_CurrentSetupContextId != m_Graph.currentStateId)
            {
                throw new InvalidOperationException($"{nameof(NodeSetupContext)} is only valid during the call to {nameof(ShaderNodeType)}.{nameof(ShaderNodeType.Setup)} it was provided for.");
            }
        }
    }
}
