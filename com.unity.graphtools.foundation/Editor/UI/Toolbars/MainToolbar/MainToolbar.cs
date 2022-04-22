using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A toolbar to display new/save/minimap/blackboard buttons and other action buttons and information.
    /// </summary>
    public class MainToolbar : Toolbar
    {
        class UpdateObserver : StateObserver
        {
            MainToolbar m_Toolbar;

            ToolStateComponent m_ToolState;

            public UpdateObserver(MainToolbar toolbar, ToolStateComponent toolState)
                : base(toolState)
            {
                m_Toolbar = toolbar;
                m_ToolState = toolState;
            }

            public override void Observe()
            {
                if (m_Toolbar?.panel != null)
                {
                    using (var observation = this.ObserveState(m_ToolState))
                    {
                        if (observation.UpdateType != UpdateType.None)
                        {
                            m_Toolbar.UpdateCommonMenu();
                            m_Toolbar.UpdateBreadcrumbMenu(m_ToolState);
                        }
                    }
                }
            }
        }

        public new static readonly string ussClassName = "ge-main-toolbar";

        UpdateObserver m_UpdateObserver;

        protected ToolbarBreadcrumbs m_Breadcrumb;
        protected ToolbarButton m_NewGraphButton;
        protected ToolbarButton m_SaveAllButton;
        protected ToolbarButton m_BuildAllButton;
        protected ToolbarButton m_ShowMiniMapButton;
        protected ToolbarButton m_ShowBlackboardButton;
        protected ToolbarButton m_OptionsButton;

        public static readonly string NewGraphButton = "newGraphButton";
        public static readonly string SaveAllButton = "saveAllButton";
        public static readonly string BuildAllButton = "buildAllButton";
        public static readonly string ShowMiniMapButton = "showMiniMapButton";
        public static readonly string ShowBlackboardButton = "showBlackboardButton";
        public static readonly string OptionsButton = "optionsButton";

        /// <summary>
        /// Initializes a new instance of the <see cref="MainToolbar"/> class.
        /// </summary>
        /// <param name="graphTool">The <see cref="BaseGraphTool"/> of the toolbar.</param>
        /// <param name="graphView">The graph view to which to attach the toolbar.</param>
        public MainToolbar(BaseGraphTool graphTool, GraphView graphView) : base(graphTool, graphView)
        {
            AddToClassList(ussClassName);

            this.AddStylesheetWithSkinVariants("MainToolbar.uss");

            var tpl = GraphElementHelper.LoadUxml("MainToolbar.uxml");
            tpl.CloneTree(this);

            m_NewGraphButton = this.MandatoryQ<ToolbarButton>(NewGraphButton);
            m_NewGraphButton.tooltip = "New Graph";
            m_NewGraphButton.ChangeClickEvent(OnNewGraphButton);

            m_SaveAllButton = this.MandatoryQ<ToolbarButton>(SaveAllButton);
            m_SaveAllButton.tooltip = "Save All";
            m_SaveAllButton.ChangeClickEvent(OnSaveAllButton);

            m_BuildAllButton = this.MandatoryQ<ToolbarButton>(BuildAllButton);
            m_BuildAllButton.tooltip = "Build All";
            m_BuildAllButton.ChangeClickEvent(OnBuildAllButton);

            m_ShowMiniMapButton = this.MandatoryQ<ToolbarButton>(ShowMiniMapButton);
            m_ShowMiniMapButton.tooltip = "Show MiniMap";
            m_ShowMiniMapButton.ChangeClickEvent(ShowGraphViewToolWindow<GraphViewMinimapWindow>);

            m_ShowBlackboardButton = this.MandatoryQ<ToolbarButton>(ShowBlackboardButton);
            m_ShowBlackboardButton.tooltip = "Show Blackboard";
            m_ShowBlackboardButton.ChangeClickEvent(ShowGraphViewToolWindow<GraphViewBlackboardWindow>);

            m_Breadcrumb = this.MandatoryQ<ToolbarBreadcrumbs>("breadcrumb");

            m_OptionsButton = this.MandatoryQ<ToolbarButton>(OptionsButton);
            m_OptionsButton.tooltip = "Options";
            m_OptionsButton.ChangeClickEvent(OnOptionsButton);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        /// <summary>
        /// AttachToPanelEvent event callback.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnEnterPanel(AttachToPanelEvent e)
        {
            if (m_UpdateObserver == null)
                m_UpdateObserver = new UpdateObserver(this, GraphTool.ToolState);
            GraphTool?.ObserverManager?.RegisterObserver(m_UpdateObserver);
        }

        /// <summary>
        /// DetachFromPanelEvent event callback.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnLeavePanel(DetachFromPanelEvent e)
        {
            GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
        }

        void UpdateBreadcrumbMenu(ToolStateComponent toolState)
        {
            bool isEnabled = toolState.GraphModel != null;
            if (!isEnabled)
            {
                m_Breadcrumb.style.display = DisplayStyle.None;
                return;
            }
            m_Breadcrumb.style.display = StyleKeyword.Null;

            var i = 0;
            var graphModels = toolState.SubGraphStack;
            for (; i < graphModels.Count; i++)
            {
                var label = GetBreadcrumbLabel(toolState, i);
                m_Breadcrumb.CreateOrUpdateItem(i, label, BreadcrumbClickedEvent);
            }

            var newCurrentGraph = GetBreadcrumbLabel(toolState, -1);
            if (newCurrentGraph != null)
            {
                m_Breadcrumb.CreateOrUpdateItem(i, newCurrentGraph, BreadcrumbClickedEvent);
                i++;
            }

            m_Breadcrumb.TrimItems(i);
        }

        /// <summary>
        /// Gets the label text for a breadcrumb.
        /// </summary>
        /// <param name="toolState">The tool state component.</param>
        /// <param name="index">The index of the breadcrumb.</param>
        /// <returns>The label text for the breadcrumb.</returns>
        protected virtual string GetBreadcrumbLabel(ToolStateComponent toolState, int index)
        {
            var graphModels = toolState.SubGraphStack;
            string graphName = null;
            if (index == -1)
            {
                graphName = toolState.GraphModel.GetFriendlyScriptName();
            }
            else if (index >= 0 && index < graphModels.Count)
            {
                graphName = graphModels[index].GetGraphModel().GetFriendlyScriptName();
            }

            return string.IsNullOrEmpty(graphName) ? "<Unknown>" : graphName;
        }

        protected void BreadcrumbClickedEvent(int i)
        {
            OpenedGraph graphToLoad = default;
            var graphModels = GraphTool.ToolState.SubGraphStack;
            if (i < graphModels.Count)
                graphToLoad = graphModels[i];

            OnBreadcrumbClick(graphToLoad, i);
        }

        /// <summary>
        /// Callback for when the user clicks on a breadcrumb element.
        /// </summary>
        /// <param name="graphToLoad">The graph to load.</param>
        /// <param name="breadcrumbIndex">The index of the breadcrumb element clicked.</param>
        protected virtual void OnBreadcrumbClick(OpenedGraph graphToLoad, int breadcrumbIndex)
        {
            if (graphToLoad.GetGraphModel() != null)
                GraphTool?.Dispatch(new LoadGraphCommand(graphToLoad.GetGraphModel(),
                    graphToLoad.BoundObject, LoadGraphCommand.LoadStrategies.KeepHistory, breadcrumbIndex));
        }

        void ShowGraphViewToolWindow<T>() where T : GraphViewToolWindow
        {
            var existingToolWindow = ConsoleWindowBridge.FindBoundGraphViewToolWindow<T>(GraphView);
            if (existingToolWindow == null)
                ConsoleWindowBridge.SpawnAttachedViewToolWindow<T>(GraphView.Window, GraphView);
            else
                existingToolWindow.Focus();
        }

        /// <summary>
        /// Updates the state of the toolbar common buttons.
        /// </summary>
        protected virtual void UpdateCommonMenu()
        {
            bool enabled = GraphTool?.ToolState.GraphModel != null;

            m_NewGraphButton.SetEnabled(enabled);
            m_SaveAllButton.SetEnabled(enabled);

            var toolbarProvider = GraphTool?.GetToolbarProvider();

            if (!(toolbarProvider?.ShowButton(NewGraphButton) ?? true))
            {
                m_NewGraphButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_NewGraphButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(SaveAllButton) ?? true))
            {
                m_SaveAllButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_SaveAllButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(BuildAllButton) ?? false))
            {
                m_BuildAllButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_BuildAllButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(ShowMiniMapButton) ?? false))
            {
                m_ShowMiniMapButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_ShowMiniMapButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(ShowBlackboardButton) ?? false))
            {
                m_ShowBlackboardButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_ShowBlackboardButton.style.display = StyleKeyword.Null;
            }
        }

        void OnNewGraphButton()
        {
            var minimap = ConsoleWindowBridge.FindBoundGraphViewToolWindow<GraphViewMinimapWindow>(GraphView);
            if (minimap != null)
            {
                minimap.Repaint();
            }

            GraphTool?.Dispatch(new UnloadGraphCommand());
        }

        static void OnSaveAllButton()
        {
            AssetDatabase.SaveAssets();
        }

        void OnBuildAllButton()
        {
            try
            {
                GraphTool?.Dispatch(new BuildAllEditorCommand());
            }
            catch (Exception e) // so the button doesn't get stuck
            {
                Debug.LogException(e);
            }
        }

        void OnOptionsButton()
        {
            GenericMenu menu = new GenericMenu();
            GraphView.BuildOptionMenu(menu);
            menu.ShowAsContext();
        }
    }
}
