using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows
{
    public class ShaderGraphEditorWindow : GraphViewEditorWindow, ISupportsOverlays
    {
        protected override bool CanHandleAssetType(IGraphAssetModel asset) => asset is ShaderGraphAssetModel;

        public ModelInspectorView nodeInspector { get; private set; }

        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<ShaderGraphEditorWindow>(ShaderGraphStencil.Name);
        }

        [MenuItem("Window/Shaders/ShaderGraph", false)]
        public static void ShowRecipeGraphWindow()
        {
            FindOrCreateGraphWindow<ShaderGraphEditorWindow>();
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
            return new ShaderGraphView(this, CommandDispatcher, EditorToolName);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider> {new ShaderGraphOnboardingProvider()};
            return new BlankPage(CommandDispatcher, onboardingProviders);
        }

        protected override GraphToolState CreateInitialState()
        {
            var prefs = Preferences.CreatePreferences(EditorToolName);
            return new ShaderGraphState(GUID, prefs);
        }
    }
}
