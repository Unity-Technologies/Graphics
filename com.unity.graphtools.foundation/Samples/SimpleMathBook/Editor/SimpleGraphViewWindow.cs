using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class SimpleGraphViewWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<SimpleGraphViewWindow>(MathBookGraphTool.toolName);
        }

        [MenuItem("GTF/Samples/MathBook Editor")]
        public static void ShowWindow()
        {
            FindOrCreateGraphWindow<SimpleGraphViewWindow>();
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<MathBookGraphTool>(WindowID);
        }

        protected override GraphView CreateGraphView()
        {
            var graphView = new MathBookGraphView(this, GraphTool, GraphTool.Name);

            GraphTool.Preferences.SetInitialSearcherSize(SearcherService.Usage.CreateNode, new Vector2(425, 400), 2.0f);

            graphView.RegisterCommandHandler<SetNumberOfInputPortCommand>(SetNumberOfInputPortCommand.DefaultCommandHandler);
            graphView.RegisterCommandHandler<Preferences, CreateEdgeCommand>(EdgeCommandOverrides.HandleCreateEdge, GraphTool.Preferences);
            graphView.RegisterCommandHandler<DeleteElementsCommand>(EdgeCommandOverrides.HandleDeleteEdge);
            graphView.RegisterCommandHandler<RenameElementCommand>(NodeCommandOverrides.HandleRenameNode);

            return graphView;
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new MathBookOnboardingProvider());

            return new BlankPage(GraphTool?.Dispatcher, onboardingProviders);
        }

        protected override bool CanHandleAssetType(IGraphAsset asset)
        {
            return asset is MathBookAsset;
        }

#if !UNITY_2022_2_OR_NEWER
        protected override MainToolbar CreateMainToolbar()
        {
            return new MathBookMainToolbar(GraphTool, GraphView);
        }
#endif
    }
}
