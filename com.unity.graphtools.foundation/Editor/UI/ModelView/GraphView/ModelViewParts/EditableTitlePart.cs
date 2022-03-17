using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for the title of an <see cref="IHasTitle"/> model using an <see cref="EditableLabel"/> to allow editing.
    /// </summary>
    public class EditableTitlePart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-editable-title-part";
        public static readonly string titleLabelName = "title";

        /// <summary>
        /// Creates a new instance of the <see cref="EditableTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="multiline">Whether the text should be displayed on multiple lines.</param>
        /// <returns>A new instance of <see cref="EditableTitlePart"/>.</returns>
        public static EditableTitlePart Create(string name, IModel model, IModelView ownerElement, string parentClassName, bool multiline = false)
        {
            if (model is IHasTitle)
            {
                return new EditableTitlePart(name, model, ownerElement, parentClassName, multiline);
            }

            return null;
        }

        bool m_Multiline;

        protected VisualElement TitleContainer { get; set; }

        public VisualElement TitleLabel { get; protected set; }

        /// <inheritdoc />
        public override VisualElement Root => TitleContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditableTitlePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="multiline">Whether the text should be displayed on multiple lines.</param>
        protected EditableTitlePart(string name, IModel model, IModelView ownerElement, string parentClassName, bool multiline)
            : base(name, model, ownerElement, parentClassName)
        {
            m_Multiline = multiline;
        }

        protected virtual bool HasEditableLabel => (m_Model as IGraphElementModel).IsRenamable();

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IHasTitle)
            {
                TitleContainer = new VisualElement { name = PartName };
                TitleContainer.AddToClassList(ussClassName);
                TitleContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                if (HasEditableLabel)
                {
                    TitleLabel = new EditableLabel { name = titleLabelName, multiline = m_Multiline, EditActionName = "Rename"};
                    TitleLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
                }
                else
                {
                    TitleLabel = new Label { name = titleLabelName };
                }

                TitleLabel.AddToClassList(ussClassName.WithUssElement(titleLabelName));
                TitleLabel.AddToClassList(m_ParentClassName.WithUssElement(titleLabelName));
                TitleContainer.Add(TitleLabel);

                container.Add(TitleContainer);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (TitleLabel != null)
            {
                var value = (m_Model as IHasTitle)?.DisplayTitle ?? String.Empty;
                if (TitleLabel is EditableLabel editableLabel)
                    editableLabel.SetValueWithoutNotify(value);
                else if (TitleLabel is Label label)
                    label.text = value;
            }
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            TitleContainer.AddStylesheet("EditableTitlePart.uss");
        }

        protected void OnRename(ChangeEvent<string> e)
        {
            m_OwnerElement.RootView.Dispatch(new RenameElementCommand(m_Model as IRenamable, e.newValue));
        }


        public void BeginEditing()
        {
            if( TitleLabel is EditableLabel editableLabel)
                editableLabel.BeginEditing();
        }
    }
}
