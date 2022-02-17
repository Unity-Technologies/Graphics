using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class FloatPart : BaseModelUIPart
    {
        const string k_FloatPartTemplate = "NodeFieldParts/FloatPart";
        const string k_FloatFieldName = "sg-float-field";

        public FloatPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        VisualElement m_Root;
        FloatField m_FloatField;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};
            GraphElementHelper.LoadTemplate(m_Root, k_FloatPartTemplate);

            m_FloatField = m_Root.Q<FloatField>(k_FloatFieldName);
            m_FloatField.RegisterValueChangedCallback(change =>
            {
                // TODO: write
                Debug.Log($"Float is now {change.newValue}");
            });

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            // TODO: read
        }
    }
}
