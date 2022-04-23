using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.ImportedGraph
{
    public class ImportedGraphWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<ImportedGraphWindow>(ImportedGraphTool.toolName);
        }

        [MenuItem("GTF/Samples/Imported Graph Editor", false)]
        public static void ShowGraphWindow()
        {
            FindOrCreateGraphWindow<ImportedGraphWindow>();
        }

        /// <inheritdoc />
        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<ImportedGraphTool>(WindowID);
        }

        protected override GraphView CreateGraphView()
        {
            return new ImportedGraphView(this, GraphTool, GraphTool.Name);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new ImportedGraphOnboardingProvider());

            return new BlankPage(GraphTool?.Dispatcher, onboardingProviders);
        }

        /// <inheritdoc />
        protected override bool CanHandleAssetType(IGraphAsset asset)
        {
            return asset is ImportedGraphAsset;
        }
    }
}
