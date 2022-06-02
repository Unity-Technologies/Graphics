using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public enum PropagationDirection
    {
        Upstream,
        Downstream
    }

    [Serializable]
    class MainPreviewData
    {
        [SerializeField]
        private SerializableMesh serializedMesh = new ();
        public bool preventRotation;

        public int width = 125;
        public int height = 125;

        [NonSerialized]
        public Quaternion rotation = Quaternion.identity;

        [NonSerialized]
        public float scale = 1f;

        public Mesh mesh
        {
            get => serializedMesh.mesh;
            set => serializedMesh.mesh = value;
        }
    }

    public class ShaderGraphModel : GraphModel
    {
        [SerializeField]
        private SerializableGraphHandler graphHandlerBox = new();
        [SerializeField]
        private SerializableTargetSettings targetSettingsBox = new();
        [SerializeField]
        private MainPreviewData mainPreviewData = new(); // TODO: This should wrap a UserPrefs entry instead of being stored here.
        [SerializeField]
        private bool isSubGraph = false;

        internal GraphHandler GraphHandler => graphHandlerBox.Graph;
        internal ShaderGraphRegistry RegistryInstance => ShaderGraphRegistry.Instance;
        internal List<JsonData<Target>> Targets => targetSettingsBox.Targets;
        internal MainPreviewData MainPreviewData => mainPreviewData;
        internal bool IsSubGraph => CanBeSubgraph();
        internal string BlackboardContextName => Registry.ResolveKey<PropertyContext>().Name;

        [NonSerialized]
        public GraphModelStateComponent graphModelStateComponent;
        public Action<IGraphElementModel> OnGraphModelChanged;

        public void Init(GraphHandler graph, bool isSubGraph)
        {
            graphHandlerBox.Init(graph);
            this.isSubGraph = isSubGraph;

            // Generate context nodes as needed.
            // TODO: This should be handled by a more generalized synchronization step.
            var contextNames = GraphHandler
                .GetNodes()
                .Where(nodeHandler => nodeHandler.GetRegistryKey().Name == Registry.ResolveKey<ContextBuilder>().Name)
                .Select(nodeHandler => nodeHandler.ID.LocalPath)
                .ToList();

            foreach (var localPath in contextNames)
            {
                GraphHandler.ReconcretizeNode(localPath);

                if (!NodeModels.Any(nodeModel =>
                        nodeModel is GraphDataContextNodeModel contextNodeModel &&
                        contextNodeModel.graphDataName == localPath))
                {
                    this.CreateGraphDataContextNode(localPath);
                }
            }
        }

        public override void OnEnable()
        {
            graphHandlerBox.OnEnable();
            targetSettingsBox.OnEnable();
            base.OnEnable();
        }
        public override bool CanBeSubgraph() => isSubGraph;
        protected override Type GetEdgeType(IPortModel toPort, IPortModel fromPort)
        {
            return typeof(GraphDataEdgeModel);
        }
        public override Type GetSectionModelType()
        {
            return typeof(SectionModel);
        }

        public override IEdgeModel CreateEdge(IPortModel toPort, IPortModel fromPort, SerializableGUID guid = default)
        {
            IPortModel resolvedEdgeSource;
            List<IPortModel> resolvedEdgeDestinations;
            resolvedEdgeSource = HandleRedirectNodesCreation(toPort, fromPort, out resolvedEdgeDestinations);

            var edgeModel = base.CreateEdge(toPort, fromPort, guid);
            if (resolvedEdgeSource is not GraphDataPortModel fromDataPort)
                return edgeModel;

            // Make the corresponding connections in CLDS data model
            foreach (var toDataPort in resolvedEdgeDestinations.OfType<GraphDataPortModel>())
            {
              // Validation should have already happened in GraphModel.IsCompatiblePort.
              Assert.IsTrue(TryConnect(fromDataPort, toDataPort));
            }

            return edgeModel;
        }

        IPortModel HandleRedirectNodesCreation(IPortModel toPort, IPortModel fromPort, out List<IPortModel> resolvedDestinations)
        {
            var resolvedSource = fromPort;
            resolvedDestinations = new List<IPortModel>();

            if (toPort.NodeModel is RedirectNodeModel toRedir)
            {
                resolvedDestinations = toRedir.ResolveDestinations().ToList();

                // Update types of descendant redirect nodes.
                foreach (var child in toRedir.GetRedirectTree(true))
                {
                    child.UpdateTypeFrom(fromPort);
                    OnGraphModelChanged(child);
                }
            }
            else
            {
                resolvedDestinations.Add(toPort);
            }

            if (fromPort.NodeModel is RedirectNodeModel fromRedir)
            {
                resolvedSource = fromRedir.ResolveSource();
            }

            return resolvedSource;
        }

        /// <summary>
        /// Tests the connection between two GraphData ports at the data level.
        /// </summary>
        /// <param name="src">Source port.</param>
        /// <param name="dst">Destination port.</param>
        /// <returns>True if the ports can be connected, false otherwise.</returns>
        bool TestConnection(GraphDataPortModel src, GraphDataPortModel dst)
        {
            // temporarily disable connections to ports of different types.
            if (src.PortDataType != dst.PortDataType)
                return false;

            return GraphHandler.TestConnection(dst.owner.graphDataName,
                dst.graphDataName, src.owner.graphDataName,
                src.graphDataName, RegistryInstance.Registry);
        }

        /// <summary>
        /// Tries to connect two GraphData ports at the data level.
        /// </summary>
        /// <param name="src">Source port.</paDram>
        /// <param name="dst">Destination port.</param>
        /// <returns>True if the connection was successful, false otherwise.</returns>
        public bool TryConnect(GraphDataPortModel src, GraphDataPortModel dst)
        {
            return GraphHandler.TryConnect(
                src.owner.graphDataName, src.graphDataName,
                dst.owner.graphDataName, dst.graphDataName,
                RegistryInstance.Registry);
        }

        /// <summary>
        /// Disconnects two GraphData ports at the data level.
        /// </summary>
        /// <param name="src">Source port.</paDram>
        /// <param name="dst">Destination port.</param>
        public void Disconnect(GraphDataPortModel src, GraphDataPortModel dst)
        {
            GraphHandler.Disconnect(
                src.owner.graphDataName, src.graphDataName,
                dst.owner.graphDataName, dst.graphDataName,
                RegistryInstance.Registry);
        }

        static bool PortsFormCycle(IPortModel fromPort, IPortModel toPort)
        {
            var queue = new Queue<IPortNodeModel>();
            queue.Enqueue(fromPort.NodeModel);

            while (queue.Count > 0)
            {
                var checkNode = queue.Dequeue();

                if (checkNode == toPort.NodeModel) return true;

                foreach (var incomingEdge in checkNode.GetIncomingEdges())
                {
                    queue.Enqueue(incomingEdge.FromPort.NodeModel);
                }
            }

            return false;
        }

        protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            if (startPortModel.Direction == compatiblePortModel.Direction) return false;

            var fromPort = startPortModel.Direction == PortDirection.Output ? startPortModel : compatiblePortModel;
            var toPort = startPortModel.Direction == PortDirection.Input ? startPortModel : compatiblePortModel;

            if (PortsFormCycle(fromPort, toPort)) return false;

            if (fromPort.NodeModel is RedirectNodeModel fromRedirect)
            {
                fromPort = fromRedirect.ResolveSource();
                if (fromPort == null) return true;
            }

            if (toPort.NodeModel is RedirectNodeModel toRedirect)
            {
                // Only connect to a hanging branch if it's valid for every connection.
                // Should not recurse more than once. ResolveDestinations returns non-redirect nodes.
                return toRedirect.ResolveDestinations().All(testPort => IsCompatiblePort(fromPort, testPort));
            }

            if ((fromPort, toPort) is (GraphDataPortModel fromDataPort, GraphDataPortModel toDataPort))
            {
                return fromDataPort.owner.existsInGraphData &&
                    toDataPort.owner.existsInGraphData &&
                    TestConnection(fromDataPort, toDataPort);
            }

            // Don't support connecting GraphDelta-backed ports to UI-only ones.
            if (fromPort is GraphDataPortModel || toPort is GraphDataPortModel)
            {
                return false;
            }

            return base.IsCompatiblePort(startPortModel, compatiblePortModel);
        }

        public IEnumerable<IVariableDeclarationModel> GetGraphProperties()
        {
            return this.VariableDeclarations;
        }

        public void GetTimeDependentNodesOnGraph(PooledHashSet<GraphDataNodeModel> timeDependentNodes)
        {
            var nodesOnGraph = NodeModels;
            foreach (var nodeModel in nodesOnGraph)
            {
                if (nodeModel is GraphDataNodeModel graphDataNodeModel && DoesNodeRequireTime(graphDataNodeModel))
                    timeDependentNodes.Add(graphDataNodeModel);
            }

            PropagateNodes(timeDependentNodes, PropagationDirection.Downstream, timeDependentNodes);
        }

        // cache the Action to avoid GC
        static readonly Action<GraphDataNodeModel> AddNextLevelNodesToWave =
            nextLevelNode =>
            {
                if (!m_TempAddedToNodeWave.Contains(nextLevelNode))
                {
                    m_TempNodeWave.Push(nextLevelNode);
                    m_TempAddedToNodeWave.Add(nextLevelNode);
                }
            };

        // Temp structures that are kept around statically to avoid GC churn (not thread safe)
        static Stack<GraphDataNodeModel> m_TempNodeWave = new();
        static HashSet<GraphDataNodeModel> m_TempAddedToNodeWave = new();

        // ADDs all nodes in sources, and all nodes in the given direction relative to them, into result
        // sources and result can be the same HashSet
        private static readonly ProfilerMarker PropagateNodesMarker = new ProfilerMarker("PropagateNodes");

        static void PropagateNodes(HashSet<GraphDataNodeModel> sources, PropagationDirection dir, HashSet<GraphDataNodeModel> result)
        {
            using (PropagateNodesMarker.Auto())
                if (sources.Count > 0)
                {
                    // NodeWave represents the list of nodes we still have to process and add to result
                    m_TempNodeWave.Clear();
                    m_TempAddedToNodeWave.Clear();
                    foreach (var node in sources)
                    {
                        m_TempNodeWave.Push(node);
                        m_TempAddedToNodeWave.Add(node);
                    }

                    while (m_TempNodeWave.Count > 0)
                    {
                        var node = m_TempNodeWave.Pop();
                        if (node == null)
                            continue;

                        result.Add(node);

                        // grab connected nodes in propagation direction, add them to the node wave
                        ForeachConnectedNode(node, dir, AddNextLevelNodesToWave);
                    }

                    // clean up any temp data
                    m_TempNodeWave.Clear();
                    m_TempAddedToNodeWave.Clear();
                }
        }

        static IEnumerable<NodeHandler> GetUpstreamNodes(NodeHandler startingNode)
        {
            return Utils.GraphTraversalUtils.GetUpstreamNodes(startingNode);
        }

        static IEnumerable<NodeHandler> GetDownstreamNodes(NodeHandler startingNode)
        {
            return Utils.GraphTraversalUtils.GetDownstreamNodes(startingNode);
        }

        static GraphDataNodeModel TryGetNodeModel(ShaderGraphModel shaderGraphModel, NodeHandler inputNodeReader)
        {
            // TODO: Make a mapping between every node model and node reader so we can also do lookup
            // from NodeReaders to NodeModels, as is needed below for instance
            return null;
        }

        static void ForeachConnectedNode(GraphDataNodeModel sourceNode, PropagationDirection dir, Action<GraphDataNodeModel> action)
        {
            sourceNode.TryGetNodeReader(out var nodeReader);

            ShaderGraphModel shaderGraphModel = sourceNode.GraphModel as ShaderGraphModel;

            // Enumerate through all nodes that the node feeds into and add them to the list of nodes to inspect
            if (dir == PropagationDirection.Downstream)
            {
                foreach (var connectedNode in GetDownstreamNodes(nodeReader))
                {
                    action(TryGetNodeModel(shaderGraphModel, connectedNode));
                }
            }
            else
            {
                foreach (var connectedNode in GetUpstreamNodes(nodeReader))
                {
                    action(TryGetNodeModel(shaderGraphModel, connectedNode));
                }
            }

            /* Custom Interpolator Blocks have implied connections to their Custom Interpolator Nodes...
            if (dir == PropagationDirection.Downstream && node is BlockNode bnode && bnode.isCustomBlock)
            {
                foreach (var cin in CustomInterpolatorUtils.GetCustomBlockNodeDependents(bnode))
                {
                    action(cin);
                }
            }
            // ... Just as custom Interpolator Nodes have implied connections to their custom interpolator blocks
            if (dir == PropagationDirection.Upstream && node is CustomInterpolatorNode ciNode && ciNode.e_targetBlockNode != null)
            {
                action(ciNode.e_targetBlockNode);
            } */
        }

        public IEnumerable<GraphDataNodeModel> GetNodesInHierarchyFromSources(IEnumerable<GraphDataNodeModel> nodeSources, PropagationDirection propagationDirection)
        {
            var nodesInHierarchy = new HashSet<GraphDataNodeModel>();
            PropagateNodes(new HashSet<GraphDataNodeModel>(nodeSources), propagationDirection, nodesInHierarchy);
            return nodesInHierarchy;
        }

        public static bool DoesNodeRequireTime(GraphDataNodeModel graphDataNodeModel)
        {
            bool nodeRequiresTime = false;
            if (graphDataNodeModel.TryGetNodeReader(out var _))
            {
                // TODO: Some way of making nodes be marked as requiring time or not
                // According to Esme, dependencies on globals/properties etc. will exist as RefNodes,
                // which are other INodeReaders/IPortReaders that exist as hidden/internal connections on a node
                //nodeReader.TryGetField("requiresTime", out var fieldReader);
                //if(fieldReader != null)
                //    fieldReader.TryGetValue(out nodeRequiresTime);
            }

            return nodeRequiresTime;
        }

        public static bool ShouldElementBeVisibleToSearcher(ShaderGraphModel shaderGraphModel, RegistryKey elementRegistryKey)
        {
            try
            {
                var nodeBuilder = shaderGraphModel.RegistryInstance.GetNodeBuilder(elementRegistryKey);
                var registryFlags = nodeBuilder.GetRegistryFlags();

                // commented out that bit cause it throws an exception for some elements at the moment
                return registryFlags.HasFlag(RegistryFlags.Func) /*|| registry.GetDefaultTopology(elementRegistryKey) == null*/;
            }
            catch (Exception exception)
            {
                AssertHelpers.Fail("Failed due to exception:" + exception);
                return false;
            }
        }

        public override IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel,
            Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            Action<VariableNodeModel> initCallback = variableNodeModel =>
            {
                if (declarationModel is GraphDataVariableDeclarationModel model && variableNodeModel is GraphDataVariableNodeModel graphDataVariable)
                {
                    variableNodeModel.VariableDeclarationModel = model;

                    // Every time a variable node is added to the graph, add a reference node pointing back to the variable/property that is wrapped by the VariableDeclarationModel, on the CLDS level
                    GraphHandler.AddReferenceNode(guid.ToString(), model.contextNodeName, model.graphDataName, RegistryInstance.Registry);

                    // Currently using GTF guid of the variable node as its graph data name
                    graphDataVariable.graphDataName = guid.ToString();
                }
            };

            return this.CreateNode<GraphDataVariableNodeModel>(guid.ToString(), position, guid, initCallback, spawnFlags);
        }

        protected override Type GetDefaultVariableDeclarationType() => typeof(GraphDataVariableDeclarationModel);
    }
}
