using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphViewStateObserver : StateObserver
    {
        PreviewManager m_PreviewManagerInstance;
        GraphViewStateComponent m_GraphViewStateComponent;

        public GraphViewStateObserver(GraphViewStateComponent graphViewStateComponent, PreviewManager previewManager) : base(new [] {graphViewStateComponent}, new [] {graphViewStateComponent})
        {
            m_PreviewManagerInstance = previewManager;
            m_GraphViewStateComponent = graphViewStateComponent;
        }

        public override void Observe()
        {
            // Note: These using statements are necessary to increment last observed version
            using (var graphViewObservation = this.ObserveState(m_GraphViewStateComponent))
            {
                if (graphViewObservation.UpdateType != UpdateType.None)
                {
                    var changeset = m_GraphViewStateComponent.GetAggregatedChangeset(graphViewObservation.LastObservedVersion);
                    var addedModels = changeset.NewModels;
                    var removedModels = changeset.DeletedModels;

                    foreach (var addedModel in addedModels)
                    {
                        if (addedModel is GraphDataNodeModel graphDataNodeModel)
                        {
                            m_PreviewManagerInstance.OnNodeAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.Guid);
                            using var graphUpdater = m_GraphViewStateComponent.UpdateScope;
                            graphUpdater.MarkChanged(addedModel);
                        }
                    }

                    foreach (var addedModel in removedModels)
                    {
                        if (addedModel is GraphDataNodeModel graphDataNodeModel)
                        {
                            m_PreviewManagerInstance.OnNodeRemoved(graphDataNodeModel.graphDataName);
                        }
                    }
                }
            }
        }
    }
}
