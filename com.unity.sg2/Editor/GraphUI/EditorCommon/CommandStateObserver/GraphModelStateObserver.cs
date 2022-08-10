using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Watches the graph model for changes and notifies the preview manager when changes occur
    /// Also handles notifying the graph model for post-copy edge
    /// </summary>
    public class GraphModelStateObserver : StateObserver
    {
        PreviewManager m_PreviewManagerInstance;
        PreviewUpdateDispatcher m_PreviewUpdateDispatcher = new();
        PreviewStateComponent m_PreviewStateComponent;
        GraphModelStateComponent m_GraphModelStateComponent;

        public GraphModelStateObserver(
            GraphModelStateComponent graphModelStateComponent,
            PreviewManager previewManager,
            PreviewStateComponent previewStateComponent)
            : base(new [] {graphModelStateComponent},
                new IStateComponent [] { graphModelStateComponent, previewStateComponent})
        {
            m_PreviewManagerInstance = previewManager;
            m_GraphModelStateComponent = graphModelStateComponent;
            m_PreviewStateComponent = previewStateComponent;
        }

        public override void Observe()
        {
            // Note: These using statements are necessary to increment last observed version
            using (var graphViewObservation = this.ObserveState(m_GraphModelStateComponent))
            {
                if (graphViewObservation.UpdateType != UpdateType.None
                    && m_GraphModelStateComponent.GraphModel is ShaderGraphModel shaderGraphModel)
                {
                    var changeset = m_GraphModelStateComponent.GetAggregatedChangeset(graphViewObservation.LastObservedVersion);
                    var addedModels = changeset.NewModels;
                    var removedModels = changeset.DeletedModels;

                    foreach (var addedModel in addedModels)
                    {
                        if (addedModel is GraphDataNodeModel graphDataNodeModel && graphDataNodeModel.HasPreview)
                        {
                            m_PreviewManagerInstance.OnNodeAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.Guid);
                            using (var previewStateUpdater = m_PreviewStateComponent.UpdateScope)
                            {
                                previewStateUpdater.RegisterNewListener(graphDataNodeModel.graphDataName, graphDataNodeModel);
                            }

                        }
                        else if (addedModel is GraphDataEdgeModel graphDataEdgeModel)
                        {
                            var nodeModel = graphDataEdgeModel.ToPort.NodeModel as GraphDataNodeModel;
                            m_PreviewManagerInstance.OnNodeFlowChanged(nodeModel.graphDataName);
                        }
                    }

                    shaderGraphModel.HandlePostDuplicationEdgeFixup();

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
