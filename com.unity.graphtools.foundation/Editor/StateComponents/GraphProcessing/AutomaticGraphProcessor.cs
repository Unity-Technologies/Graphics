using System.Diagnostics;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// An observer that automatically processes the graph when it is updated.
    /// </summary>
    /// <remarks>If the preference <see cref="BoolPref.AutoProcess"/> is true,
    /// the graph will only be processed after the mouse stays idle for
    /// <see cref="k_IdleTimeBeforeGraphProcessingMs"/> in edit mode or for
    /// <see cref="k_IdleTimeBeforeGraphProcessingMsPlayMode"/> in play mode.
    /// </remarks>
    public class AutomaticGraphProcessor : StateObserver
    {
        const int k_IdleTimeBeforeGraphProcessingMs = 1000;
        const int k_IdleTimeBeforeGraphProcessingMsPlayMode = 1000;

        readonly Stopwatch m_IdleTimer;
        bool m_LastObservedAutoProcessPref;
        PluginRepository m_PluginRepository;
        GraphViewStateComponent m_GraphViewStateComponent;
        Preferences m_Preferences;
        GraphProcessingStateComponent m_GraphProcessingStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaticGraphProcessor" /> class.
        /// </summary>
        /// <param name="pluginRepository">The plugin repository.</param>
        /// <param name="graphViewStateComponent">The graph view state component.</param>
        /// <param name="preferences">The tool preferences.</param>
        /// <param name="graphProcessingState">The graph processing state.</param>
        public AutomaticGraphProcessor(PluginRepository pluginRepository, GraphViewStateComponent graphViewStateComponent, Preferences preferences, GraphProcessingStateComponent graphProcessingState)
            : base(new IStateComponent[]
                {
                    graphViewStateComponent,
                },
                new[]
                {
                    graphProcessingState
                })
        {
            m_GraphViewStateComponent = graphViewStateComponent;
            m_Preferences = preferences;
            m_GraphProcessingStateComponent = graphProcessingState;

            m_IdleTimer = new Stopwatch();

            m_PluginRepository = pluginRepository;
        }

        /// <inheritdoc/>
        public override void Observe()
        {
            if (m_Preferences.GetBool(BoolPref.AutoProcess))
            {
                if (!m_IdleTimer.IsRunning)
                {
                    ResetTimer();
                }

                if (!m_LastObservedAutoProcessPref)
                    EditorApplication.update += OnUpdate;

                m_LastObservedAutoProcessPref = true;
            }
            else
            {
                if (m_IdleTimer.IsRunning)
                {
                    StopTimer();
                }

                if (m_LastObservedAutoProcessPref)
                    EditorApplication.update -= OnUpdate;

                // Force update if auto-process was just switched off.
                ObserveNow(m_LastObservedAutoProcessPref);
                m_LastObservedAutoProcessPref = false;
            }
        }

        void ObserveIfIdle()
        {
            var elapsedTime = m_IdleTimer.ElapsedMilliseconds;
            if (elapsedTime >= (EditorApplication.isPlaying
                ? k_IdleTimeBeforeGraphProcessingMsPlayMode
                : k_IdleTimeBeforeGraphProcessingMs))
            {
                ResetTimer();
                ObserveNow(false);
            }
            else
            {
                // We only want to display a notification that we will process the graph.
                // We need to check if the state components were modified, but
                // without updating our internal version numbers (they will be
                // updated when we actually process the graph). We use PeekAtState.
                using (var gvObservation = this.PeekAtState(m_GraphViewStateComponent))
                {
                    var gvUpdateType = gvObservation.UpdateType;
                    if (gvUpdateType == UpdateType.Partial)
                    {
                        // Adjust gvUpdateType if there was no modifications on the graph model
                        var changeset = m_GraphViewStateComponent.GetAggregatedChangeset(gvObservation.LastObservedVersion);
                        if (!changeset.NewModels.Any() && !changeset.ChangedModels.Any() && !changeset.DeletedModels.Any())
                            gvUpdateType = UpdateType.None;
                    }

                    var shouldRebuild = gvUpdateType != UpdateType.None;
                    if (m_GraphProcessingStateComponent.GraphProcessingPending != shouldRebuild)
                    {
                        using (var updater = m_GraphProcessingStateComponent.UpdateScope)
                        {
                            updater.GraphProcessingPending = shouldRebuild;
                        }
                    }
                }
            }
        }

        void ObserveNow(bool forceUpdate)
        {
            using (var gvObservation = this.ObserveState(m_GraphViewStateComponent))
            {
                var gvUpdateType = gvObservation.UpdateType;
                if (gvUpdateType == UpdateType.Partial)
                {
                    // Adjust gvUpdateType if there was no modifications on the graph model
                    var changeset = m_GraphViewStateComponent.GetAggregatedChangeset(gvObservation.LastObservedVersion);
                    if (!changeset.NewModels.Any() && !changeset.ChangedModels.Any() && !changeset.DeletedModels.Any())
                        gvUpdateType = UpdateType.None;
                }

                if (forceUpdate || gvUpdateType != UpdateType.None)
                {
                    var results = m_GraphViewStateComponent.GraphModel.ProcessGraph(m_PluginRepository,
                        RequestGraphProcessingOptions.Default);

                    if (results != null || m_GraphProcessingStateComponent.GraphProcessingPending)
                    {
                        using (var updater = m_GraphProcessingStateComponent.UpdateScope)
                        {
                            updater.GraphProcessingPending = false;

                            if (results != null)
                                updater.SetResults(results,
                                    GraphProcessingHelper.GetErrors((Stencil)m_GraphViewStateComponent.GraphModel.Stencil, results));
                        }
                    }
                }
            }
        }

        void OnUpdate()
        {
            ObserveIfIdle();
        }

        /// <summary>
        /// Resets the idle timer.
        /// </summary>
        public void ResetTimer()
        {
            m_IdleTimer.Restart();
        }

        /// <summary>
        /// Stops the idle timer.
        /// </summary>
        public void StopTimer()
        {
            m_IdleTimer.Stop();
        }
    }
}
