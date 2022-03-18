using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class ResultObserver : StateObserver
    {
        GraphProcessingStateComponent m_GraphProcessingState;
        IToolbarElement m_ToolbarElement;

        public ResultObserver(GraphProcessingStateComponent graphProcessingState, IToolbarElement resultLabel)
            : base(graphProcessingState)
        {
            m_GraphProcessingState = graphProcessingState;
            m_ToolbarElement = resultLabel;
        }

        public override void Observe()
        {
            using (var procesingObservation = this.ObserveState(m_GraphProcessingState))
            {
                if (procesingObservation.UpdateType != UpdateType.None)
                {
                    m_ToolbarElement.Update();
                }
            }
        }
    }
}
