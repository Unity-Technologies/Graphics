using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<VerticalGraphWindow>(VerticalGraphTool.toolName);
        }

        [MenuItem("GTF/Samples/Vertical Flow", false)]
        public static void ShowRecipeGraphWindow()
        {
            FindOrCreateGraphWindow<VerticalGraphWindow>();
        }

        protected override GraphView CreateGraphView()
        {
            return new VerticalGraphView(this, GraphTool, GraphTool.Name);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider> { new VerticalOnboardingProvider() };

            return new BlankPage(GraphTool?.Dispatcher, onboardingProviders);
        }

        /// <inheritdoc />
        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return asset is VerticalGraphAssetModel;
        }

        /// <inheritdoc />
        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<VerticalGraphTool>(WindowID);
        }
    }
}
