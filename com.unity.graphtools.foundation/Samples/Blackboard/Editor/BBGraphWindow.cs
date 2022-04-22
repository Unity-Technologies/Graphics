using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public class BBGraphWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<BBGraphWindow>(BBStencil.graphName);
        }

        [MenuItem("GTF/Samples/Blackboard Sample", false)]
        public static void ShowBlackboardGraphWindow()
        {
            FindOrCreateGraphWindow<BBGraphWindow>();
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            var tool = base.CreateGraphTool();
            tool.Name = "Blackboard Sample";
            return tool;
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new BBSampleOnboardingProvider());

            return new BlankPage(GraphTool?.Dispatcher, onboardingProviders);
        }

        protected override GraphView CreateGraphView()
        {
            return new GraphView(this, GraphTool, "Blackboard Sample");
        }

        /// <inheritdoc />
        protected override bool CanHandleAssetType(IGraphAsset asset)
        {
            return asset is BBGraphAsset;
        }
    }
}
