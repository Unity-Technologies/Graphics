using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public enum PropagationDirection
    {
        Upstream,
        Downstream
    }

    public class ShaderGraphModel : GraphModel
    {
        // TODO: This will eventually delegate to real serialized data. For now, it's kept here ephemerally. Until
        //       then, (at least) the following features are broken as a result: reloading assembly, saving, loading.
        public GraphHandler GraphHandler => ShaderGraphAssetModel.GraphHandler;

        public ShaderGraphAssetModel ShaderGraphAssetModel => AssetModel as ShaderGraphAssetModel;

        public Registry.Registry RegistryInstance => ((ShaderGraphStencil)Stencil).GetRegistry();

        protected override Type GetEdgeType(IPortModel toPort, IPortModel fromPort)
        {
            return typeof(GraphDataEdgeModel);
        }

        public override Type GetSectionModelType()
        {
            return typeof(SectionModel);
        }

        public override void OnEnable()
        {
            base.OnEnable();

            // Assigning this value manually as section models are setup by default to have the asset model reference serialized in, but we modified GTF to prevent that
            foreach(var sectionModel in SectionModels)
            {
                sectionModel.AssetModel = AssetModel;
            }
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

            return GraphHandler.TestConnection(dst.graphDataNodeModel.graphDataName,
                dst.graphDataName, src.graphDataNodeModel.graphDataName,
                src.graphDataName, RegistryInstance);
        }

        /// <summary>
        /// Tries to connect two GraphData ports at the data level.
        /// </summary>
        /// <param name="src">Source port.</param>
        /// <param name="dst">Destination port.</param>
        /// <returns>True if the connection was successful, false otherwise.</returns>
        public bool TryConnect(GraphDataPortModel src, GraphDataPortModel dst)
        {
            return GraphHandler.TryConnect(
                dst.graphDataNodeModel.graphDataName, dst.graphDataName,
                src.graphDataNodeModel.graphDataName, src.graphDataName,
                RegistryInstance);
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
                return fromDataPort.graphDataNodeModel.existsInGraphData &&
                       toDataPort.graphDataNodeModel.existsInGraphData &&
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
                if(nodeModel is GraphDataNodeModel graphDataNodeModel && DoesNodeRequireTime(graphDataNodeModel))
                    timeDependentNodes.Add(graphDataNodeModel);
            }

            PropagateNodes(timeDependentNodes, PropagationDirection.Downstream, timeDependentNodes);
        }

        // cache the Action to avoid GC
        static Action<GraphDataNodeModel> AddNextLevelNodesToWave =
            nextLevelNode =>
            {
                if (!m_TempAddedToNodeWave.Contains(nextLevelNode))
                {
                    m_TempNodeWave.Push(nextLevelNode);
                    m_TempAddedToNodeWave.Add(nextLevelNode);
                }
            };

        // Temp structures that are kept around statically to avoid GC churn (not thread safe)
        static Stack<GraphDataNodeModel> m_TempNodeWave = new ();
        static HashSet<GraphDataNodeModel> m_TempAddedToNodeWave = new ();
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

        static IEnumerable<INodeReader> GetUpstreamNodes(INodeReader startingNode)
        {
            foreach (var inputPort in startingNode.GetInputPorts())
            {
                foreach (var connectedPort in inputPort.GetConnectedPorts())
                {
                    foreach (var upstreamNode in GetUpstreamNodes(connectedPort.GetNode()))
                        yield return upstreamNode;
                }
            }

            yield return startingNode;
        }

        static IEnumerable<INodeReader> GetDownstreamNodes(INodeReader startingNode)
        {
            foreach (var inputPort in startingNode.GetOutputPorts())
            {
                foreach (var connectedPort in inputPort.GetConnectedPorts())
                {
                    foreach (var downstreamNodes in GetDownstreamNodes(connectedPort.GetNode()))
                        yield return downstreamNodes;
                }
            }

            yield return startingNode;
        }

        static GraphDataNodeModel TryGetNodeModel(ShaderGraphModel shaderGraphModel, INodeReader inputNodeReader)
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
            if(graphDataNodeModel.TryGetNodeReader(out var nodeReader))
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
            catch(Exception exception)
            {
                AssertHelpers.Fail("Failed due to exception:" + exception);
                return false;
            }
        }
    }
}
