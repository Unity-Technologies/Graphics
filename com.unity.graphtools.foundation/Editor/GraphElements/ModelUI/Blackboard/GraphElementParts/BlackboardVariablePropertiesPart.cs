using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for the variable properties.
    /// </summary>
    public class BlackboardVariablePropertiesPart : BaseModelUIPart
    {
        public static readonly string ussClassName = "ge-blackboard-variable-properties-part";

        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardVariablePropertiesPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardVariablePropertiesPart"/>.</returns>
        public static BlackboardVariablePropertiesPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IVariableDeclarationModel)
            {
                return new BlackboardVariablePropertiesPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected GraphElement m_VariablePropertiesView;

        /// <inheritdoc />
        public override VisualElement Root => m_VariablePropertiesView;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardVariablePropertiesPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BlackboardVariablePropertiesPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_VariablePropertiesView = GraphElementFactory.CreateUI<GraphElement>(m_OwnerElement.View, m_Model, BlackboardCreationContext.VariablePropertyCreationContext);
            m_VariablePropertiesView.AddToClassList(ussClassName);
            m_VariablePropertiesView.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_VariablePropertiesView.AddToView(m_OwnerElement.View);

            if (parent is BlackboardRow row)
                row.PropertiesSlot.Add(m_VariablePropertiesView);
            else
                parent.Add(m_VariablePropertiesView);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            m_VariablePropertiesView.UpdateFromModel();
        }

        /// <inheritdoc />
        protected override void PartOwnerAddedToView()
        {
            m_VariablePropertiesView.AddToView(m_OwnerElement.View);
            base.PartOwnerAddedToView();
        }

        /// <inheritdoc />
        protected override void PartOwnerRemovedFromView()
        {
            m_VariablePropertiesView.RemoveFromView();
            base.PartOwnerRemovedFromView();
        }
    }
}
