using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    [Serializable]
    [SearcherItem(typeof(ImportedGraphStencil), SearcherContext.Graph, "Service")]
    public class ServiceNode : ImportedGraphNodeBaseModel
    {
        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            AddInputPort("Request", PortType.Data, TypeHandle.String);

            AddOutputPort("Response Code", PortType.Data, TypeHandle.Int);
            AddOutputPort("Response Payload", PortType.Data, TypeHandle.String);
        }
    }
}
