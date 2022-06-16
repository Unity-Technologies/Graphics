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
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
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

        Dictionary<INodeModel, INodeModel> m_DuplicatedNodesMap = new();

        [NonSerialized]
        public GraphModelStateComponent graphModelStateComponent;

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
                try
                {
                    GraphHandler.ReconcretizeNode(localPath);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

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

        public override IReadOnlyCollection<IGraphElementModel> DeleteEdges(IReadOnlyCollection<IEdgeModel> edgeModels)
        {
            // Remove CLDS edges as well
            foreach (var edge in edgeModels)
            {
                if (edge.FromPort is GraphDataPortModel sourcePort && edge.ToPort is GraphDataPortModel destPort)
                    Disconnect(sourcePort, destPort);
            }

            return base.DeleteEdges(edgeModels);
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
                dst.owner.graphDataName, dst.graphDataName);
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
                dst.owner.graphDataName, dst.graphDataName);
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

        /// <summary>
        /// Called by PasteSerializedDataCommand to handle node duplication
        /// </summary>
        /// <param name="sourceNode"> The Original node we are duplicating, that has been JSON serialized/deserialized to create this instance </param>
        /// <param name="delta"> Position delta on the graph between original and duplicated node </param>
        /// <returns></returns>
        public override INodeModel DuplicateNode(INodeModel sourceNode, Vector2 delta, IStateComponentUpdater stateComponentUpdater = null)
        {
            var pastedNodeModel = sourceNode.Clone();
            // Set graphmodel BEFORE define node as it is commonly use during Define
            pastedNodeModel.GraphModel = this;
            pastedNodeModel.AssignNewGuid();

            switch (pastedNodeModel)
            {
                // We don't want to be able to duplicate context nodes,
                // also they subclass from GraphDataNodeModel so need to handle first
                case GraphDataContextNodeModel:
                    return null;
                case GraphDataNodeModel newCopiedNode when sourceNode is GraphDataNodeModel sourceGraphDataNode:
                {
                    newCopiedNode.graphDataName = newCopiedNode.Guid.ToString();
                    var sourceNodeHandler = GraphHandler.GetNode(sourceGraphDataNode.graphDataName);
                    GraphHandler.DuplicateNode(sourceNodeHandler, true, newCopiedNode.graphDataName);
                    break;
                }
                case GraphDataVariableNodeModel { DeclarationModel: GraphDataVariableDeclarationModel declarationModel } newCopiedVariableNode:
                {
                    newCopiedVariableNode.graphDataName = newCopiedVariableNode.Guid.ToString();

                    // Every time a variable node is duplicated, add a reference node pointing back
                    // to the property/keyword that is wrapped by the VariableDeclarationModel, on the CLDS level
                    GraphHandler.AddReferenceNode(newCopiedVariableNode.graphDataName, declarationModel.contextNodeName, declarationModel.graphDataName);
                    break;
                }
            }

            pastedNodeModel.Position += delta;
            AddNode(pastedNodeModel);
            pastedNodeModel.OnDuplicateNode(sourceNode);

            var graphModelStateUpdater = stateComponentUpdater as GraphModelStateComponent.StateUpdater;
            graphModelStateUpdater?.MarkNew(sourceNode);

            // Need to get a reference to the original node model to get edge info., the serialized copy in 'sourceNode' doesn't have that
            var originalSourceNode = NodeModels.FirstOrDefault(model => model.Guid == sourceNode.Guid);
            if (originalSourceNode != null)
            {
                m_DuplicatedNodesMap.Add(originalSourceNode, pastedNodeModel);
            }

            if (pastedNodeModel is IGraphElementContainer container)
            {
                foreach (var element in container.GraphElementModels)
                    RecursivelyRegisterAndAssignNewGuid(element);
            }

            return pastedNodeModel;
        }

        public override TDeclType DuplicateGraphVariableDeclaration<TDeclType>(TDeclType sourceModel, bool keepGuid = false)
        {
            var uniqueName = sourceModel.Title;
            var copiedVariable = sourceModel.Clone();
            copiedVariable.GraphModel = this;
            if (keepGuid)
                copiedVariable.Guid = sourceModel.Guid;
            copiedVariable.Title = GenerateGraphVariableDeclarationUniqueName(uniqueName);

            // Need to get a reference to the original variable declaration model
            // to get stored default value of the constant
            var originalVariable = VariableDeclarations.FirstOrDefault(model => model.Guid == sourceModel.Guid);
            if (originalVariable != null)
            {
                // Initialize CLDS data prior to initializing the constant
                ShaderGraphStencil.InitVariableDeclarationModel(copiedVariable, copiedVariable.InitializationModel);
                copiedVariable.CreateInitializationValue();
                copiedVariable.InitializationModel.ObjectValue = originalVariable.InitializationModel.ObjectValue;
            }

            AddVariableDeclaration(copiedVariable);

            if (sourceModel.ParentGroup != null && sourceModel.ParentGroup.GraphModel == this)
                sourceModel.ParentGroup.InsertItem(copiedVariable, -1);
            else
            {
                var section = GetSectionModel(Stencil.GetVariableSection(copiedVariable));
                section.InsertItem(copiedVariable, -1);
            }

            return copiedVariable;
        }

        public void HandlePostDuplicationEdgeFixup()
        {
            if (m_DuplicatedNodesMap.Count != 0)
            {
                try
                {
                    // Key is the original node, Value is the duplicated node
                    foreach (var (key, value) in m_DuplicatedNodesMap)
                    {
                        var originalNodeConnections = key.GetConnectedEdges();
                        foreach (var originalNodeEdge in originalNodeConnections)
                        {
                            var incomingEdgeSourceNode = originalNodeEdge.FromPort.NodeModel;
                            // This is an output edge, we can skip it
                            if (key == incomingEdgeSourceNode)
                                continue;

                            m_DuplicatedNodesMap.TryGetValue(incomingEdgeSourceNode, out var duplicatedIncomingNode);

                            // If any node that was copied has an incoming edge from a node that was ALSO
                            // copied, then we need to find the duplicated copy of the incoming node
                            // and create the edge between these new duplicated nodes instead
                            if (duplicatedIncomingNode is NodeModel duplicatedIncomingNodeModel)
                            {
                                var fromPort = FindOutputPortByName(duplicatedIncomingNodeModel, originalNodeEdge.FromPortId);
                                var toPort = FindInputPortByName(value, originalNodeEdge.ToPortId);
                                Assert.IsNotNull(fromPort);
                                Assert.IsNotNull(toPort);
                                CreateEdge(toPort, fromPort);
                            }
                            else // Just copy that connection over to the new duplicated node
                            {
                                var toPort = FindInputPortByName(value, originalNodeEdge.ToPortId);
                                var fromPort = originalNodeEdge.FromPort;
                                Assert.IsNotNull(fromPort);
                                Assert.IsNotNull(toPort);
                                CreateEdge(toPort, fromPort);
                            }
                        }
                    }
                }
                catch (Exception edgeFixupException)
                {
                    Debug.Log("Exception Thrown while trying to handle post copy-paste edge fixup." + edgeFixupException);
                }
                finally
                {
                    // We always want to make sure that the dictionary is cleared to prevent this from endless looping
                    m_DuplicatedNodesMap.Clear();
                }
            }
        }

        public static IPortModel FindInputPortByName(INodeModel nodeModel, string portID)
        {
            return ((NodeModel)nodeModel).InputsById.FirstOrDefault(input => input.Key == portID).Value;
        }

        public static IPortModel FindOutputPortByName(INodeModel nodeModel, string portID)
        {
            return ((NodeModel)nodeModel).OutputsById.FirstOrDefault(input => input.Key == portID).Value;
        }

        // TODO: (Sai) Would it be better to have a way to gather any variable nodes
        // linked to a blackboard item at a GraphHandler level instead of here?
        public IEnumerable<INodeModel> GetLinkedVariableNodes(string variableName)
        {
            return NodeModels.Where(
                node => node is GraphDataVariableNodeModel { VariableDeclarationModel: GraphDataVariableDeclarationModel variableDeclarationModel }
                    && variableDeclarationModel.graphDataName == variableName);
        }

        public static bool DoesNodeRequireTime(GraphDataNodeModel graphDataNodeModel)
        {
            bool nodeRequiresTime = false;
            if (graphDataNodeModel.TryGetNodeHandler(out var _))
            {
                // TODO: Some way of making nodes be marked as requiring time or not
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
                    GraphHandler.AddReferenceNode(guid.ToString(), model.contextNodeName, model.graphDataName);

                    // Currently using GTF guid of the variable node as its graph data name
                    graphDataVariable.graphDataName = guid.ToString();
                }
            };

            return this.CreateNode<GraphDataVariableNodeModel>(guid.ToString(), position, guid, initCallback, spawnFlags);
        }

        protected override Type GetDefaultVariableDeclarationType() => typeof(GraphDataVariableDeclarationModel);
    }
}
