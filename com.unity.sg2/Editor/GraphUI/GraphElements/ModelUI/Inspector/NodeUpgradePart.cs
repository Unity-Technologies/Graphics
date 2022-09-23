using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class NodeUpgradePart : BaseMultipleModelViewsPart
    {
        // TODO GTF UPGRADE: support edition of multiple models.

        const string k_RootClassName = "sg-node-upgrade-part";

        static string GetUpgradeMessage(string deprecatedTypeName)
        {
            return $"The {deprecatedTypeName} has new updates. This version maintains the old behavior. " +
                $"If you update a {deprecatedTypeName}, you can use Undo to change it back. " +
                $"See the {deprecatedTypeName} documentation for more information.";
        }

        public NodeUpgradePart(string name, IEnumerable<Model> models, RootView rootView, string parentClassName)
            : base(name, models, rootView, parentClassName) { }

        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = "sg-node-upgrade-prompt" };
            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (!m_Models.OfType<GraphDataNodeModel>().Any()) return;

            m_Root.Clear();

            var graphDataNodeModel = m_Models.OfType<GraphDataNodeModel>().First();
            if (graphDataNodeModel.currentVersion >= graphDataNodeModel.latestAvailableVersion)
            {
                // Nothing to show if no upgrade is needed.
                return;
            }

            if (graphDataNodeModel.dismissedUpgradeVersion >= graphDataNodeModel.latestAvailableVersion)
            {
                // No warning box if the user already acknowledged that the node is out-of-date.
                m_Root.Add(new Button(UpgradeNode) {text = "Update"});
                return;
            }

            GraphElementHelper.LoadTemplateAndStylesheet(m_Root, "HelpBox", k_RootClassName);
            m_Root.Q(classes: "sg-help-box").AddToClassList("sg-help-box-warning");

            var contentRoot = m_Root.MandatoryQ("sg-help-box-content");
            var label = new Label {text = GetUpgradeMessage($"{graphDataNodeModel.Title.Nicify()} Node")};
            contentRoot.Add(label);
            label.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);

            contentRoot.Add(new Button(UpgradeNode) {text = "Upgrade"});
            contentRoot.Add(new Button(DismissUpgrade) {text = "Dismiss"});
        }

        void UpgradeNode()
        {
            if (!m_Models.OfType<GraphDataNodeModel>().Any()) return;
            var graphDataNodeModel = m_Models.OfType<GraphDataNodeModel>().First();
            RootView.Dispatch(new UpgradeNodeCommand(graphDataNodeModel));
        }

        void DismissUpgrade()
        {
            if (!m_Models.OfType<GraphDataNodeModel>().Any()) return;
            var graphDataNodeModel = m_Models.OfType<GraphDataNodeModel>().First();
            RootView.Dispatch(new DismissNodeUpgradeCommand(graphDataNodeModel));
        }
    }
}
