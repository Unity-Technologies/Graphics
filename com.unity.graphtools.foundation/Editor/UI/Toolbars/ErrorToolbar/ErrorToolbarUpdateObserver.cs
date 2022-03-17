using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Observer that updates error toolbar elements.
    /// </summary>
    public class ErrorToolbarUpdateObserver : StateObserver
    {
        IToolbarElement m_ToolbarElement;

        ToolStateComponent m_ToolState;
        GraphProcessingStateComponent m_GraphProcessingState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorToolbarUpdateObserver"/> class.
        /// </summary>
        /// <param name="element">The element to update.</param>
        /// <param name="toolState">The tool state.</param>
        /// <param name="graphProcessingState">The graph processing state.</param>
        public ErrorToolbarUpdateObserver(IToolbarElement element, ToolStateComponent toolState, GraphProcessingStateComponent graphProcessingState)
            : base(toolState, graphProcessingState)
        {
            m_ToolbarElement = element;
            m_ToolState = toolState;
            m_GraphProcessingState = graphProcessingState;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var toolObservation = this.ObserveState(m_ToolState))
            using (var processingObservation = this.ObserveState(m_GraphProcessingState))
            {
                var updateType = toolObservation.UpdateType.Combine(processingObservation.UpdateType);
                if (updateType != UpdateType.None)
                {
                    m_ToolbarElement.Update();
                }
            }
        }
    }
}
