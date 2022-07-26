using System;
using System.Linq;
using System.Windows.Input;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Toolbars;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphGraphTool : BaseGraphTool
    {
        public static readonly string toolName = "Shader Graph";

        public ShaderGraphGraphTool()
        {
            Name = toolName;
        }

        PreviewManager m_PreviewManager;
        ShaderGraphEditorWindow m_EditorWindow;

        public bool wasGraphSaved { get; set; }

        // Get any dependencies that we need
        // (can't use constructor cause GTF uses a helper to initialize the GraphTool that abstracts it away)
        public void Initialize(
            PreviewManager previewManager,
            ShaderGraphEditorWindow editorWindow)
        {
            m_PreviewManager = previewManager;
            m_EditorWindow = editorWindow;
        }

        /// <summary>
        /// Gets the toolbar provider for the main toolbar.
        /// </summary>
        /// <remarks>Use this method to get the provider for the <see cref="ShaderGraphMainToolbar"/>.</remarks>
        /// <returns>The toolbar provider for the main toolbar.</returns>
        public override IToolbarProvider GetToolbarProvider()
        {
            return new ShaderGraphMainToolbarProvider();
        }

        protected override void InitDispatcher()
        {
            Dispatcher = new ShaderGraphCommandDispatcher(HandleGraphLoad, HandleUndoRedo);
        }

        T GetStateComponentOfType<T>()
        {
            var stateComponents = ToolState.State.AllStateComponents;
            return (T)stateComponents.FirstOrDefault(stateComponent => stateComponent is T);
        }

        internal void HandleUndoRedo(UndoableCommand commandBeingUndoneRedone)
        {
            var graphModelStateComponent = GetStateComponentOfType<GraphModelStateComponent>();
            var shaderGraphModel = m_EditorWindow.Asset.GraphModel as ShaderGraphModel;

            m_PreviewManager.UpdateReferencesAfterUndoRedo(graphModelStateComponent, shaderGraphModel);

            // Handling nodes being added/removed
            m_PreviewManager.PostUndoRedoConsistencyCheck();

            switch (commandBeingUndoneRedone)
            {
                // Handling edge connections that may have changed
                case CreateEdgeCommand createEdgeCommand:
                    var graphDataNodeModel = createEdgeCommand.ToPortModel.NodeModel as GraphDataNodeModel;
                    m_PreviewManager.OnNodeFlowChanged(graphDataNodeModel.graphDataName);
                    break;

                // GTF is weird and has multiple commands for delete handling across nodes, edges, blackboard items etc...
                case DeleteEdgeCommand deleteEdgeCommand:
                    foreach (var edgeModel in deleteEdgeCommand.Models)
                    {
                        var toPortNodeModel = edgeModel.ToPort.NodeModel as GraphDataNodeModel;
                        m_PreviewManager.OnNodeFlowChanged(toPortNodeModel.graphDataName);
                    }
                    break;

                // Hence we need to respond to both types of commands for full coverage
                case DeleteElementsCommand deleteElementsCommand:
                    foreach (var model in deleteElementsCommand.Models)
                    {
                        if (model is GraphDataEdgeModel graphDataEdgeModel)
                        {
                            var toPortNodeModel = graphDataEdgeModel.ToPort.NodeModel as GraphDataNodeModel;
                            m_PreviewManager.OnNodeFlowChanged(toPortNodeModel.graphDataName);
                        }
                    }
                    break;

                // Handling value changes in ports
                case UpdateConstantValueCommand updateConstantValueCommand:
                    m_PreviewManager.HandleConstantValueUndoRedo(updateConstantValueCommand.Constant as BaseShaderGraphConstant);
                    break;

                case ChangeTargetSettingsCommand changeTargetSettingsCommand:
                    m_PreviewManager.HandleTargetSettingsChanged();
                    break;
            }

            m_PreviewManager.ReassignAllNodePreviewTextures();
        }

        internal void HandleGraphLoad(LoadGraphCommand loadGraphCommand)
        {
            var windowClosedInDirtyState = m_EditorWindow.wasWindowClosedInDirtyState;
            BlackboardView blackboardView = null;
            MainPreviewView mainPreviewView = null;

            m_EditorWindow.TryGetOverlay(SGBlackboardOverlay.k_OverlayID, out var blackboard);
            if (blackboard is SGBlackboardOverlay blackboardOverlay)
                blackboardView = blackboardOverlay.BlackboardView;

            m_EditorWindow.TryGetOverlay(PreviewOverlay.k_OverlayID, out var preview);
            if (preview is PreviewOverlay previewOverlay)
                mainPreviewView = previewOverlay.MainPreviewView;

            var graphView = m_EditorWindow.GraphView;

            var graphModelStateComponent = GetStateComponentOfType<GraphModelStateComponent>();
            var graphViewStateComponent = GetStateComponentOfType<GraphViewStateComponent>();
            var selectionStateComponent = GetStateComponentOfType<SelectionStateComponent>();
            var undoStateComponent = GetStateComponentOfType<UndoStateComponent>();

            var shaderGraphModel = loadGraphCommand.GraphModel as ShaderGraphModel;
            // If handling a fresh load and not a reimport after a save
            if (!wasGraphSaved)
            {
                m_PreviewManager.Initialize(
                    graphModelStateComponent,
                    shaderGraphModel,
                    mainPreviewView,
                    windowClosedInDirtyState);

                ShaderGraphCommandsRegistrar.RegisterCommandHandlers(
                    graphView,
                    blackboardView,
                    m_PreviewManager,
                    shaderGraphModel,
                    Dispatcher,
                    graphModelStateComponent,
                    graphViewStateComponent,
                    selectionStateComponent,
                    undoStateComponent);
            }
            else // if handling a graph reimport due to a save
            {
                m_PreviewManager.HandleGraphReload(graphModelStateComponent, shaderGraphModel, mainPreviewView);
            }

            wasGraphSaved = false;
        }

        protected override IOverlayToolbarProvider CreateToolbarProvider(string toolbarId)
        {
            switch (toolbarId)
            {
                case MainOverlayToolbar.toolbarId:
                    return new ShaderGraphMainToolbarProvider();
                case BreadcrumbsToolbar.toolbarId:
                    return new BreadcrumbsToolbarProvider();
                case PanelsToolbar.toolbarId:
                    return new SGPanelsToolbarProvider();
                case OptionsMenuToolbar.toolbarId:
                    return new OptionsToolbarProvider();
                default:
                    return null;
            }
        }
    }
}
