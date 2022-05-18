using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class NodeUpgradePart : BaseModelViewPart
    {
        const string k_RootClassName = "sg-node-upgrade-part";

        static string GetUpgradeMessage(string deprecatedTypeName)
        {
            return $"The {deprecatedTypeName} has new updates. This version maintains the old behavior. " +
                $"If you update a {deprecatedTypeName}, you can use Undo to change it back. " +
                $"See the {deprecatedTypeName} documentation for more information.";
        }

        public NodeUpgradePart(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();
            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;

            m_Root.Clear();
            if (graphDataNodeModel.optedOutOfUpgrade)
            {
                // No warning box if the user already acknowledged that the node is out-of-date.
                m_Root.Add(new Button(() =>
                {
                    m_OwnerElement.RootView.Dispatch(new UpgradeNodeCommand(graphDataNodeModel));
                }) {text = "Update"});
            }
            else
            {
                GraphElementHelper.LoadTemplateAndStylesheet(m_Root, "HelpBox", k_RootClassName);
                m_Root.Q(classes: "sg-help-box").AddToClassList("sg-help-box-warning");

                var contentRoot = m_Root.MandatoryQ("sg-help-box-content");
                var l = new Label {text = GetUpgradeMessage($"{graphDataNodeModel.DisplayTitle} Node")};
                contentRoot.Add(l);
                l.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);

                contentRoot.Add(new Button(() =>
                {
                    m_OwnerElement.RootView.Dispatch(new UpgradeNodeCommand(graphDataNodeModel));
                }) {text = "Upgrade"});

                contentRoot.Add(new Button(() =>
                {
                    m_OwnerElement.RootView.Dispatch(new DismissNodeUpgradeCommand(graphDataNodeModel));
                }) {text = "Dismiss"});
            }
        }
    }
}
