using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    class ContextGraphViewWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<ContextGraphViewWindow>(ContextGraphTool.toolName);
        }

        [MenuItem("GTF/Samples/Contexts Editor")]
        public static void ShowWindow()
        {
            GetWindow<ContextGraphViewWindow>();
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<ContextGraphTool>(WindowID);
        }

        protected override GraphView CreateGraphView()
        {
            var graphView = new ContextGraphView(this, GraphTool, GraphTool.Name);

            graphView.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
            graphView.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);

            return graphView;
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new ContextSampleOnboardingProvider());

            return new BlankPage(GraphTool?.Dispatcher, onboardingProviders);
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return asset is ContextSampleAsset;
        }
    }
}
