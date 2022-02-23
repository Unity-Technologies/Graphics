using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// AbstractStaticPortPart is a node part that reads/writes a static port on a node.
    /// </summary>
    public abstract class AbstractStaticPortPart : BaseModelUIPart
    {
        /// <summary>
        /// Update this part's UI using the given port reader.
        /// </summary>
        /// <param name="reader">Reader for the port associated with this part.</param>
        protected abstract void UpdatePartFromPortReader(IPortReader reader);

        protected string m_PortName;

        public AbstractStaticPortPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_PortName = portName;
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel model) return;
            if (!model.TryGetNodeReader(out var nodeReader)) return;
            if (!nodeReader.TryGetPort(m_PortName, out var portReader)) return;

            UpdatePartFromPortReader(portReader);
        }
    }
}
