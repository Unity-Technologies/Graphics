using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.CommandStateObserver;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphEditorWindow : GraphViewEditorWindow
    {
        ShaderGraphGraphTool m_GraphTool;

        MainPreviewView m_MainPreviewView;
        public MainPreviewView MainPreviewView => m_MainPreviewView;

        protected PreviewUpdateDispatcher m_PreviewUpdateDispatcher = new();

        // Made internal for tests
        internal PreviewUpdateDispatcher previewUpdateDispatcher => m_PreviewUpdateDispatcher;

        // We store the preview size that the overlays load in with here to pass to the preview systems
        Vector2 m_PreviewSize;

        protected BlackboardView m_BlackboardView;

        // We setup a reference to the MainPreview when the overlay containing it is created
        // We do this because the resources needed to initialize the preview are not available at overlay creation time
        internal void SetMainPreviewReference(MainPreviewView mainPreviewView)
        {
            m_MainPreviewView = mainPreviewView;
            // This handles when the main preview overlay is undocked and moved around or re-docked
            if(GraphView?.GraphModel is SGGraphModel shaderGraphModel)
                SetDefaultMainPreviewUpdateListener(shaderGraphModel);
        }

        internal GraphAsset Asset => m_GraphTool.ToolState.CurrentGraph.GetGraphAsset();

        // This flag gets set by tests to close the editor window directly without prompts to save the dirty asset
        internal bool shouldCloseWindowNoPrompt = false;

        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<ShaderGraphEditorWindow>(ShaderGraphGraphTool.toolName);
        }

        // TODO: Re-enable when GTF fixes onboarding provider handling
        //[MenuItem("Window/Shaders/ShaderGraph", false)]
        //public static void ShowWindow()
        //{
        //    Type sceneView = typeof(SceneView);
        //    GetWindow<ShaderGraphEditorWindow>(sceneView);
        //}

        void InitializeOverlayWindows()
        {
            TryGetOverlay("gtf-inspector", out var gtfInspector);
            overlayCanvas.Remove(gtfInspector);

            TryGetOverlay("gtf-blackboard", out var gtfBlackboard);
            overlayCanvas.Remove(gtfBlackboard);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            InitializeOverlayWindows();
        }

        protected override void OnDisable()
        {
            Cleanup();
            base.OnDisable();
        }

        // Made internal for tests to access and call in case of a test failing and exceptions being thrown
        internal void Cleanup()
        {
            m_PreviewUpdateDispatcher.Cleanup();
        }

        // returns true when the user is OK with closing the window or application (either they've saved dirty content, or are ok with losing it)
        // returns false when the user wants to cancel closing the window or application
        bool PromptSaveIfDirtyOnQuit()
        {
            if (Asset == null)
                return true;

            if (isAssetDirty)
            {
                // TODO (Sai): Implement checking for whether the asset file has been deleted on disk and if so provide feedback to user and allow them to save state of current graph/discard etc
                // Work item for this: https://jira.unity3d.com/browse/GSG-933
                //if (!DoesAssetFileExist())
                //    return DisplayDeletedFromDiskDialog();

                // If there are unsaved modifications, ask the user what to do.
                // If the editor has already handled this check we'll no longer have unsaved changes
                // (either they saved or they discarded, both of which will set hasUnsavedChanges to false).
                if (isAssetDirty)
                {
                    int option = EditorUtility.DisplayDialogComplex(
                        "Shader Graph Has Been Modified",
                        GetSaveChangesMessage(),
                        "Save", "Cancel", "Discard Changes");

                    if (option == 0) // save
                    {
                        GraphAssetUtils.SaveOpenGraphAsset(GraphTool);
                        return true;
                    }
                    else if (option == 1) // cancel (or escape/close dialog)
                    {
                        // Should cancel save the current state of graph before closing out? cause we can't halt the window close
                        return false;
                    }
                    else if (option == 2) // discard
                    {
                        return true;
                    }
                }
            }

            return true;
        }

        bool isAssetDirty => Asset.Dirty;

        private string GetSaveChangesMessage()
        {
            return "Do you want to save the changes you made in the Shader Graph?\n\n" +
                m_GraphTool.ToolState.CurrentGraph.GetGraphAsset() +
                "\n\nYour changes will be lost if you don't save them.";
        }

        bool DisplayDeletedFromDiskDialog()
        {
            bool saved = false;
            bool okToClose = false;

            var originalAssetPath = m_GraphTool.ToolState.CurrentGraph.GetGraphAsset();
            int option = EditorUtility.DisplayDialogComplex(
                "Graph removed from project",
                "The file has been deleted or removed from the project folder.\n\n" +
                originalAssetPath +
                "\n\nWould you like to save your Graph Asset?",
                "Save As...", "Cancel", "Discard Graph and Close Window");

            if (option == 0)
            {
                var savedPath = GraphAssetUtils.SaveOpenGraphAssetAs(GraphTool);
                if (savedPath != null)
                {
                    saved = true;
                }
            }
            else if (option == 2)
            {
                okToClose = true;
            }
            else if (option == 1)
            {
                // continue in deleted state...
            }

            return (saved || okToClose);
        }

        bool DoesAssetFileExist()
        {
            var assetPath = m_GraphTool.ToolState.CurrentGraph.GetGraphAssetPath();
            return File.Exists(assetPath);
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            m_GraphTool = CsoTool.Create<ShaderGraphGraphTool>();
            return m_GraphTool;
        }

        protected override GraphView CreateGraphView()
        {
            GraphTool.Preferences.SetInitialItemLibrarySize(ItemLibraryService.Usage.CreateNode, new Vector2(425, 100), 2.0f);

            var shaderGraphView = new ShaderGraphView(this, GraphTool, GraphTool.Name, m_PreviewUpdateDispatcher);
            return shaderGraphView;
        }

        public static T GetStateComponentOfType<T>(IState stateStore) where T : class
        {
            return stateStore.AllStateComponents.FirstOrDefault(stateComponent => stateComponent is T) as T;
        }

        static void RegisterBlackboardOverrideCommandHandlers(BlackboardView blackboardView, IState stateStore)
        {
            var undoStateComponent = GetStateComponentOfType<UndoStateComponent>(stateStore);
            var graphModelStateComponent = blackboardView.BlackboardViewModel.GraphModelState;
            var selectionStateComponent = blackboardView.BlackboardViewModel.SelectionState;

            // Note: Currently we don't have any blackboard overrides but this is a space for it
        }

        public override BlackboardView CreateBlackboardView()
        {
            if (GraphView != null)
            {
                m_BlackboardView = new BlackboardView(this, GraphView,
                    (view, model) => new SGBlackboardViewSelection(view, model));

                // Register blackboard commands
                RegisterBlackboardOverrideCommandHandlers(m_BlackboardView, m_GraphTool.State);
            }
            return m_BlackboardView;
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new ShaderGraphOnboardingProvider());
            return new BlankPage(GraphTool, onboardingProviders);
        }

        protected override bool CanHandleAssetType(GraphAsset asset)
        {
            return asset is ShaderGraphAsset;
        }

        // Entry point for initializing any systems that depend on the graph model
        public void HandleGraphLoad(SGGraphModel graphModel, IPreviewUpdateReceiver previewUpdateReceiver)
        {
            // Can be null when the editor window is opened to the onboarding page
            if (graphModel == null)
                return;

            TryGetOverlay(PreviewOverlay.k_OverlayID, out var overlay);
            if (overlay is PreviewOverlay previewOverlay)
            {
                m_PreviewSize = previewOverlay.size;
            }

            graphModel.CreateUIData();
            graphModel.MainPreviewData.mainPreviewSize = m_PreviewSize;

            m_PreviewUpdateDispatcher.Initialize(this, graphModel, previewUpdateReceiver);

            SetDefaultMainPreviewUpdateListener(graphModel);

            ShaderGraphCommands.RegisterCommandHandlers(m_GraphTool, m_PreviewUpdateDispatcher);

            PreviewCommands.RegisterCommandHandlers(
                GraphTool,
                m_PreviewUpdateDispatcher,
                graphModel,
                GraphTool,
                GraphView.GraphViewModel);

            // TODO (Joe): With this, we can remove old calls to DefineNode in places the UI expected nodes to reconcretize.
            graphModel.GraphHandler.AddBuildCallback(nodeHandler =>
            {
                var nodeLocalId = nodeHandler.ID.LocalPath;
                var guid = new SerializableGUID(nodeLocalId);

                if (!graphModel.TryGetModelFromGuid<SGNodeModel>(guid, out var nodeModel) || nodeModel == null)
                {
                    return;
                }

                nodeModel.DefineNode();
            });
        }

        /// <summary>
        /// This gets the main fragment context node from the graph and sets it as the source of data for the main preview
        /// </summary>
        /// <param name="graphModel"></param>
        void SetDefaultMainPreviewUpdateListener(SGGraphModel graphModel)
        {
            // Give main preview its view-model
            foreach (var nodeModel in graphModel.NodeModels)
            {
                if (nodeModel is SGContextNodeModel contextNodeModel && contextNodeModel.IsMainContextNode())
                    m_MainPreviewView.SetTargetPreviewUpdateListener(contextNodeModel);
            }
        }

        protected override void Update()
        {
            if (Asset == null)
                return;

            base.Update();

            m_PreviewUpdateDispatcher.Update();
        }
    }
}
