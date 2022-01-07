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
            }

            using var graphViewObservation = this.ObserveState(state.GraphViewState);
            if (graphViewObservation.UpdateType != UpdateType.None)
            {
                var changeset = state.GraphViewState.GetAggregatedChangeset(graphViewObservation.LastObservedVersion);
                var addedModels = changeset.NewModels;

                using var previewUpdater = state.GraphPreviewState.UpdateScope;
                {
                    foreach (var addedModel in addedModels)
                    {
                        if (addedModel is GraphDataNodeModel graphDataNodeModel)
                        {
                            previewUpdater.GraphDataNodeAdded(graphDataNodeModel);
                        }
                    }
                }
            }
        }
    }
}
