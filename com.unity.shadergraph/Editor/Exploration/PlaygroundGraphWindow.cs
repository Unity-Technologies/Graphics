using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace GtfPlayground
{
    public class PlaygroundGraphWindow : GraphViewEditorWindow, ISupportsOverlays
    {
        protected override bool CanHandleAssetType(IGraphAssetModel asset) => asset is PlaygroundGraphAssetModel;

        public ModelInspectorView nodeInspector { get; private set; }

        [InitializeOnLoadMethod]
        static void RegisterTool()
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
            WithSidePanel = false;

            base.OnEnable();

            // Needed to ensure that graph view takes up full window when overlay canvas is present
            rootVisualElement.style.position = new StyleEnum<Position>(Position.Absolute);
            rootVisualElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            rootVisualElement.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            // TODO: See if we can eventually use m_SidePanel instead
            nodeInspector = CreateModelInspectorView();
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
