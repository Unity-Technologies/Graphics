using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements
{
    public class RegistryKeyPart : BaseModelUIPart
    {
        public RegistryKeyPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName) :
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
            if (m_Model is not RegistryNodeModel registryNodeModel) return;
            m_Root.text = $"Registry Key: {registryNodeModel.registryKey}";
        }

        Label m_Root;
        public override VisualElement Root => m_Root;
    }
}
