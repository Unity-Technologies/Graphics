using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
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
        GraphModelStateComponent m_GraphModelStateComponent;

        PreviewStateComponent m_PreviewStateComponent;
        PreviewUpdateDispatcher m_PreviewUpdateDispatcher;

        public GraphModelStateObserver(
            GraphModelStateComponent graphModelStateComponent,
            PreviewManager previewManager,
            PreviewStateComponent previewStateComponent,
            PreviewUpdateDispatcher previewUpdateDispatcher)
            : base(new [] {graphModelStateComponent},
                new IStateComponent [] { graphModelStateComponent, previewStateComponent})
        {
            m_PreviewManagerInstance = previewManager;
            m_GraphModelStateComponent = graphModelStateComponent;
            m_PreviewStateComponent = previewStateComponent;
            m_PreviewUpdateDispatcher = previewUpdateDispatcher;
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

                    HandleNewModels(addedModels);

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

        /// <summary>
        /// Handling for any new models added to the graph
        /// </summary>
        /// <param name="addedModels"> List of any graph element models (nodes, edges etc.) that were just added to the graph </param>
        void HandleNewModels(IEnumerable<IGraphElementModel> addedModels)
        {
            var nodes = addedModels.Where(model => model is NodeModel);
            var edges = addedModels.Where(model => model is EdgeModel);

            foreach (var node in nodes)
            {
                if (node is GraphDataNodeModel { HasPreview: true } graphDataNodeModel)
                {
                    m_PreviewManagerInstance.OnNodeAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.Guid);

                    // Register new node with the state component
                    //using (var previewStateUpdater = m_PreviewStateComponent.UpdateScope)
                    //{
                    //    previewStateUpdater.RegisterNewListener(graphDataNodeModel.graphDataName, graphDataNodeModel);
                    //}
                    //
                    //// And then request an update for that node
                    //m_PreviewUpdateDispatcher.OnPreviewListenerAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.NodePreviewMode);
                }
            }


            foreach (var edge in edges)
            {
                if (edge is GraphDataEdgeModel graphDataEdgeModel)
                {
                    var nodeModel = graphDataEdgeModel.ToPort.NodeModel as GraphDataNodeModel;
                    m_PreviewManagerInstance.OnNodeFlowChanged(nodeModel.graphDataName);
                }
            }
        }
    }
}
