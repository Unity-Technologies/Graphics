using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// SingleFieldPart is a static port part that wraps interaction with a UIElements field.
    /// The field, of type F, has to support a value changed callback of type T.
    ///
    /// This simplifies implementation of parts that correspond directly to existing UIElements field controls, like
    /// UnityEngine.UIElements.IntegerField and UnityEditor.UIElements.ColorField.
    /// </summary>
    /// <typeparam name="F">Field UI element type.</typeparam>
    /// <typeparam name="T">Type of the value in the field.</typeparam>
    public abstract class SingleFieldPart<F, T> : AbstractStaticPortPart where F : VisualElement, INotifyValueChanged<T>
    {
        /// <summary>
        /// Template path, relative to Editor/GraphUI/Templates, to instantiate as the root of this element.
        ///
        /// Needs to contain a control of type F with the name specified in FieldName. See
        /// StaticPortParts/FloatPart.uxml for an example.
        /// </summary>
        protected abstract string UXMLTemplateName { get; }

        /// <summary>
        /// Name of the field within the instantiated template.
        /// </summary>
        protected abstract string FieldName { get; }

        /// <summary>
        /// Called when the field with name FieldName is changed. Should dispatch a command if updating graph data.
        /// </summary>
        /// <param name="change">Change event dispatched from field.</param>
        protected abstract void OnFieldValueChanged(ChangeEvent<T> change);

        public SingleFieldPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName, string portName)
            : base(name, model, ownerElement, parentClassName, portName) { }

        protected F m_Field;
        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};

            // Add common stylesheet which includes fixes for label spacing.
            m_Root.AddStylesheet("StaticPortParts/SingleFieldPart.uss");

            // Additional styling could be loaded here.
            GraphElementHelper.LoadTemplate(m_Root, UXMLTemplateName);

            m_Field = m_Root.Q<F>(FieldName);
            m_Field.RegisterValueChangedCallback(OnFieldValueChanged);

            if (m_Field is BaseField<T> baseField)
            {
                baseField.label = m_PortName;
            }

            parent.Add(m_Root);
        }
    }
}
