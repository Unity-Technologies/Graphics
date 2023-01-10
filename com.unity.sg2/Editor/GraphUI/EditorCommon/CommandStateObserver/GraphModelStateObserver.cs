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
        class NodeRemovalInfo
        {
            public string graphDataName { get; }

            public NodeRemovalInfo(SGNodeModel nodeModel)
            {
                graphDataName = nodeModel.graphDataName;
            }
        }

        class BlockRemovalInfo
        {
            public string contextNodeName { get; }

            public BlockRemovalInfo(SGBlockNodeModel blockNodeModel)
            {
                contextNodeName = (blockNodeModel.ContextNodeModel as SGContextNodeModel)?.graphDataName;
            }
        }

        class VariableNodeRemovalInfo
        {
            public string graphDataName { get; }
            public string declarationContextNodeName { get; }
            public string declarationGraphDataName { get; }

            public VariableNodeRemovalInfo(SGVariableNodeModel nodeModel)
            {
                graphDataName = nodeModel.graphDataName;

                var declaration = nodeModel.DeclarationModel as SGVariableDeclarationModel;
                declarationContextNodeName = declaration?.contextNodeName;
                declarationGraphDataName = declaration?.graphDataName;
            }
        }

        class WireRemovalInfo
        {
            public string toNodeGraphDataName { get; }

            public WireRemovalInfo(IGraphDataOwner toNodeModel)
            {
                toNodeGraphDataName = toNodeModel.graphDataName;
            }
        }

        class VariableRemovalInfo
        {
            public string contextNodeName { get; }
            public string graphDataName { get; }

            public VariableRemovalInfo(SGVariableDeclarationModel variableDeclarationModel)
            {
                contextNodeName = variableDeclarationModel.contextNodeName;
                graphDataName = variableDeclarationModel.graphDataName;
            }
        }

        Dictionary<SerializableGUID, NodeRemovalInfo> m_NodeRemovalInfo;
        Dictionary<SerializableGUID, BlockRemovalInfo> m_BlockRemovalInfo;
        Dictionary<SerializableGUID, VariableNodeRemovalInfo> m_VariableNodeRemovalInfo;
        Dictionary<SerializableGUID, WireRemovalInfo> m_WireRemovalInfo;
        Dictionary<SerializableGUID, VariableRemovalInfo> m_VariableRemovalInfo;

        GraphModelStateComponent m_GraphModelStateComponent;
        SGGraphModel graphModel => m_GraphModelStateComponent.GraphModel as SGGraphModel;

        PreviewStateComponent m_PreviewStateComponent;
        PreviewUpdateDispatcher m_PreviewUpdateDispatcher;

        public GraphModelStateObserver(
            GraphModelStateComponent graphModelStateComponent,
            PreviewStateComponent previewStateComponent,
            PreviewUpdateDispatcher previewUpdateDispatcher)
            : base(new[] { graphModelStateComponent },
                new[] { previewStateComponent })
        {
            m_GraphModelStateComponent = graphModelStateComponent;
            m_PreviewStateComponent = previewStateComponent;
            m_PreviewUpdateDispatcher = previewUpdateDispatcher;

        }

        void InitCache()
        {
            if (graphModel != null)
            {
                m_NodeRemovalInfo = new Dictionary<SerializableGUID, NodeRemovalInfo>();
                m_BlockRemovalInfo = new Dictionary<SerializableGUID, BlockRemovalInfo>();
                m_VariableNodeRemovalInfo = new Dictionary<SerializableGUID, VariableNodeRemovalInfo>();
                m_WireRemovalInfo = new Dictionary<SerializableGUID, WireRemovalInfo>();
                m_VariableRemovalInfo = new Dictionary<SerializableGUID, VariableRemovalInfo>();

                AddOrUpdateModelsToCache(graphModel.NodeAndBlockModels
                    .Concat<GraphElementModel>(graphModel.WireModels)
                    .Concat(graphModel.VariableDeclarations));
            }
        }

        void AddOrUpdateModelsToCache(IEnumerable<GraphElementModel> models)
        {
            foreach (var model in models)
            {
                switch (model)
                {
                    case SGVariableDeclarationModel variableDeclarationModel:
                    {
                        m_VariableRemovalInfo[model.Guid] = new VariableRemovalInfo(variableDeclarationModel);
                        break;
                    }
                    case SGEdgeModel wireModel when
                        graphModel.TryGetModelFromGuid(wireModel.ToNodeGuid, out var toNode) &&
                        toNode is IGraphDataOwner dataOwner:
                    {
                        m_WireRemovalInfo[model.Guid] = new WireRemovalInfo(dataOwner);
                        break;
                    }
                    case SGNodeModel { HasPreview: true } nodeModel:
                    {
                        m_NodeRemovalInfo[model.Guid] = new NodeRemovalInfo(nodeModel);
                        break;
                    }
                    case SGBlockNodeModel blockNodeModel:
                    {
                        m_BlockRemovalInfo[model.Guid] = new BlockRemovalInfo(blockNodeModel);
                        break;
                    }
                    case SGVariableNodeModel nodeModel:
                    {
                        m_VariableNodeRemovalInfo[model.Guid] = new VariableNodeRemovalInfo(nodeModel);
                        break;
                    }
                }
            }
        }

        void RemoveModelsFromCache(IEnumerable<SerializableGUID> guids)
        {
            foreach (var guid in guids)
            {
                _ = m_WireRemovalInfo.Remove(guid) ||
                    m_NodeRemovalInfo.Remove(guid) ||
                    m_BlockRemovalInfo.Remove(guid) ||
                    m_VariableNodeRemovalInfo.Remove(guid) ||
                    m_VariableRemovalInfo.Remove(guid);
            }
        }

        public override void Observe()
        {
            if (m_NodeRemovalInfo == null)
            {
                InitCache();
            }

            // Note: These using statements are necessary to increment last observed version
            using (var graphViewObservation = this.ObserveState(m_GraphModelStateComponent))
            {
                if (graphViewObservation.UpdateType != UpdateType.None)
                {
                    var changeset = m_GraphModelStateComponent.GetAggregatedChangeset(graphViewObservation.LastObservedVersion);
                    HandleNewModels(changeset.NewModels);
                    HandleRemovedModels(changeset.DeletedModels);
                    HandleChangedModels(changeset.ChangedModels);
                }
            }
        }

        /// <summary>
        /// Handling for any new models added to the graph
        /// </summary>
        /// <param name="addedModelGuids"> List of any graph element models (nodes, edges etc.) that were just added to the graph </param>
        void HandleNewModels(IEnumerable<SerializableGUID> addedModelGuids)
        {
            var addedModels = addedModelGuids.Select(guid => graphModel.GetModel(guid)).ToList();

            if (!addedModels.Any())
                return;

            AddOrUpdateModelsToCache(addedModels);

            foreach (var graphDataNodeModel in addedModels.OfType<SGNodeModel>())
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

            foreach (var graphDataEdgeModel in addedModels.OfType<SGEdgeModel>())
            {
                if (graphDataEdgeModel.ToPort.NodeModel is SGNodeModel nodeModel)
                {
                    m_PreviewUpdateDispatcher.OnListenerConnectionChanged(nodeModel.graphDataName);
                }
            }
        }

        /// <summary>
        /// Handling for any models removed from the graph
        /// </summary>
        /// <param name="removedModelGuids"> List of any graph element models (nodes, edges etc.) that were just removed from the graph </param>
        void HandleRemovedModels(IEnumerable<SerializableGUID> removedModelGuids)
        {
            if (!removedModelGuids.Any())
                return;

            foreach (var guid in removedModelGuids)
            {
                if (m_NodeRemovalInfo.TryGetValue(guid, out var nodeRemovalInfo))
                {
                    // Remove node from the state component
                    using (var previewStateUpdater = m_PreviewStateComponent.UpdateScope)
                    {
                        previewStateUpdater.RemoveListener(nodeRemovalInfo.graphDataName);
                    }

                    // And then notify any nodes down stream of the deleted node that they need to be updated
                    m_PreviewUpdateDispatcher.OnListenerConnectionChanged(nodeRemovalInfo.graphDataName, true);

                    // Remove CLDS data backing the node
                    graphModel.GraphHandler.RemoveNode(nodeRemovalInfo.graphDataName);
                }
                else if (m_BlockRemovalInfo.TryGetValue(guid, out var blockRemovalInfo))
                {
                    m_PreviewUpdateDispatcher.OnListenerConnectionChanged(blockRemovalInfo.contextNodeName);
                }
                else if (m_VariableNodeRemovalInfo.TryGetValue(guid, out var variableNodeRemovalInfo))
                {
                    m_PreviewUpdateDispatcher.OnListenerConnectionChanged(variableNodeRemovalInfo.graphDataName, true);

                    graphModel.GraphHandler.RemoveReferenceNode(variableNodeRemovalInfo.graphDataName,
                        variableNodeRemovalInfo.declarationContextNodeName,
                        variableNodeRemovalInfo.declarationGraphDataName);
                }
                else if (m_WireRemovalInfo.TryGetValue(guid, out var wireRemovalInfo))
                {
                    m_PreviewUpdateDispatcher.OnListenerConnectionChanged(wireRemovalInfo.toNodeGraphDataName);

                    // NOTE: Calling GraphHandler.RemoveEdge() is unnecessary because
                    // all invalid edges from deleted nodes are already pruned in GraphHandler.RemoveNode()
                }
                else if (m_VariableRemovalInfo.TryGetValue(guid, out var variableRemovalInfo))
                {
                    graphModel.GraphHandler.RemoveReferableEntry(variableRemovalInfo.contextNodeName,
                        variableRemovalInfo.graphDataName);
                }
            }

            RemoveModelsFromCache(removedModelGuids);
        }


        /// <summary>
        /// Handling for any models changed on the graph
        /// </summary>
        /// <param name="changedModelGuids"> List of any graph element models (nodes, edges etc.) that were just changed on the graph </param>
        void HandleChangedModels(IEnumerable<SerializableGUID> changedModelGuids)
        {
            var changedModels = changedModelGuids.Select(guid => graphModel.GetModel(guid)).ToList();

            if (!changedModels.Any())
                return;

            AddOrUpdateModelsToCache(changedModels);

            foreach (var graphDataPortModel in changedModels.OfType<SGPortModel>())
            {
                if (graphDataPortModel.EmbeddedValue is not BaseShaderGraphConstant cldsConstant)
                    continue;
                // Update preview for node that owns changed port
                m_PreviewUpdateDispatcher.OnLocalPropertyChanged(graphDataPortModel.owner.graphDataName,  cldsConstant.PortName, cldsConstant.ObjectValue);
            }

            foreach (var variableDeclarationModel in changedModels.OfType<SGVariableDeclarationModel>())
            {
                var cldsConstant = variableDeclarationModel.InitializationModel as BaseShaderGraphConstant;
                if (cldsConstant != null && cldsConstant.NodeName == Registry.ResolveKey<PropertyContext>().Name)
                    m_PreviewUpdateDispatcher.OnGlobalPropertyChanged(cldsConstant.PortName, cldsConstant.ObjectValue);
            }
        }
    }
}
