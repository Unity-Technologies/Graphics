using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

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
            /// Sets the results of the graph processing.
            /// </summary>
            /// <param name="results">The results.</param>
            /// <param name="errorModels">The error to display.</param>
            public void SetResults(IReadOnlyList<GraphProcessingResult> results, IEnumerable<IGraphProcessingErrorModel> errorModels)
            {
                m_State.RawResults = results;
                m_State.RawErrors = m_State.RawResults?.SelectMany(r => r.Errors).ToList();

                if (m_State.m_Errors == null)
                {
                    m_State.m_Errors = new List<IGraphProcessingErrorModel>();
                }
                else
                {
                    m_State.m_Errors.Clear();
                }

                if (errorModels != null)
                    m_State.m_Errors.AddRange(errorModels);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Clears the graph processing results stored in the component.
            /// </summary>
            public void Clear()
            {
                m_State.RawResults = null;
                m_State.RawErrors = null;
                m_State.m_Errors.Clear();
                m_State.GraphProcessingPending = false;
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        List<IGraphProcessingErrorModel> m_Errors = new List<IGraphProcessingErrorModel>();

        /// <summary>
        /// Whether we are waiting for the graph processing to begin.
        /// </summary>
        public bool GraphProcessingPending { get; private set; }

        /// <summary>
        /// The graph processing results.
        /// </summary>
        public IReadOnlyList<GraphProcessingResult> RawResults { get; private set; }

        /// <summary>
        /// All the errors from the <see cref="RawResults"/>.
        /// </summary>
        public IReadOnlyList<GraphProcessingError> RawErrors { get; private set; }

        /// <summary>
        /// The errors to display.
        /// </summary>
        public IReadOnlyList<IGraphProcessingErrorModel> Errors => m_Errors;

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RawResults = null;
                RawErrors = null;
                m_Errors.Clear();
            }
        }
    }
}
