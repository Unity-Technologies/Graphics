using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A state component holding data related to graph processing.
    /// </summary>
    public class GraphProcessingStateComponent : StateComponent<GraphProcessingStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="GraphProcessingStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<GraphProcessingStateComponent>
        {
            /// <inheritdoc  cref="GraphProcessingStateComponent.GraphProcessingPending"/>
            public bool GraphProcessingPending
            {
                set
                {
                    if (m_State.GraphProcessingPending != value)
                    {
                        m_State.GraphProcessingPending = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <summary>
            /// Sets the result of the graph processing.
            /// </summary>
            /// <param name="results">The results.</param>
            /// <param name="errorModels">The error to display.</param>
            public void SetResults(GraphProcessingResult results, IEnumerable<IGraphProcessingErrorModel> errorModels)
            {
                m_State.RawResults = results;
                m_State.m_Errors?.Clear();
                if (errorModels != null)
                    m_State.m_Errors?.AddRange(errorModels);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Clears the graph processing results stored in the component.
            /// </summary>
            public void Clear()
            {
                m_State.RawResults = null;
                m_State.m_Errors = null;
                m_State.GraphProcessingPending = false;
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        List<IGraphProcessingErrorModel> m_Errors;

        /// <summary>
        /// Whether we are waiting for the graph processing to begin.
        /// </summary>
        public bool GraphProcessingPending { get; private set; }

        /// <summary>
        /// The graph processing results.
        /// </summary>
        public GraphProcessingResult RawResults { get; private set; }

        /// <summary>
        /// The errors to display.
        /// </summary>
        public IReadOnlyList<IGraphProcessingErrorModel> Errors => m_Errors ??= new List<IGraphProcessingErrorModel>();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RawResults = null;
                m_Errors = null;
            }
        }
    }
}
