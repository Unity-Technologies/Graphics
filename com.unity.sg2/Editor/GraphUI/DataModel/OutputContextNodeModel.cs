using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    // TODO: Placeholder, this has no data backing.
    public class OutputContextNodeModel : NodeModel
    {
        public OutputContextNodeModel()
        {
            m_Capabilities.Remove(GraphToolsFoundation.Overdrive.Capabilities.Deletable);
            m_Capabilities.Remove(GraphToolsFoundation.Overdrive.Capabilities.Copiable);
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            this.AddDataInputPort("PLACEHOLDER", TypeHandle.Unknown);
        }
    }
}
