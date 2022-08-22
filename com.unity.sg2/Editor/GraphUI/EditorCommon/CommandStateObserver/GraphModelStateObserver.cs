using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
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
        GraphModelStateComponent m_GraphModelStateComponent;
        ShaderGraphModel graphModel => m_GraphModelStateComponent.GraphModel as ShaderGraphModel;

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
            m_GraphModelStateComponent = graphModelStateComponent;
            m_PreviewStateComponent = previewStateComponent;
            m_PreviewUpdateDispatcher = previewUpdateDispatcher;
        }

        public override void Observe()
        {
            // Note: These using statements are necessary to increment last observed version
            using (var graphViewObservation = this.ObserveState(m_GraphModelStateComponent))
            {
                if (graphViewObservation.UpdateType != UpdateType.None)
                {
                    var changeset = m_GraphModelStateComponent.GetAggregatedChangeset(graphViewObservation.LastObservedVersion);
                    var addedModels = changeset.NewModels;
                    var removedModels = changeset.DeletedModels;
                    var changedModels = changeset.ChangedModels;

                    HandleNewModels(addedModels);

                    HandleRemovedModels(removedModels);

                    HandleChangedModels(changedModels);

                    graphModel.HandlePostDuplicationEdgeFixup();
                }
            }
        }

        /// <summary>
        /// Handling for any new models added to the graph
        /// </summary>
        /// <param name="addedModels"> List of any graph element models (nodes, edges etc.) that were just added to the graph </param>
        void HandleNewModels(IEnumerable<IGraphElementModel> addedModels)
        {
            if (!addedModels.Any())
                return;

            var nodes = addedModels.Where(model => model is NodeModel);
            var edges = addedModels.Where(model => model is EdgeModel);

            foreach (var node in nodes)
            {
                if (node is GraphDataNodeModel { HasPreview: true } graphDataNodeModel)
                {
                    // Register new node with the state component
                    using (var previewStateUpdater = m_PreviewStateComponent.UpdateScope)
                    {
                        previewStateUpdater.RegisterNewListener(graphDataNodeModel.graphDataName, graphDataNodeModel);
                    }

                    // And then request an update for that node
                    m_PreviewUpdateDispatcher.OnListenerAdded(
                        graphDataNodeModel.graphDataName,
                        graphDataNodeModel.NodePreviewMode,
                        graphModel.DoesNodeRequireTime(graphDataNodeModel));
                }
            }


            foreach (var edge in edges)
            {
                if (edge is GraphDataEdgeModel graphDataEdgeModel)
                {
                    var nodeModel = graphDataEdgeModel.ToPort.NodeModel as GraphDataNodeModel;
                    m_PreviewUpdateDispatcher.OnListenerConnectionChanged(nodeModel.graphDataName);
                }
            }
        }

        /// <summary>
        /// Handling for any models removed from the graph
        /// </summary>
        /// <param name="removedModels"> List of any graph element models (nodes, edges etc.) that were just removed from the graph </param>
        void HandleRemovedModels(IEnumerable<IGraphElementModel> removedModels)
        {
            if (!removedModels.Any())
                return;

            var nodes = removedModels.Where(model => model is NodeModel);
            var edges = removedModels.Where(model => model is EdgeModel);
            var variables = removedModels.Where(model => model is VariableDeclarationModel);

            // Node handling
            foreach (var node in nodes)
            {
                switch (node)
                {
                    case GraphDataNodeModel { HasPreview: true } graphDataNodeModel:
                    {
                        // Remove node from the state component
                        using (var previewStateUpdater = m_PreviewStateComponent.UpdateScope)
                        {
                            previewStateUpdater.RemoveListener(graphDataNodeModel.graphDataName);
                        }

                        // And then notify any nodes down stream of the deleted node that they need to be updated
                        m_PreviewUpdateDispatcher.OnListenerConnectionChanged(graphDataNodeModel.graphDataName, true);

                        // Remove CLDS data backing the node
                        graphModel.GraphHandler.RemoveNode(graphDataNodeModel.graphDataName);
                        break;
                    }
                    case GraphDataVariableNodeModel variableNodeModel:

                        m_PreviewUpdateDispatcher.OnListenerConnectionChanged(variableNodeModel.graphDataName, true);

                        var declarationModel = variableNodeModel.DeclarationModel as GraphDataVariableDeclarationModel;
                        graphModel.GraphHandler.RemoveReferenceNode(variableNodeModel.graphDataName, declarationModel.contextNodeName, declarationModel.graphDataName);
                        break;
                }
            }


            // Edge handling
            foreach (var edge in edges)
            {
                if (edge is GraphDataEdgeModel graphDataEdgeModel)
                {
                    var toNodeModel = graphDataEdgeModel.ToPort.NodeModel as GraphDataNodeModel;
                    m_PreviewUpdateDispatcher.OnListenerConnectionChanged(toNodeModel.graphDataName);

                    // NOTE: Calling GraphHandler.RemoveEdge() is unnecessary because
                    // all invalid edges from deleted nodes are already pruned in GraphHandler.RemoveNode()
                }
            }

            // Variable handling
            foreach (var variable in variables)
            {
                if (variable is GraphDataVariableDeclarationModel variableDeclarationModel)
                {
                    // Gather all variable nodes linked to this blackboard item
                    var linkedVariableNodes = graphModel.GetLinkedVariableNodes(variableDeclarationModel.graphDataName);
                    foreach (var linkedVariableNode in linkedVariableNodes)
                    {
                        var graphDataVariableNode = linkedVariableNode as GraphDataVariableNodeModel;
                        // Notify downstream nodes to update previews
                        m_PreviewUpdateDispatcher.OnListenerConnectionChanged(graphDataVariableNode.graphDataName, true);
                    }

                    graphModel.GraphHandler.RemoveReferableEntry(variableDeclarationModel.contextNodeName, variableDeclarationModel.graphDataName);
                }
            }
        }


        /// <summary>
        /// Handling for any models changed on the graph
        /// </summary>
        /// <param name="changedModels"> List of any graph element models (nodes, edges etc.) that were just changed on the graph </param>
        void HandleChangedModels(IEnumerable<IGraphElementModel> changedModels)
        {
            if (!changedModels.Any())
                return;

            var ports = changedModels.Where(model => model is PortModel);
            var variables = changedModels.Where(model => model is VariableDeclarationModel);

            foreach (var port in ports)
            {
                if (port is GraphDataPortModel { owner: GraphDataNodeModel graphDataNodeModel } graphDataPortModel)
                {
                    if(graphDataPortModel.EmbeddedValue is not BaseShaderGraphConstant cldsConstant)
                        continue;
                    // Update preview for node that owns changed port
                    m_PreviewUpdateDispatcher.OnLocalPropertyChanged(graphDataNodeModel.graphDataName,  cldsConstant.PortName, cldsConstant.ObjectValue);
                }
            }

            foreach (var variable in variables)
            {
                if (variable is GraphDataVariableDeclarationModel variableDeclarationModel)
                {
                    var cldsConstant = variableDeclarationModel.InitializationModel as BaseShaderGraphConstant;
                    if (cldsConstant.NodeName == Registry.ResolveKey<PropertyContext>().Name)
                        m_PreviewUpdateDispatcher.OnGlobalPropertyChanged(cldsConstant.PortName, cldsConstant.ObjectValue);
                }
            }
        }
    }
}
