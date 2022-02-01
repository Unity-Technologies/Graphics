using System;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for a value editor for a <see cref="IConstantNodeModel"/>.
    /// </summary>
    public class ConstantNodeEditorPart : BaseModelUIPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ConstantNodeEditorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="ConstantNodeEditorPart"/>.</returns>
        public static ConstantNodeEditorPart Create(string name, IGraphElementModel model,
            IModelUI ownerElement, string parentClassName)
        {
            if (model is IConstantNodeModel)
            {
                return new ConstantNodeEditorPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantNodeEditorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected ConstantNodeEditorPart(string name, IGraphElementModel model, IModelUI ownerElement,
                                         string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        public static readonly string constantEditorElementUssClassName = "constant-editor";
        public static readonly string labelUssName = "constant-editor-label";

        protected Label m_Label;

        protected VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IConstantNodeModel constantNodeModel)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_Label = new Label { name = labelUssName };
                m_Label.AddToClassList(m_ParentClassName.WithUssElement(labelUssName));
                m_Root.Add(m_Label);

                var tokenEditor = InlineValueEditor.CreateEditorForConstant(
                    m_OwnerElement.View, null, constantNodeModel.Value, OnValueChanged, constantNodeModel.IsLocked);

                if (tokenEditor != null)
                {
                    tokenEditor.AddToClassList(m_ParentClassName.WithUssElement(constantEditorElementUssClassName));
                    m_Root.Add(tokenEditor);
                }
                container.Add(m_Root);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is IConstantNodeModel constantNodeModel)
            {
                if (constantNodeModel.Value is IStringWrapperConstantModel icm)
                {
                    if (!TokenEditorNeedsLabel)
                        m_Label.style.display = DisplayStyle.None;
                    else
                        m_Label.text = icm.Label;
                }
            }
        }

        static TypeHandle[] s_PropsToHideLabel =
        {
            TypeHandle.Int,
            TypeHandle.Float,
            TypeHandle.Vector2,
            TypeHandle.Vector3,
            TypeHandle.Vector4,
            TypeHandle.String
        };

        bool TokenEditorNeedsLabel
        {
            get
            {
                if (m_Model is IConstantNodeModel constantNodeModel)
                    return !s_PropsToHideLabel.Contains(constantNodeModel.Type.GenerateTypeHandle());
                return true;
            }
        }

        protected void OnValueChanged(IChangeEvent evt, object arg3)
        {
            if (m_Model is IConstantNodeModel constantNodeModel)
            {
                if (evt != null) // Enum editor sends null
                {
                    var p = evt.GetType().GetProperty("newValue");
                    var newValue = p?.GetValue(evt);
                    m_OwnerElement.View.Dispatch(new UpdateConstantValueCommand(constantNodeModel.Value, newValue, constantNodeModel));
                }
            }
        }
    }
}
