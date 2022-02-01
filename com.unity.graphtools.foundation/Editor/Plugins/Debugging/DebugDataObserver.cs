using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    /// <summary>
    /// An observer that updates debug data.
    /// </summary>
    class DebugDataObserver : StateObserver
    {
        DebugInstrumentationHandler m_DebugInstrumentationHandler;

        GraphViewStateComponent m_GraphViewState;
        TracingControlStateComponent m_TracingControlState;
        TracingDataStateComponent m_TracingDataState;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugDataObserver" /> class.
        /// </summary>
        public DebugDataObserver(DebugInstrumentationHandler handler, GraphViewStateComponent graphViewState, TracingControlStateComponent tracingControlState, TracingDataStateComponent tracingDataState)
            : base(new IStateComponent[]
                {
                    graphViewState,
                    tracingControlState
                },
                new[]
                {
                    tracingDataState
                })
        {
            m_DebugInstrumentationHandler = handler;
            m_GraphViewState = graphViewState;
            m_TracingControlState = tracingControlState;
            m_TracingDataState = tracingDataState;
        }

        /// <inheritdoc/>
        public override void Observe()
        {
            using (var gvObservation = this.ObserveState(m_GraphViewState))
            using (var tsObservation = this.ObserveState(m_TracingControlState))
            {
                var updateType = gvObservation.UpdateType.Combine(tsObservation.UpdateType);

                if (updateType != UpdateType.None)
                    m_DebugInstrumentationHandler.MapDebuggingData(m_TracingControlState, m_TracingDataState, m_GraphViewState.GraphModel);
            }
        }
    }
}
