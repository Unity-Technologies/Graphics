using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    /// <summary>
    /// Plugin to show debugging data on the graph.
    /// </summary>
    public class DebugInstrumentationHandler : IPluginHandler
    {
        class LoadAssetObserver : StateObserver
        {
            ToolStateComponent m_ToolState;
            TracingStatusStateComponent m_TracingStatusState;
            TracingControlStateComponent m_TracingControlState;
            TracingDataStateComponent m_TracingDataState;

            /// <summary>
            /// Initializes a new instance of the <see cref="DebugDataObserver" /> class.
            /// </summary>
            public LoadAssetObserver(ToolStateComponent toolState, TracingStatusStateComponent tracingStatusState, TracingControlStateComponent tracingControlState, TracingDataStateComponent tracingDataState)
                : base(new[]
                    {
                        toolState,
                    },
                    new IStateComponent[]
                    {
                        tracingStatusState,
                        tracingStatusState,
                        tracingDataState
                    })
            {
                m_ToolState = toolState;
                m_TracingStatusState = tracingStatusState;
                m_TracingControlState = tracingControlState;
                m_TracingDataState = tracingDataState;
            }

            /// <inheritdoc/>
            public override void Observe()
            {
                using (var obs = this.ObserveState(m_ToolState))
                {
                    if (obs.UpdateType != UpdateType.None)
                    {
                        using (var updater = m_TracingStatusState.UpdateScope)
                        {
                            updater.SaveAndLoadStateForAsset(m_ToolState.AssetModel);
                        }
                        using (var updater = m_TracingControlState.UpdateScope)
                        {
                            updater.SaveAndLoadStateForAsset(m_ToolState.AssetModel);
                        }
                        using (var updater = m_TracingDataState.UpdateScope)
                        {
                            updater.SaveAndLoadStateForAsset(m_ToolState.AssetModel);
                        }
                    }
                }
            }
        }

        const string k_TraceHighlight = "trace-highlight";
        const string k_ExceptionHighlight = "exception-highlight";
        const string k_TraceSecondaryHighlight = "trace-secondary-highlight";
        const int k_UpdateIntervalMs = 10;

        BaseGraphTool m_GraphTool;
        GraphView m_GraphView;
        PlayModeStateChange m_PlayState = PlayModeStateChange.EnteredEditMode;
        Stopwatch m_Stopwatch;

        TracingToolbar m_TimelineToolbar;
        DebugDataObserver m_DebugDataObserver;
        LoadAssetObserver m_LoadGraphObserver;

        public TracingStatusStateComponent TracingStatusState { get; private set; }
        public TracingControlStateComponent TracingControlState { get; private set; }
        public TracingDataStateComponent TracingDataState { get; private set; }

        /// <inheritdoc />
        public void Register(GraphViewEditorWindow window)
        {
            m_GraphTool = window.GraphTool;
            m_GraphView = window.GraphViews.First();

            var assetKey = PersistedState.MakeAssetKey(m_GraphTool.ToolState.AssetModel);

            TracingStatusState = PersistedState.GetOrCreateAssetStateComponent<TracingStatusStateComponent>(default, assetKey);
            m_GraphTool.State.AddStateComponent(TracingStatusState);

            TracingControlState = PersistedState.GetOrCreateAssetStateComponent<TracingControlStateComponent>(default, assetKey);
            m_GraphTool.State.AddStateComponent(TracingControlState);

            TracingDataState = PersistedState.GetOrCreateAssetStateComponent<TracingDataStateComponent>(default, assetKey);
            m_GraphTool.State.AddStateComponent(TracingDataState);

            m_LoadGraphObserver = new LoadAssetObserver(m_GraphTool.ToolState, TracingStatusState, TracingControlState, TracingDataState);
            m_GraphTool.ObserverManager.RegisterObserver(m_LoadGraphObserver);

            m_DebugDataObserver = new DebugDataObserver(this, m_GraphView.GraphViewState, TracingControlState, TracingDataState);
            m_GraphTool.ObserverManager.RegisterObserver(m_DebugDataObserver);

            m_GraphTool.Dispatcher.RegisterCommandHandler<ToolStateComponent, TracingStatusStateComponent, ActivateTracingCommand>(
                ActivateTracingCommand.DefaultCommandHandler, m_GraphTool.ToolState, TracingStatusState);

            EditorApplication.update += OnUpdate;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            ((Stencil)m_GraphTool.ToolState.GraphModel?.Stencil)?.Debugger?.Start(m_GraphTool.ToolState.GraphModel, TracingStatusState.TracingEnabled);

            var root = window.rootVisualElement;
            if (m_TimelineToolbar == null)
            {
                m_TimelineToolbar = root.SafeQ<TracingToolbar>();
                if (m_TimelineToolbar == null)
                {
                    m_TimelineToolbar = new TracingToolbar(m_GraphTool, m_GraphView, this);
                }
            }

            if (m_TimelineToolbar.parent != root)
                root.Insert(1, m_TimelineToolbar);
        }

        /// <inheritdoc />
        public void Unregister()
        {
            m_GraphTool.ObserverManager.UnregisterObserver(m_LoadGraphObserver);
            m_GraphTool.ObserverManager.UnregisterObserver(m_DebugDataObserver);

            ClearHighlights(m_GraphTool.ToolState.GraphModel);
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= OnUpdate;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            ((Stencil)m_GraphTool.ToolState.GraphModel?.Stencil)?.Debugger?.Stop();
            m_TimelineToolbar?.RemoveFromHierarchy();
        }

        /// <inheritdoc />
        public void OptionsMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Dump Frame Trace"), false, DumpFrameTrace);
        }

        // PF FIXME to command
        void DumpFrameTrace()
        {
            var state = m_GraphTool.State;
            var currentGraphModel = m_GraphTool.ToolState.GraphModel;
            var debugger = ((Stencil)currentGraphModel?.Stencil)?.Debugger;
            if (state == null || debugger == null)
                return;

            if (debugger.GetTracingSteps(currentGraphModel, TracingControlState.CurrentTracingFrame,
                TracingControlState.CurrentTracingTarget,
                out var stepList))
            {
                try
                {
                    var searcherItems = stepList.Select(MakeStepItem).ToList();
                    Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcherItems, "Steps", item =>
                    {
                        using (var updater = TracingControlState.UpdateScope)
                        {
                            if (item != null)
                                updater.CurrentTracingStep = ((StepSearcherItem)item).Index;
                        }

                        return true;
                    }, Vector2.zero);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else
                Debug.Log("No frame data");

            SearcherItem MakeStepItem(TracingStep step, int i)
            {
                return new StepSearcherItem(step, i);
            }
        }

        class StepSearcherItem : SearcherItem
        {
            public readonly int Index;

            public StepSearcherItem(TracingStep step, int i) : base(GetName(step), "No help available.")
            {
                Index = i;
            }

            static string GetName(TracingStep step)
            {
                return $"{step.Type} {step.NodeModel} {step.PortModel}";
            }
        }

        // PF FIXME Register plugin instead of using editor update.
        void OnUpdate()
        {
            if (m_TimelineToolbar == null)
                return;

            using (var updater = TracingControlState.UpdateScope)
            {
                if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                {
                    updater.CurrentTracingFrame = Time.frameCount;
                }

                m_TimelineToolbar.UpdateTracingMenu(updater);
            }

            m_TimelineToolbar?.SyncVisible();
        }

        void OnEditorPauseStateChanged(PauseState state)
        {
            // TODO Save tracing data
        }

        void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            m_PlayState = state;

            if (m_PlayState == PlayModeStateChange.ExitingPlayMode)
            {
                ClearHighlights(m_GraphTool.ToolState.GraphModel);
                m_Stopwatch?.Stop();
                m_Stopwatch = null;
            }
        }

        internal void MapDebuggingData(TracingControlStateComponent tracingControlStateComponent, TracingDataStateComponent tracingDataStateComponent, IGraphModel graphModel)
        {
            bool needUpdate = false;
            if (m_Stopwatch == null)
                m_Stopwatch = Stopwatch.StartNew();
            else if (EditorApplication.isPaused || m_Stopwatch.ElapsedMilliseconds > k_UpdateIntervalMs)
            {
                needUpdate = true;
                m_Stopwatch.Restart();
            }

            var debugger = ((Stencil)graphModel?.Stencil)?.Debugger;
            if (tracingControlStateComponent == null || debugger == null)
                return;

            if (needUpdate && debugger.GetTracingSteps(graphModel, tracingControlStateComponent.CurrentTracingFrame,
                tracingControlStateComponent.CurrentTracingTarget,
                out var stepList))
            {
                using (var updater = tracingDataStateComponent.UpdateScope)
                {
                    updater.MaxTracingStep = stepList?.Count ?? 0;
                    updater.DebuggingData = stepList;
                }

                // PF FIXME We are updating and observing tracingDataStateComponent. BAD!
                // PF FIXME HighlightTrace should be an observer on tracing states.
                ClearHighlights(graphModel);
                HighlightTrace(tracingControlStateComponent.CurrentTracingStep, tracingDataStateComponent.DebuggingData);
            }
        }

        void ClearHighlights(IGraphModel graphModel)
        {
            if (graphModel == null)
                return;

            graphModel.DeleteBadgesOfType<DebuggingErrorBadgeModel>();
            graphModel.DeleteBadgesOfType<DebuggingValueBadgeModel>();

            for (var index = 0; index < graphModel.NodeModels.Count; index++)
            {
                var nodeModel = graphModel.NodeModels[index];
                var n = nodeModel.GetUI<Node>(m_GraphView);
                if (n == null)
                    continue;

                n.RemoveFromClassList(k_TraceHighlight);
                n.RemoveFromClassList(k_TraceSecondaryHighlight);
                n.RemoveFromClassList(k_ExceptionHighlight);

                foreach (var p in n.Query<DebuggingPort>().ToList())
                {
                    p.ExecutionPortActive = false;
                }
            }
        }

        void HighlightTrace(int currentTracingStep, IReadOnlyList<TracingStep> graphDebuggingData)
        {
            if (graphDebuggingData != null)
            {
                if (currentTracingStep < 0 || currentTracingStep >= graphDebuggingData.Count)
                {
                    foreach (TracingStep step in graphDebuggingData)
                    {
                        AddStyleClassToModel(step, k_TraceHighlight);
                        DisplayStepValues(step);
                    }
                }
                else
                {
                    for (var i = 0; i < currentTracingStep; i++)
                    {
                        var step = graphDebuggingData[i];
                        AddStyleClassToModel(step, k_TraceHighlight);
                        DisplayStepValues(step);
                    }
                }
            }
            m_GraphView.schedule.Execute(() =>
            {
                for (var index = 0; index < m_GraphView.GraphModel.EdgeModels.Count; index++)
                {
                    var edge = m_GraphView.GraphModel.EdgeModels[index];
                    edge.GetUI<Edge>(m_GraphView)?.MarkDirtyRepaint();
                }
            }).StartingIn(1);
        }

        void DisplayStepValues(TracingStep step)
        {
            switch (step.Type)
            {
                case TracingStepType.ExecutedNode:
                    // Do Nothing, already handled in HighlightTrace()
                    break;
                case TracingStepType.TriggeredPort:
                    var p = step.PortModel.GetUI<DebuggingPort>(m_GraphView);
                    if (p != null)
                        p.ExecutionPortActive = true;
                    break;
                case TracingStepType.WrittenValue:
                    step.NodeModel.GraphModel.AddBadge(new DebuggingValueBadgeModel(step));
                    break;
                case TracingStepType.ReadValue:
                    step.NodeModel.GraphModel.AddBadge(new DebuggingValueBadgeModel(step));
                    break;
                case TracingStepType.Error:
                    step.NodeModel.GraphModel.AddBadge(new DebuggingErrorBadgeModel(step));
                    break;
            }

            var hasProgress = step.NodeModel as IHasProgress;
            if (hasProgress?.HasProgress ?? false)
            {
                var node = step.NodeModel.GetUI<CollapsibleInOutNode>(m_GraphView);
                if (node != null)
                    node.Progress = step.Progress;
            }
        }

        void AddStyleClassToModel(TracingStep step, string highlightStyle)
        {
            var node = step.NodeModel.GetUI<Node>(m_GraphView);
            if (step.NodeModel != null && node != null)
            {
                // TODO TRACING errors
                // if (step.type == DebuggerTracer.EntityFrameTrace.StepType.Exception)
                // {
                //     ui.AddToClassList(k_ExceptionHighlight);
                //
                //     if (m_PauseState == PauseState.Paused || m_PlayState == PlayModeStateChange.EnteredEditMode)
                //     {
                //         ((VseGraphView)m_GraphView).UIController.AttachErrorBadge(ui, step.errorMessage, SpriteAlignment.TopLeft);
                //     }
                // }
                // else
                {
                    node.AddToClassList(highlightStyle);
                }
            }
        }
    }
}
