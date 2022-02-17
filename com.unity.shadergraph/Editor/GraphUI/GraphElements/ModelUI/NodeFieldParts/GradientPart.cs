using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GradientPart : BaseModelUIPart
    {
        const string k_GradientPartTemplate = "NodeFieldParts/GradientPart";
        const string k_GradientFieldName = "sg-gradient-field";

        public GradientPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        GradientField m_GradientField;
        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};
            GraphElementHelper.LoadTemplate(m_Root, k_GradientPartTemplate);

            m_GradientField = m_Root.Q<GradientField>(k_GradientFieldName);
            m_GradientField.RegisterValueChangedCallback(change =>
            {
                // TODO: Actually write to field, likely need to do some translation here
                Debug.Log($"Gradient is now {change.newValue}");
            });

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            // TODO: Actually read from field and reconstruct Gradient object in the way the field wants
            // m_GradientField.value = ...
        }
    }
}
