using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for a value editor for a port.
    /// </summary>
    public class PortConstantEditorPart : BaseModelUIPart
    {
        public static readonly string constantEditorUssName = "constant-editor";

        /// <summary>
        /// Initializes a new instance of the <see cref="PortConstantEditorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="PortConstantEditorPart"/>.</returns>
        public static PortConstantEditorPart Create(string name, IGraphElementModel model,
            IModelUI ownerElement, string parentClassName)
        {
            if (model is IPortModel)
            {
                return new PortConstantEditorPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected VisualElement m_Editor;

        Type m_EditorDataType;

        protected VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortConstantEditorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected PortConstantEditorPart(string name, IGraphElementModel model, IModelUI ownerElement,
                                         string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            var portModel = m_Model as IPortModel;
            if (portModel != null && portModel.Direction == PortDirection.Input)
            {
                InitRoot(container);
                if (portModel.EmbeddedValue != null)
                    BuildConstantEditor();
            }
        }

        void InitRoot(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            container.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is IPortModel portModel)
            {
                BuildConstantEditor();
                m_Editor?.SetEnabled(!portModel.DisableEmbeddedValueEditor);
            }
        }

        protected void BuildConstantEditor()
        {
            if (m_Model is IPortModel portModel)
            {
                // Rebuild editor if port data type changed.
                if (m_Editor != null && portModel.EmbeddedValue?.Type != m_EditorDataType)
                {
                    m_Editor.RemoveFromHierarchy();
                    m_Editor = null;
                }

                if (m_Editor == null)
                {
                    if (portModel.Direction == PortDirection.Input && portModel.EmbeddedValue != null)
                    {
                        m_EditorDataType = portModel.EmbeddedValue.Type;
                        m_Editor = InlineValueEditor.CreateEditorForConstant(
                            m_OwnerElement.View, portModel, portModel.EmbeddedValue,
                            OnValueChanged, false);

                        if (m_Editor != null)
                        {
                            m_Editor.AddToClassList(m_ParentClassName.WithUssElement(constantEditorUssName));
                            m_Root.Add(m_Editor);
                        }
                    }
                }
            }
        }

        protected void OnValueChanged(IChangeEvent evt, object newValue)
        {
            if (m_Model is IPortModel portModel)
            {
                m_OwnerElement.View.Dispatch(new UpdatePortConstantCommand(portModel, newValue));
            }
        }
    }
}
