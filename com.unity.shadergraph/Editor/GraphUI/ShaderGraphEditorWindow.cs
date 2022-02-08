using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphEditorWindow : GraphViewEditorWindow
    {
        ShaderGraphGraphTool m_GraphTool;

        PreviewManager m_PreviewManager;

        GraphViewStateObserver m_GraphViewStateObserver;

        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<ShaderGraphEditorWindow>(ShaderGraphGraphTool.toolName);
        }

        [MenuItem("Window/Shaders/ShaderGraph", false)]
        public static void ShowWindow()
        {
            GetWindow<ShaderGraphEditorWindow>();
        }

        protected override void OnEnable()
        {
            WithSidePanel = false;
            base.OnEnable();

            // Needed to ensure that graph view takes up full window when overlay canvas is present
            rootVisualElement.style.position = new StyleEnum<Position>(Position.Absolute);
            rootVisualElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            rootVisualElement.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            m_GraphTool = CsoTool.Create<ShaderGraphGraphTool>();
            return m_GraphTool;
        }

        protected override GraphView CreateGraphView()
        {
            GraphTool.Preferences.SetInitialSearcherSize(SearcherService.Usage.k_CreateNode, new Vector2(425, 100), 2.0f);

            var shaderGraphView = new ShaderGraphView(this, GraphTool, GraphTool.Name);
            m_PreviewManager = new PreviewManager(shaderGraphView.GraphViewState);
            m_GraphViewStateObserver = new GraphViewStateObserver(shaderGraphView.GraphViewState, m_PreviewManager);

            GraphTool.ObserverManager.RegisterObserver(m_GraphViewStateObserver);

            // TODO (Brett) Command registration or state handler creation belongs here.
            // Example: graphView.RegisterCommandHandler<SetNumberOfInputPortCommand>(SetNumberOfInputPortCommand.DefaultCommandHandler);

            return shaderGraphView;
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new ShaderGraphOnboardingProvider());
            return new BlankPage(GraphTool?.Dispatcher, onboardingProviders);
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return asset is ShaderGraphAssetModel;
        }

        protected void OnBecameVisible()
        {
            m_PreviewManager.Initialize(GraphTool.ToolState.GraphModel as ShaderGraphModel);

            ShaderGraphCommandsRegistrar.RegisterCommandHandlers(GraphTool, GraphView, m_PreviewManager, GraphTool.Dispatcher);
        }

        protected override MainToolbar CreateMainToolbar()
        {
            return new ShaderGraphMainToolbar(GraphTool, GraphView);
        }

        protected override void Update()
        {
            base.Update();

            m_PreviewManager.Update();
        }

        // ----------
        // Commented out because compatible with a previous GTF
        // ----------

        // InspectorController m_InspectorController;
        // ModelInspectorView m_InspectorView => m_InspectorController?.View;
        // BlackboardController m_BlackboardController;
        // Blackboard m_BlackboardView => m_BlackboardController?.View;
        // PreviewController m_PreviewController;
        // Preview m_Preview => m_PreviewController?.View;
        // static GraphWindowTickCommand s_CachedGraphWindowTickCommand = new ();

        //public VisualElement GetGraphSubWindow<T>()
        //{
        //    if (typeof(T) == typeof(Blackboard))
        //        return m_BlackboardView;
        //    if (typeof(T) == typeof(ModelInspectorView))
        //        return m_InspectorView;
        //    if (typeof(T) == typeof(Preview))
        //        return m_Preview;
        //    return null;
        //}

        //void InitializeSubWindows()
        //{
        //    m_InspectorController = new InspectorController((CommandDispatcher)GraphTool.Dispatcher, GraphView, this);
        //    m_BlackboardController = new BlackboardController((CommandDispatcher)GraphTool.Dispatcher, GraphView, this);
        //    m_PreviewController = new PreviewController((CommandDispatcher)GraphTool.Dispatcher, GraphView, this);
        //}

        ////protected void OnBecameVisible()
        ////{
        ////    if (GraphView.GraphModel is ShaderGraphModel shaderGraphModel)
        ////    {
        ////        var shaderGraphState = this.CommandDispatcher.State as ShaderGraphState;
        ////        shaderGraphState?.GraphPreviewState.SetGraphModel(shaderGraphModel);
        ////    }
        ////}

        //protected override GraphToolState CreateInitialState()
        //{
        //    var prefs = Preferences.CreatePreferences(EditorToolName);
        //    return new ShaderGraphState(GUID, prefs);
        //}
    }
}
