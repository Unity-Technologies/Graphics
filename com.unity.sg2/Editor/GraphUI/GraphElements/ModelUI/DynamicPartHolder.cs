using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class DynamicPartHolder : BaseModelViewPart
    {
        public override VisualElement Root => m_rootVisualElement;
        private VisualElement m_rootVisualElement;
        private readonly SGNodeModel m_sgNodeModel;

        public DynamicPartHolder(
            string name,
            GraphElementModel model,
            ModelView ownerElement,
            string parentClassName) : base(name, model, ownerElement, parentClassName)
        {
            m_sgNodeModel = model as SGNodeModel;
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            // TODO (Brett) How do we build the sub-parts?
            //for (IModelViewPart part in PartList)
            m_rootVisualElement = new VisualElement();
            parent.Add(m_rootVisualElement);
        }

        protected override void UpdatePartFromModel()
        {
            // Currently does not respond to updates from model
        }
    }
}
