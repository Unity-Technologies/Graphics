using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.CommandStateObserver;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Watches the graph model for changes and notifies the preview manager when changes occur
    /// Also handles notifying the graph model for post-copy edge
    /// </summary>
    class GraphModelStateObserver : StateObserver
    {
        GraphModelStateComponent m_GraphModelStateComponent;
        ShaderGraphModel graphModel => m_GraphModelStateComponent.GraphModel as ShaderGraphModel;

        PreviewStateComponent m_PreviewStateComponent;
        PreviewUpdateDispatcher m_PreviewUpdateDispatcher;

        public GraphModelStateObserver(
            GraphModelStateComponent graphModelStateComponent,
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
                }
            }
        }

        /// <summary>
        /// Handling for any new models added to the graph
        /// </summary>
        /// <param name="addedModels"> List of any graph element models (nodes, edges etc.) that were just added to the graph </param>
        void HandleNewModels(IEnumerable<SerializableGUID> addedModels)
        {
            if (!addedModels.Any())
                return;

            var nodes = addedModels.Select(guid =>
            {
                graphModel.TryGetModelFromGuid<GraphDataNodeModel>(guid, out var model);
                return model;
            }).Where(m => m != null);

            var edges = addedModels.Select(guid =>
            {
                graphModel.TryGetModelFromGuid<GraphDataEdgeModel>(guid, out var model);
                return model;
            }).Where(m => m != null);

            foreach (var graphDataNodeModel in nodes)
            {
                if (graphDataNodeModel.HasPreview)
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

            foreach (var graphDataEdgeModel in edges)
            {
                var nodeModel = graphDataEdgeModel.ToPort.NodeModel as GraphDataNodeModel;
                m_PreviewUpdateDispatcher.OnListenerConnectionChanged(nodeModel.graphDataName);
            }
        }

        /// <summary>
        /// Handling for any models removed from the graph
        /// </summary>
        /// <param name="removedModels"> List of any graph element models (nodes, edges etc.) that were just removed from the graph </param>
        void HandleRemovedModels(IEnumerable<SerializableGUID> removedModels)
        {
            if (!removedModels.Any())
                return;

            // TODO GTF UPGRADE: this will not work since models have been deleted.

            var nodes = removedModels.Select(guid =>
            {
                graphModel.TryGetModelFromGuid<NodeModel>(guid, out var model);
                return model;
            }).Where(m => m != null);

            var edges = removedModels.Select(guid =>
            {
                graphModel.TryGetModelFromGuid<GraphDataEdgeModel>(guid, out var model);
                return model;
            }).Where(m => m != null);

            var variables = removedModels.Select(guid =>
            {
                graphModel.TryGetModelFromGuid<GraphDataVariableDeclarationModel>(guid, out var model);
                return model;
            }).Where(m => m != null);

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
            foreach (var graphDataEdgeModel in edges)
            {
                var toNodeModel = graphDataEdgeModel.ToPort.NodeModel as GraphDataNodeModel;
                m_PreviewUpdateDispatcher.OnListenerConnectionChanged(toNodeModel.graphDataName);

                // NOTE: Calling GraphHandler.RemoveEdge() is unnecessary because
                // all invalid edges from deleted nodes are already pruned in GraphHandler.RemoveNode()
            }

            // Variable handling
            foreach (var variableDeclarationModel in variables)
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


        /// <summary>
        /// Handling for any models changed on the graph
        /// </summary>
        /// <param name="changedModels"> List of any graph element models (nodes, edges etc.) that were just changed on the graph </param>
        void HandleChangedModels(IEnumerable<SerializableGUID> changedModels)
        {
            if (!changedModels.Any())
                return;

            var ports = changedModels.Select(guid =>
            {
                graphModel.TryGetModelFromGuid<GraphDataPortModel>(guid, out var model);
                return model;
            }).Where(m => m != null);

            var variables = changedModels.Select(guid =>
            {
                graphModel.TryGetModelFromGuid<GraphDataVariableDeclarationModel>(guid, out var model);
                return model;
            }).Where(m => m != null);

            foreach (var graphDataPortModel in ports)
            {
                if (graphDataPortModel.owner is GraphDataNodeModel graphDataNodeModel)
                {
                    if(graphDataPortModel.EmbeddedValue is not BaseShaderGraphConstant cldsConstant)
                        continue;
                    // Update preview for node that owns changed port
                    m_PreviewUpdateDispatcher.OnLocalPropertyChanged(graphDataNodeModel.graphDataName,  cldsConstant.PortName, cldsConstant.ObjectValue);
                }
            }

            foreach (var variableDeclarationModel in variables)
            {
                var cldsConstant = variableDeclarationModel.InitializationModel as BaseShaderGraphConstant;
                if (cldsConstant.NodeName == Registry.ResolveKey<PropertyContext>().Name)
                    m_PreviewUpdateDispatcher.OnGlobalPropertyChanged(cldsConstant.PortName, cldsConstant.ObjectValue);
            }
        }
    }
}
