using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for a variable.
    /// </summary>
    public class BlackboardVariablePart : BaseModelViewPart
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
        public static BlackboardVariablePart Create(string name, IModel model, IModelView ownerElement, string parentClassName)
        {
            if (model is IVariableDeclarationModel)
            {
                return new BlackboardVariablePart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected BlackboardElement m_Field;

        /// <inheritdoc />
        public override VisualElement Root => m_Field;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardVariablePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BlackboardVariablePart(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            if (m_Model is IVariableDeclarationModel variableDeclarationModel)
            {
                m_Field = ModelViewFactory.CreateUI<BlackboardElement>(m_OwnerElement.RootView,
                    variableDeclarationModel, BlackboardCreationContext.VariableCreationContext);

                if (m_Field == null)
                    return;

                m_Field.AddToClassList(ussClassName);
                m_Field.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_Field.AddToRootView(m_OwnerElement.RootView);

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
            m_OwnerElement.RootView.Dispatch(new RenameElementCommand(m_Model as IRenamable, e.newValue));
        }

        /// <inheritdoc />
        protected override void PartOwnerAddedToView()
        {
            m_Field.AddToRootView(m_OwnerElement.RootView);
            base.PartOwnerAddedToView();
        }

        /// <inheritdoc />
        protected override void PartOwnerRemovedFromView()
        {
            m_Field.RemoveFromRootView();
            base.PartOwnerRemovedFromView();
        }
    }
}
