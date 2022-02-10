using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for a variable.
    /// </summary>
    public class BlackboardVariablePart : BaseModelUIPart
    {
        public static readonly string ussClassName = "ge-blackboard-variable-part";

        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardVariablePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardVariablePart"/>.</returns>
        public static BlackboardVariablePart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IVariableDeclarationModel)
            {
                return new BlackboardVariablePart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected GraphElement m_Field;

        /// <inheritdoc />
        public override VisualElement Root => m_Field;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardVariablePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BlackboardVariablePart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            if (m_Model is IVariableDeclarationModel variableDeclarationModel)
            {
                m_Field = GraphElementFactory.CreateUI<GraphElement>(m_OwnerElement.View,
                    variableDeclarationModel, BlackboardCreationContext.VariableCreationContext);

                if (m_Field == null)
                    return;

                m_Field.AddToClassList(ussClassName);
                m_Field.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                m_Field.viewDataKey = m_Model.Guid + "__" + Blackboard.persistenceKey;

                m_Field.AddToView(m_OwnerElement.View);

                if (m_Field is BlackboardField blackboardField)
                {
                    blackboardField.NameLabel.RegisterCallback<ChangeEvent<string>>(OnFieldRenamed);
                }

                if (parent is BlackboardRow row)
                    row.FieldSlot.Add(m_Field);
                else
                    parent.Add(m_Field);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel() {}

        void OnFieldRenamed(ChangeEvent<string> e)
        {
            m_OwnerElement.View.Dispatch(new RenameElementCommand(m_Model as IRenamable, e.newValue));
        }

        /// <inheritdoc />
        protected override void PartOwnerAddedToView()
        {
            m_Field.AddToView(m_OwnerElement.View);
            base.PartOwnerAddedToView();
        }

        /// <inheritdoc />
        protected override void PartOwnerRemovedFromView()
        {
            m_Field.RemoveFromView();
            base.PartOwnerRemovedFromView();
        }
    }
}
