using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class NodeUpgradePart : BaseModelViewPart
    {
        const string k_RootClassName = "sg-node-upgrade-part";

        public NodeUpgradePart(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();
            GraphElementHelper.LoadTemplateAndStylesheet(m_Root, "HelpBox", k_RootClassName);

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            Debug.LogWarning("UNIMPLEMENTED: UpdatePartFromModel"); // TODO
        }
    }
}
