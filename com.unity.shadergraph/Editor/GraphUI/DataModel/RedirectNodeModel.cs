using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    // Create a temporary searcher item for redirect nodes. Eventually, they will be created through other means.
    [SearcherItem(typeof(ShaderGraphStencil), SearcherContext.Graph, "Redirect (TEMP)")]
    public class RedirectNodeModel : NodeModel
    {
        TypeHandle m_RedirectType = TypeHandle.Unknown;

        public TypeHandle RedirectType
        {
            get => m_RedirectType;
            set => m_RedirectType = value;
        }

        IPortModel m_InputPort, m_OutputPort;

        public IEnumerable<IPortModel> ResolveDestinations()
        {
            foreach (var connectedEdge in m_OutputPort.GetConnectedEdges())
            {
                var port = connectedEdge.ToPort;
                if (port.NodeModel is RedirectNodeModel redirect)
                {
                    foreach (var nodeModel in redirect.ResolveDestinations()) yield return nodeModel;
                }
                else
                {
                    yield return port;
                }
            }
        }

        public IPortModel ResolveSource()
        {
            var port = m_InputPort.GetConnectedEdges().FirstOrDefault()?.FromPort;
            if (port == null) return null;

            if (port.NodeModel is RedirectNodeModel redirect)
            {
                return redirect.ResolveSource();
            }

            return port;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            m_InputPort = this.AddDataInputPort("In", m_RedirectType, options: PortModelOptions.NoEmbeddedConstant);
            m_OutputPort = this.AddDataOutputPort("Out", m_RedirectType);
        }
    }
}
