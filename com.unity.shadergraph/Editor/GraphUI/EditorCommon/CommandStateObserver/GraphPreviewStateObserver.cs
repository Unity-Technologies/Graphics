using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public class GraphPreviewStateObserver : StateObserver<ShaderGraphState>
    {
        public GraphPreviewStateObserver() :
        base(
            new []
            {
                nameof(ShaderGraphState.GraphPreviewState),
                nameof(ShaderGraphState.GraphViewState)
            },
            new []
            {
                nameof(ShaderGraphState.GraphPreviewState)
            })
        {

        }

        protected override void Observe(ShaderGraphState state)
        {
            using var previewObservation = this.ObserveState(state.GraphPreviewState);
            if (previewObservation.UpdateType != UpdateType.None)
            {
                Debug.Log("Observed a change: " + previewObservation);

                // TODO: React to preview state being changed (PreviewMode, preview expand/collapse etc)

            }

            using var graphViewObservation = this.ObserveState(state.GraphViewState);
            if (graphViewObservation.UpdateType != UpdateType.None)
            {
                Debug.Log("Observed a change: " + graphViewObservation);
                // TODO: React to topology and preview property changes to update preview content

                var changeset = state.GraphViewState.GetAggregatedChangeset(graphViewObservation.LastObservedVersion);
                var addedModels = changeset.NewModels;
                foreach (var addedModel in addedModels)
                {
                    if (addedModel is GraphDataNodeModel graphDataNodeModel)
                    {
                        state.GraphPreviewState.OnGraphDataNodeAdded(graphDataNodeModel);
                    }
                }
            }
        }
    }
}
