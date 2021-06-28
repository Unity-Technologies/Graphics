using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace GtfPlayground
{
    public class PlaygroundGraphWindow : GraphViewEditorWindow
    {
        protected override bool CanHandleAssetType(GraphAssetModel asset) => asset is PlaygroundGraphAssetModel;

        [InitializeOnLoadMethod]
        private static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<PlaygroundGraphWindow>(PlaygroundStencil.Name);
        }


        [MenuItem("Window/GTF Playground", false)]
        public static void ShowRecipeGraphWindow()
        {
            FindOrCreateGraphWindow<PlaygroundGraphWindow>();
        }

        protected override void OnEnable()
        {
            EditorToolName = "GTF Playground";
            base.OnEnable();
        }

        protected override GraphView CreateGraphView()
        {
            return new PlaygroundGraphView(this, CommandDispatcher, EditorToolName);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider> {new PlaygroundOnboardingProvider()};
            return new BlankPage(CommandDispatcher, onboardingProviders);
        }
        
        protected override GraphToolState CreateInitialState()
        {
            var prefs = Preferences.CreatePreferences(EditorToolName);
            return new PlaygroundState(GUID, prefs);
        }
    }
}