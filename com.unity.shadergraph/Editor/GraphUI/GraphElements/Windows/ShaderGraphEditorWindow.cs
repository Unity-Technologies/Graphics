using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.Controllers;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.CommandDispatch;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows
{
    public class ShaderGraphEditorWindow : GraphViewEditorWindow, ISupportsOverlays
    {
        protected override bool CanHandleAssetType(IGraphAssetModel asset) => asset is ShaderGraphAssetModel;

        InspectorController m_InspectorController;
        ModelInspectorView m_InspectorView => m_InspectorController?.View;

        BlackboardController m_BlackboardController;
        Blackboard m_BlackboardView => m_BlackboardController?.View;

        PreviewController m_PreviewController;
        Preview m_Preview => m_PreviewController?.View;

        static GraphWindowTickCommand s_CachedGraphWindowTickCommand = new ();

        public VisualElement GetGraphSubWindow<T>()
        {
            if (typeof(T) == typeof(Blackboard))
                return m_BlackboardView;
            if (typeof(T) == typeof(ModelInspectorView))
                return m_InspectorView;
            if (typeof(T) == typeof(Preview))
                return m_Preview;

            return null;
        }

        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<ShaderGraphEditorWindow>(ShaderGraphStencil.Name);
        }

        [MenuItem("Window/Shaders/ShaderGraph", false)]
        public static void ShowShaderGraphWindow()
        {
            var shaderGraphEditorWindow = CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
            shaderGraphEditorWindow.Show();
            shaderGraphEditorWindow.Focus();
        }

        void InitializeSubWindows()
        {
            m_InspectorController = new InspectorController(CommandDispatcher, GraphView, this);
            m_BlackboardController = new BlackboardController(CommandDispatcher, GraphView, this);
            m_PreviewController = new PreviewController(CommandDispatcher, GraphView, this);
        }

        protected override void OnEnable()
        {
            EditorToolName = "Shader Graph";
            WithSidePanel = false;

            base.OnEnable();

            // Needed to ensure that graph view takes up full window when overlay canvas is present
            rootVisualElement.style.position = new StyleEnum<Position>(Position.Absolute);
            rootVisualElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            rootVisualElement.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            InitializeSubWindows();

            m_BlackboardController.InitializeWindowPosition();
        }

        protected void OnBecameVisible()
        {
            if (GraphView.GraphModel is ShaderGraphModel shaderGraphModel)
            {
                var shaderGraphState = this.CommandDispatcher.State as ShaderGraphState;
                shaderGraphState?.GraphPreviewState.SetGraphModel(shaderGraphModel);
            }

        }

        protected override void Update()
        {
            base.Update();

            CommandDispatcher.Dispatch(new GraphWindowTickCommand());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
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
