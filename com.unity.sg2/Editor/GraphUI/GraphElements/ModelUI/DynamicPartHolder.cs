using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class DynamicPartHolder : BaseModelViewPart
    {
        public override VisualElement Root => m_rootVisualElement;
        private VisualElement m_rootVisualElement;
        private readonly GraphDataNodeModel m_graphDataNodeModel;

        public DynamicPartHolder(
            string name,
            IGraphElementModel model,
            IModelView ownerElement,
            string parentClassName) : base(name, model, ownerElement, parentClassName)
        {
            m_graphDataNodeModel = model as GraphDataNodeModel;
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
