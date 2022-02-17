using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class IntPart : BaseModelUIPart
    {
        const string k_IntPartTemplate = "NodeFieldParts/IntPart";
        const string k_IntFieldName = "sg-int-field";

        public IntPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        VisualElement m_Root;
        IntegerField m_IntegerField;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};
            GraphElementHelper.LoadTemplate(m_Root, k_IntPartTemplate);

            m_IntegerField = m_Root.Q<IntegerField>(k_IntFieldName);
            m_IntegerField.RegisterValueChangedCallback(change =>
            {
                // TODO: write
                Debug.Log($"Int is now {change.newValue}");
            });

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            // TODO: read
        }
    }
}
