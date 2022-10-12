using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class DataNodeTextPart : BaseModelViewPart
    {
        public DataNodeTextPart(string name, GraphElementModel model, ModelView ownerElement,
            string parentClassName) : base(name, model, ownerElement, parentClassName)
        {
        }

        public override VisualElement Root => label;
        private TextElement label;

        protected override void BuildPartUI(VisualElement parent)
        {
            if (m_Model is not DataNodeModel)
                return;

            label = new Label("");
            label.style.marginTop = 4;
            label.style.marginBottom = 4;
            label.style.marginLeft = 4;
            label.style.marginRight = 4;

            parent.Add(label);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not DataNodeModel dataNode)
                return;

            label.text = $"intValue = {dataNode.intValue}\n" +
                $"floatValue = {dataNode.floatValue}";
        }
    }
}
