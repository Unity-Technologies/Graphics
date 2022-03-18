using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A BlackboardElement to display a <see cref="IVariableDeclarationModel"/>.
    /// </summary>
    public class BlackboardField : BlackboardElement
    {
        /// <summary>
        /// The uss class name for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard-field";

        /// <summary>
        /// The uss class name for the capsule.
        /// </summary>
        public static readonly string capsuleUssClassName = ussClassName.WithUssElement("capsule");

        /// <summary>
        /// The uss class name for the name label.
        /// </summary>
        public static readonly string nameLabelUssClassName = ussClassName.WithUssElement("name-label");

        /// <summary>
        /// The uss class name for the icon.
        /// </summary>
        public static readonly string iconUssClassName = ussClassName.WithUssElement("icon");

        /// <summary>
        /// The uss class name for the type label.
        /// </summary>
        public static readonly string typeLabelUssClassName = ussClassName.WithUssElement("type-label");

        /// <summary>
        /// The uss class name for the highlighted modifier.
        /// </summary>
        public static readonly string highlightedModifierUssClassName = ussClassName.WithUssModifier("highlighted");

        /// <summary>
        /// The uss class name for the read only modifier.
        /// </summary>
        public static readonly string readOnlyModifierUssClassName = ussClassName.WithUssModifier("read-only");

        /// <summary>
        /// The uss class name for the write only modifier.
        /// </summary>
        public static readonly string writeOnlyModifierUssClassName = ussClassName.WithUssModifier("write-only");

        /// <summary>
        /// The uss class name for the exposed modifier.
        /// </summary>
        public static readonly string iconExposedModifierUssClassName = iconUssClassName.WithUssModifier("exposed");

        /// <summary>
        /// The selection border element.
        /// </summary>
        public static readonly string selectionBorderElementName = "selection-border";

        /// <summary>
        /// The label containing the type name.
        /// </summary>
        protected Label m_TypeLabel;

        /// <summary>
        /// The element containing the icon.
        /// </summary>
        protected VisualElement m_Icon;

        SelectionDropper m_SelectionDropper;

        /// <summary>
        /// The <see cref="EditableLabel"/> containing the name of the field.
        /// </summary>
        public EditableLabel NameLabel { get; protected set; }

        internal bool IsHighlighted() => ClassListContains(highlightedModifierUssClassName);

        /// <summary>
        /// The selection dropper for the element.
        /// </summary>
        protected SelectionDropper SelectionDropper
        {
            get => m_SelectionDropper;
            set => this.ReplaceManipulator(ref m_SelectionDropper, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardField"/> class.
        /// </summary>
        public BlackboardField()
        {
            SelectionDropper = new SelectionDropper();
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);

            var capsule = new VisualElement();
            capsule.AddToClassList(capsuleUssClassName);
            selectionBorder.ContentContainer.Add(capsule);

            m_Icon = new Image();
            m_Icon.AddToClassList(iconUssClassName);
            m_Icon.AddStylesheet("TypeIcons.uss");
            capsule.Add(m_Icon);

            NameLabel = new EditableLabel { name = "name", EditActionName = "Rename"};
            NameLabel.AddToClassList(nameLabelUssClassName);
            capsule.Add(NameLabel);

            m_TypeLabel = new Label() { name = "type-label" };
            m_TypeLabel.AddToClassList(typeLabelUssClassName);
            Add(m_TypeLabel);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (Model is IVariableDeclarationModel variableDeclarationModel)
            {
                m_Icon.PrefixEnableInClassList(Port.dataTypeClassPrefix, Port.GetClassNameSuffixForDataType(variableDeclarationModel.DataType.Resolve()));

                var typeName = variableDeclarationModel.DataType.GetMetadata(variableDeclarationModel.GraphModel.Stencil).FriendlyName;

                NameLabel.SetValueWithoutNotify(variableDeclarationModel.DisplayTitle);
                m_TypeLabel.text = typeName;

                EnableInClassList(readOnlyModifierUssClassName, (variableDeclarationModel.Modifiers & ModifierFlags.Read) != 0);
                EnableInClassList(writeOnlyModifierUssClassName, (variableDeclarationModel.Modifiers & ModifierFlags.Write) != 0);

                var highlight = BlackboardView.GraphTool.HighlighterState.GetDeclarationModelHighlighted(variableDeclarationModel);

                // Do not show highlight if selected.
                if (highlight && BlackboardView.BlackboardViewModel.SelectionState.IsSelected(variableDeclarationModel))
                {
                    highlight = false;
                }

                EnableInClassList(highlightedModifierUssClassName, highlight);
            }
        }

        /// <inheritdoc />
        public override void ActivateRename()
        {
            NameLabel.BeginEditing();
        }
    }
}
