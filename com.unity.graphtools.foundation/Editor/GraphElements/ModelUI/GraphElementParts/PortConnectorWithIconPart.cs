using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A <see cref="PortConnectorPart"/> with an icon showing the port type.
    /// </summary>
    public class PortConnectorWithIconPart : PortConnectorPart
    {
        public static readonly string iconUssName = "icon";

        /// <summary>
        /// Creates a new instance of the <see cref="PortConnectorWithIconPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="PortConnectorWithIconPart"/>.</returns>
        public new static PortConnectorWithIconPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IPortModel && ownerElement is Port)
            {
                return new PortConnectorWithIconPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected Image m_Icon;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortConnectorWithIconPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected PortConnectorWithIconPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            base.BuildPartUI(container);

            m_Icon = new Image();
            m_Icon.AddToClassList(m_ParentClassName.WithUssElement(iconUssName));
            m_Icon.tintColor = (m_OwnerElement as Port)?.PortColor ?? Color.white;
            Root.Insert(1, m_Icon);
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            Root.AddStylesheet("PortConnectorWithIconPart.uss");
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();
            m_Icon.tintColor = (m_OwnerElement as Port)?.PortColor ?? Color.white;
        }
    }
}
