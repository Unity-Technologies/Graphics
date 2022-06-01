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
                        if (addedModel is GraphDataNodeModel graphDataNodeModel && graphDataNodeModel.HasPreview)
                        {
                            m_PreviewManagerInstance.OnNodeAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.Guid);
                            using var graphUpdater = m_graphModelStateComponent.UpdateScope;
                            graphUpdater.MarkChanged(addedModel);
                        }
                        else if (addedModel is GraphDataEdgeModel graphDataEdgeModel)
                        {
                            var nodeModel = graphDataEdgeModel.ToPort.NodeModel as GraphDataNodeModel;
                            m_PreviewManagerInstance.OnNodeFlowChanged(nodeModel.graphDataName);
                        }
                    }

                    // TODO: (Sai) This is currently handled by `ShaderGraphCommandOverrides.HandleDeleteElements
                    // I think we want it to live here in the long run
                    /*foreach (var removedModel in removedModels)
                    {
                        if (removedModel is GraphDataNodeModel graphDataNodeModel)
                        {
                            m_PreviewManagerInstance.OnNodeRemoved(graphDataNodeModel.graphDataName);
                        }
                        else if (removedModel is GraphDataEdgeModel graphDataEdgeModel)
                        {
                            var nodeModel = graphDataEdgeModel.ToPort.NodeModel as GraphDataNodeModel;
                            m_PreviewManagerInstance.OnNodeFlowChanged(nodeModel.graphDataName);
                        }
                    }*/
                }
            }
        }
    }
}
