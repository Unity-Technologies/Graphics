using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.VFX
{
    public class VFXGraphWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<VFXGraphWindow>(VFXGraphTool.toolName);
        }

        [MenuItem("Visual Effect/VFX Graph Editor", false)]
        public static void ShowVFXGraphWindow()
        {
            FindOrCreateGraphWindow<VFXGraphWindow>();
        }

        /// <inheritdoc />
        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<VFXGraphTool>();
        }

        protected override GraphView CreateGraphView()
        {
            // We use our own derived GraphView type to be able to scope RecipeGraphViewFactoryExtensions to our graph view.
            return new VFXGraphView(this, GraphTool, GraphTool.Name);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new VFXOnboardingProvider());

            return new BlankPage(GraphTool?.Dispatcher, onboardingProviders);
        }

        /// <inheritdoc />
        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return asset is VFXGraphAssetModel;
        }
    }
}
