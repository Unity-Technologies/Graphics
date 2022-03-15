using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphViewStateObserver : StateObserver
    {
        PreviewManager m_PreviewManagerInstance;
        GraphModelStateComponent m_graphModelStateComponent;

        public GraphViewStateObserver(GraphModelStateComponent graphModelStateComponent, PreviewManager previewManager) : base(new [] {graphModelStateComponent}, new [] {graphModelStateComponent})
        {
            m_PreviewManagerInstance = previewManager;
            m_graphModelStateComponent = graphModelStateComponent;
        }

        public override void Observe()
        {
            // Note: These using statements are necessary to increment last observed version
            using (var graphViewObservation = this.ObserveState(m_graphModelStateComponent))
            {
                if (graphViewObservation.UpdateType != UpdateType.None)
                {
                    var changeset = m_graphModelStateComponent.GetAggregatedChangeset(graphViewObservation.LastObservedVersion);
                    var addedModels = changeset.NewModels;
                    var removedModels = changeset.DeletedModels;

                    foreach (var addedModel in addedModels)
                    {
                        if (addedModel is GraphDataNodeModel graphDataNodeModel)
                        {
                            m_PreviewManagerInstance.OnNodeAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.Guid);
                            using var graphUpdater = m_graphModelStateComponent.UpdateScope;
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
