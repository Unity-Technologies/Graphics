using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class RegistryKeyPart : BaseModelViewPart
    {
        public RegistryKeyPart(string name, IGraphElementModel model, IModelView ownerElement, string parentClassName) :
            base(name, model, ownerElement, parentClassName)
        {
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new Label();
            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel graphDataNode) return;
            m_Root.text = $"Registry Key: {graphDataNode.registryKey}";
        }

        Label m_Root;
        public override VisualElement Root => m_Root;
    }
}
