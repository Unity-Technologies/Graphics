using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class RedirectNodeModel : NodeModel
    {
        [SerializeField]
        TypeHandle m_RedirectType = TypeHandle.Float;

        public IPortModel InputPort { get; private set; }
        public IPortModel OutputPort { get; private set; }

        /// <summary>
        /// Copies the type from a given input port to this redirect node.
        /// </summary>
        /// <param name="fromPort">Port to copy type data from.</param>
        public void UpdateTypeFrom(IPortModel fromPort)
        {
            m_RedirectType = fromPort.DataTypeHandle;
            DefineNode();
        }

        /// <summary>
        /// Clears the type of this redirect node.
        /// </summary>
        public void ClearType()
        {
            m_RedirectType = TypeHandle.Float;
            DefineNode();
        }

        /// <summary>
        /// Recursively gets the tree of redirect nodes rooted at this RedirectNodeModel.
        /// </summary>
        /// <param name="includeSelf">If true, this RedirectNodeModel will be yielded first.</param>
        /// <returns>An IEnumerable of redirect node descendants.</returns>
        public IEnumerable<RedirectNodeModel> GetRedirectTree(bool includeSelf = false)
        {
            if (includeSelf) yield return this;

            foreach (var connectedEdge in OutputPort.GetConnectedEdges())
            {
                var port = connectedEdge.ToPort;
                if (port.NodeModel is not RedirectNodeModel redirect) continue;

                yield return redirect;
                foreach (var nodeModel in redirect.GetRedirectTree()) yield return nodeModel;
            }
        }

        /// <summary>
        /// Walks the graph to find all non-redirect ports that receive a value from this redirect node.
        /// </summary>
        /// <returns>IEnumerable of destination ports. Will not include ports on redirect nodes.</returns>
        public IEnumerable<IPortModel> ResolveDestinations()
        {
            foreach (var connectedEdge in OutputPort.GetConnectedEdges())
            {
                var port = connectedEdge.ToPort;
                if (port.NodeModel is RedirectNodeModel redirect)
                {
                    foreach (var portModel in redirect.ResolveDestinations()) yield return portModel;
                }
                else
                {
                    yield return port;
                }
            }
        }

        /// <summary>
        /// Walks the graph to find the non-redirect port that supplies a value to this redirect node.
        /// </summary>
        /// <returns>The port that this redirect node's value comes from, or null if none is connected.</returns>
        public IPortModel ResolveSource()
        {
            var port = InputPort.GetConnectedEdges().FirstOrDefault()?.FromPort;
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

            InputPort = this.AddDataInputPort("In", m_RedirectType, options: PortModelOptions.NoEmbeddedConstant);
            OutputPort = this.AddDataOutputPort("Out", m_RedirectType);
        }
    }
}
