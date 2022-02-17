using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ColorPart : BaseModelUIPart
    {
        const string k_ColorPartTemplate = "NodeFieldParts/ColorPart";
        const string k_ColorFieldName = "sg-color-field";

        public ColorPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        VisualElement m_Root;
        ColorField m_ColorField;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};
            GraphElementHelper.LoadTemplate(m_Root, k_ColorPartTemplate);

            m_ColorField = m_Root.Q<ColorField>(k_ColorFieldName);
            m_ColorField.RegisterValueChangedCallback(change =>
            {
                // TODO: Write color to field
                Debug.Log($"Color is now {change.newValue}");
            });

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            // TODO: Read color from field
            // m_ColorField.value = ...
        }
    }
}
