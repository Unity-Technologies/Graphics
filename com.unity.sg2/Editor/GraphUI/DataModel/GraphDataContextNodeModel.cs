using System;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // TODO/NOTE: Don't rely on this inheriting from GraphDataNodeModel, it will eventually become a context w/ blocks.
    public class GraphDataContextNodeModel : GraphDataNodeModel
    {
        public GraphDataContextNodeModel()
        {
            m_Capabilities.Remove(GraphToolsFoundation.Overdrive.Capabilities.Deletable);
            m_Capabilities.Remove(GraphToolsFoundation.Overdrive.Capabilities.Copiable);
        }
    }
}
