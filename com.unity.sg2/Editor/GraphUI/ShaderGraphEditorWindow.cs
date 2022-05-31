using System;
using System.IO;
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

        protected PreviewManager m_PreviewManager;

        internal IGraphAsset Asset => m_GraphTool.ToolState.CurrentGraph.GetGraphAsset();

        protected GraphViewStateObserver m_GraphViewStateObserver;

        MainPreviewView m_MainPreviewView;

        // We setup a reference to the MainPreview when the overlay containing it is created
        // We do this because the resources needed to initialize the preview are not available at overlay creation time
        internal void SetMainPreviewReference(MainPreviewView mainPreviewView)
        {
            m_MainPreviewView = mainPreviewView;
        }

        // This Flag gets set when the editor window is closed with the graph still in a dirty state,
        // letting various sub-systems and the user know on window re-open that the graph is still dirty
        bool m_WasWindowCloseCancelledInDirtyState;

        // This flag gets set by tests to close the editor window directly without prompts to save the dirty asset
        internal bool shouldCloseWindowNoPrompt = false;

        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<ShaderGraphEditorWindow>(ShaderGraphGraphTool.toolName);
        }

        [MenuItem("Window/Shaders/ShaderGraph", false)]
        public static void ShowWindow()
        {
            Type sceneView = typeof(SceneView);
            GetWindow<ShaderGraphEditorWindow>(sceneView);
        }

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

            // Needed to ensure to that on domain reload we go through and actually reinitialize stuff as this flag remains true when reload happens
            // TODO (Sai): Figure out a better place for command handler registration and preview manager initialization
            m_PreviewManager.IsInitialized = false;

            // Needed to ensure that graph view takes up full window when overlay canvas is present
            rootVisualElement.style.position = new StyleEnum<Position>(Position.Absolute);
            rootVisualElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            rootVisualElement.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            InitializeOverlayWindows();
        }

        protected override void OnDisable()
        {
            if (!shouldCloseWindowNoPrompt && !PromptSaveIfDirtyOnQuit())
            {
                // User does not want to close the window.
                // We can't stop the close from this code path though..
                // All we can do is open a new window and transfer our data to the new one to avoid losing it

                var shaderGraphEditorWindow = CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
                if(shaderGraphEditorWindow == null)
                {
                    return;
                }
                shaderGraphEditorWindow.Show();
                shaderGraphEditorWindow.Focus();
                shaderGraphEditorWindow.SetCurrentSelection(Asset, OpenMode.OpenAndFocus);
                // Set this flag in order to let anything that would clear the dirty state know that graph is still dirty
                shaderGraphEditorWindow.m_WasWindowCloseCancelledInDirtyState = true;
            }

            base.OnDisable();
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
            m_GraphTool = CsoTool.Create<ShaderGraphGraphTool>(WindowID);
            return m_GraphTool;
        }

        protected override GraphView CreateGraphView()
        {
            GraphTool.Preferences.SetInitialSearcherSize(SearcherService.Usage.CreateNode, new Vector2(425, 100), 2.0f);

            var shaderGraphView = new ShaderGraphView(this, GraphTool, GraphTool.Name);
            m_PreviewManager = new PreviewManager(shaderGraphView.GraphViewModel.GraphModelState);
            m_GraphViewStateObserver = new GraphViewStateObserver(shaderGraphView.GraphViewModel.GraphModelState, m_PreviewManager);
            GraphTool.ObserverManager.RegisterObserver(m_GraphViewStateObserver);

            return shaderGraphView;
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new ShaderGraphOnboardingProvider());
            return new BlankPage(GraphTool?.Dispatcher, onboardingProviders);
        }

        protected override bool CanHandleAssetType(IGraphAsset asset)
        {
            return asset is ShaderGraphAsset;
        }

        protected override void Update()
        {
            if (Asset == null)
                return;

            base.Update();

            if (m_PreviewManager is { IsInitialized: false })
            {
                m_PreviewManager.Initialize(GraphTool.ToolState.GraphModel as ShaderGraphModel, m_MainPreviewView, m_WasWindowCloseCancelledInDirtyState);
                var shaderGraphModel = GraphTool.ToolState.GraphModel as ShaderGraphModel;
                ShaderGraphCommandsRegistrar.RegisterCommandHandlers(GraphTool, GraphView.GraphViewModel, m_PreviewManager, shaderGraphModel, GraphTool.Dispatcher);
            }

            m_PreviewManager?.Update();
        }
    }
}
