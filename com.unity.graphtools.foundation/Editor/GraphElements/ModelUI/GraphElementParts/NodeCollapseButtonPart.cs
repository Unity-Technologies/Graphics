using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A <see cref="CollapseButtonPart"/> that disables itself if the node cannot be collapsed.
    /// </summary>
    public class NodeCollapseButtonPart : CollapseButtonPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NodeCollapseButtonPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="NodeCollapseButtonPart"/>.</returns>
        public new static NodeCollapseButtonPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is ICollapsible)
            {
                return new NodeCollapseButtonPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeCollapseButtonPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected NodeCollapseButtonPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            if (CollapseButton != null)
            {
                if (m_Model is IPortNodeModel portHolder && portHolder.Ports != null)
                {
                    var allPortConnected = portHolder.Ports.All(port => port.IsConnected());
                    CollapseButton?.SetDisabledPseudoState(allPortConnected);
                }
            }
        }
    }
}
