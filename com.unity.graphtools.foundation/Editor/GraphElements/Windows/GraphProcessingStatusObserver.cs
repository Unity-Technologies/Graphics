using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class GraphProcessingStatusObserver : StateObserver
    {
        Label m_StatusLabel;
        ErrorToolbar m_ErrorToolbar;
        GraphProcessingStateComponent m_GraphProcessingStateComponent;

        public GraphProcessingStatusObserver(Label statusLabel, ErrorToolbar errorToolbar, GraphProcessingStateComponent graphProcessingState)
            : base(graphProcessingState)
        {
            m_StatusLabel = statusLabel;
            m_ErrorToolbar = errorToolbar;
            m_GraphProcessingStateComponent = graphProcessingState;
        }

        public override void Observe()
        {
            using (var observation = this.ObserveState(m_GraphProcessingStateComponent))
            {
                if (observation.UpdateType != UpdateType.None)
                {
                    if (m_ErrorToolbar?.panel != null)
                        m_ErrorToolbar?.UpdateUI();

                    m_StatusLabel?.EnableInClassList(
                        GraphViewEditorWindow.graphProcessingPendingUssClassName,
                        m_GraphProcessingStateComponent.GraphProcessingPending);
                }
            }
        }
    }
}
