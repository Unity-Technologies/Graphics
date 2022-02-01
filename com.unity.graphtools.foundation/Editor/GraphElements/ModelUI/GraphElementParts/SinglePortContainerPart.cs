using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for a single port.
    /// </summary>
    public class SinglePortContainerPart : BaseModelUIPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="SinglePortContainerPart"/>.</returns>
        public static SinglePortContainerPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IPortModel)
            {
                return new SinglePortContainerPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected PortContainer m_PortContainer;

        /// <inheritdoc />
        public override VisualElement Root => m_PortContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected SinglePortContainerPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IPortModel)
            {
                m_PortContainer = new PortContainer { name = PartName };
                m_PortContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(m_PortContainer);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is IPortModel portModel)
                m_PortContainer?.UpdatePorts(new[] { portModel }, m_OwnerElement.View);
        }
    }
}
