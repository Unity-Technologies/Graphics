using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// AbstractStaticPortPart is a node part that reads/writes a static port on a node.
    /// </summary>
    public abstract class AbstractStaticPortPart : BaseModelViewPart
    {
        /// <summary>
        /// Update this part's UI using the given port reader.
        /// </summary>
        /// <param name="reader">Reader for the port associated with this part.</param>
        protected abstract void UpdatePartFromPortReader(PortHandler reader);

        protected string m_PortName;

        public AbstractStaticPortPart(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_PortName = portName;
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel model) return;
            if (!model.TryGetNodeHandler(out var nodeReader)) return;
            var port = nodeReader.GetPort(m_PortName);
            if (port != null)
                UpdatePartFromPortReader(port);
        }
    }
}
