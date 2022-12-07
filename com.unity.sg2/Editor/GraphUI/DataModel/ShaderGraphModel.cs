using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.CommandStateObserver;
using Unity.GraphToolsFoundation;
using UnityEditor.ShaderGraph.GraphUI.UIData;
using SerializedMesh = UnityEditor.ShaderGraph.Utils.SerializableMesh;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Serializable]
    class MainPreviewData
    {
        string m_GraphModelGuid;
        string ScaleUserPrefKey => m_GraphModelGuid + "." + ChangePreviewZoomCommand.UserPrefsKey;
        string RotationUserPrefKey => m_GraphModelGuid + "." + ChangePreviewRotationCommand.UserPrefsKey;
        string MeshUserPrefKey => m_GraphModelGuid + "." + ChangePreviewMeshCommand.UserPrefsKey;

        // We don't serialize these fields, we just set them for easy access by other systems...
        [NonSerialized]
        public Vector2 mainPreviewSize = new (200, 200);
        [NonSerialized]
        public bool lockMainPreviewRotation = false;

        public MainPreviewData(string graphAssetGuid)
        {
            // Get graph asset guid so we can search for user prefs attached to this asset (if any)
            m_GraphModelGuid = graphAssetGuid;

            // Get scale from prefs if present
            scale = EditorPrefs.GetFloat(ScaleUserPrefKey, 1.0f);

            // Get rotation from prefs if present
            var rotationJson = EditorPrefs.GetString(RotationUserPrefKey, string.Empty);
            if (rotationJson != string.Empty)
                m_Rotation = StringToQuaternion(rotationJson);

            // Get mesh from prefs if present
            var meshJson = EditorPrefs.GetString(MeshUserPrefKey, string.Empty);
            if (meshJson != string.Empty)
               EditorJsonUtility.FromJsonOverwrite(meshJson, serializedMesh);
        }

        public static Quaternion StringToQuaternion(string sQuaternion)
        {
            // Remove the parentheses
            if (sQuaternion.StartsWith("(") && sQuaternion.EndsWith(")"))
            {
                sQuaternion = sQuaternion.Substring(1, sQuaternion.Length - 2);
            }

            // split the items
            string[] sArray = sQuaternion.Split(',');

            // store as a Vector3
            Quaternion result = new Quaternion(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]),
                float.Parse(sArray[3]));

            return result;
        }

        [SerializeField]
        private SerializedMesh serializedMesh = new ();

        [NonSerialized]
        Quaternion m_Rotation = Quaternion.identity;

        public Quaternion rotation
        {
            get => m_Rotation;
            set
            {
                m_Rotation = value;
                EditorPrefs.SetString(RotationUserPrefKey, rotation.ToString());
            }
        }


        [NonSerialized]
        float m_Scale = 1.0f;

        public float scale
        {
            get => m_Scale;
            set
            {
                m_Scale = value;
                EditorPrefs.SetFloat(ScaleUserPrefKey, m_Scale);
            }
        }

        public Mesh mesh
        {
            get => serializedMesh.mesh;
            set
            {
                serializedMesh.mesh = value;
                EditorPrefs.SetString(MeshUserPrefKey, EditorJsonUtility.ToJson(serializedMesh));
            }
        }
    }

    class ShaderGraphModel : GraphModel
    {
        [SerializeField]
        private ShaderGraphAssetUtils.SerializableTargetSettings targetSettingsBox = new();
        [SerializeField]
        private MainPreviewData mainPreviewData;
        [SerializeField]
        private bool isSubGraph = false;

        [NonSerialized]
        GraphHandler m_GraphHandler;
        internal GraphHandler GraphHandler => ((ShaderGraphAsset)Asset).CLDSModel;
        internal ShaderGraphRegistry RegistryInstance => ShaderGraphRegistry.Instance;
        internal List<JsonData<Target>> Targets => targetSettingsBox.Targets; // TODO: Store the active editing target in the box?
        internal Target ActiveTarget => Targets.FirstOrDefault();
        internal MainPreviewData MainPreviewData => mainPreviewData;
        internal bool IsSubGraph => CanBeSubgraph();
        internal string BlackboardContextName => Registry.ResolveKey<PropertyContext>().Name;
        internal string DefaultContextName => Registry.ResolveKey<ShaderGraphContext>().Name;

        [NonSerialized]
        Dictionary<RegistryKey, SGNodeUIData> m_NodeUIData = new();

        [NonSerialized]
        GraphDataContextNodeModel m_DefaultContextNode;

        [NonSerialized]
        public GraphModelStateComponent graphModelStateComponent;

        #region CopyPasteData
        /// <summary>
        /// ShaderGraphViewSelection and SGBlackboardViewSelection sets this to true
        /// prior to a cut operation as we need to handle it a little differently
        /// </summary>
        [NonSerialized]
        public bool isCutOperation;
        #endregion CopyPasteData

        // TODO: This should be customizable through the UI: https://jira.unity3d.com/browse/GSG-777
        // TODO: Default should be changed back to "Shader Graphs" before release: https://jira.unity3d.com/browse/GSG-1431
        string m_ShaderCategory = "Shader Graphs (SG2)";
        public string ShaderName => string.IsNullOrEmpty(m_ShaderCategory) ? Name : m_ShaderCategory + "/" + Name;

        // TODO: Not initialize this way, need to provide a more generic model context object with all necessary info.
        internal void InitModelForNewAsset(bool isSubGraph, LegacyTargetType targetType = LegacyTargetType.Blank)
        {
            var target = targetType switch
            {
                LegacyTargetType.Blank => null,
                LegacyTargetType.URPLit => URPTargetUtils.ConfigureURPLit(GraphHandler),
                LegacyTargetType.URPUnlit => URPTargetUtils.ConfigureURPUnlit(GraphHandler),
                _ => throw new ArgumentOutOfRangeException("ShaderGraphTemplate.m_TargetType")
            };

            this.isSubGraph = isSubGraph;
            if (!isSubGraph && target != null)
            {
                Targets.Add(target);
                // all target-based graphs have a Vert
                // TODO: https://jira.unity3d.com/browse/GSG-1290
                //var vertNode = this.CreateGraphDataContextNode("VertOut");
                //vertNode.Title = "Vertex Stage";
                //vertNode.Position = new Vector2(0, -180);

            }

            m_DefaultContextNode = this.CreateGraphDataContextNode(ShaderGraphAssetUtils.kMainEntryContextName);
            Init(isSubGraph);
        }

        public void Init(bool isSubGraph)
        {
            foreach (var addedTarget in Targets)
            {
                // at most there is only one target right now, so this solution is not robust.
                InitializeContextFromTarget(addedTarget.value);
            }

            GraphHandler.ReconcretizeAll();

            m_DefaultContextNode ??= GetMainContextNode();
            m_DefaultContextNode.Title = isSubGraph ? "Subgraph Outputs" : "Fragment Stage";
        }

        public override void OnEnable()
        {
            targetSettingsBox.OnEnable();
            base.OnEnable();
            mainPreviewData = new(Guid.ToString());
        }

        GraphDataContextNodeModel GetMainContextNode()
        {
            foreach (var node in NodeModels)
            {
                if (node is GraphDataContextNodeModel graphDataContextNodeModel && graphDataContextNodeModel.IsMainContextNode())
                    return graphDataContextNodeModel;
            }

            return null;
        }

        /// <summary>
        /// Used to retrieve the UI data associated with a node & ports on that node, via that node's registry key
        /// </summary>
        /// <param name="registryKey"></param>
        /// <returns></returns>
        public SGNodeUIData GetUIData(RegistryKey registryKey)
        {
            return m_NodeUIData[registryKey];
        }

        /// <summary>
        /// Called after the graph model is loaded
        /// Creates the application side UI data by extracting it from the UI Descriptors
        /// </summary>
        public void CreateUIData()
        {
            if (Stencil is ShaderGraphStencil stencil && m_NodeUIData == null)
            {
                foreach (var registryKey in RegistryInstance.Registry.BrowseRegistryKeys())
                {
                    var nodeUIInfo = stencil.GetUIHints(registryKey);
                    var functionDictionary = nodeUIInfo.SelectableFunctions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    var portInfoList = new List<SGPortUIData>();
                    foreach (var parameter in nodeUIInfo.Parameters)
                    {
                        portInfoList.Add(new SGPortUIData(
                                                    parameter.Name,
                                                    parameter.DisplayName,
                                                    parameter.Tooltip,
                                                    parameter.UseColor,
                                                    parameter.IsHdr,
                                                    false,      // TODO: Add flags for isStatic and isGradient to ParameterUIDescriptor
                                                    false,
                                                    parameter.UseSlider,
                                                    parameter.InspectorOnly,
                                                    parameter.Options));
                    }

                    var nodeUIData = new SGNodeUIData(
                                            nodeUIInfo.Version,
                                            nodeUIInfo.Name,
                                            nodeUIInfo.Tooltip,
                                            nodeUIInfo.Category,
                                            nodeUIInfo.Synonyms.ToArray(),
                                            nodeUIInfo.DisplayName,
                                            nodeUIInfo.HasPreview,
                                            functionDictionary,
                                            portInfoList.ToArray(),
                                            nodeUIInfo.FunctionSelectorLabel);
                    if(!m_NodeUIData.TryAdd(registryKey, nodeUIData))
                        Debug.LogWarning("Tried to add duplicate to Node UI Data with value: " + registryKey);
                }
            }
        }

        internal void InitializeContextFromTarget(Target target)
        {
            // TODO: we can assume we're using the standard SG config for now, but this is not good.
            ShaderGraphAssetUtils.RebuildContextNodes(GraphHandler, target);

            foreach (var contextNode in NodeModels.OfType<GraphDataContextNodeModel>())
            {
                contextNode.DefineNode();
            }
        }

        public override bool CanBeSubgraph() => isSubGraph;
        protected override Type GetWireType(PortModel toPort, PortModel fromPort)
        {
            return typeof(GraphDataEdgeModel);
        }
        public override Type GetSectionModelType()
        {
            return typeof(SectionModel);
        }

        public override WireModel CreateWire(PortModel toPort, PortModel fromPort, SerializableGUID guid = default)
        {
            PortModel resolvedEdgeSource;
            List<PortModel> resolvedEdgeDestinations;
            resolvedEdgeSource = HandleRedirectNodesCreation(toPort, fromPort, out resolvedEdgeDestinations);

            var edgeModel = base.CreateWire(toPort, fromPort, guid);
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

        public override void DeleteWires(IReadOnlyCollection<WireModel> edgeModels)
        {
            // Remove CLDS edges as well
            foreach (var edge in edgeModels)
            {
                if (edge.FromPort is GraphDataPortModel sourcePort && edge.ToPort is GraphDataPortModel destPort)
                    Disconnect(sourcePort, destPort);
            }
            base.DeleteWires(edgeModels);
        }

        public override void DeleteVariableDeclarations(IReadOnlyCollection<VariableDeclarationModel> variableModels, bool deleteUsages = true)
        {
            // var changedNodes = new Dictionary<GraphElementModel, IReadOnlyList<ChangeHint>>();

            // Remove any ports that correspond to this property on the property context
            // as it causes issues with future port compability tests if the junk isnt cleared
            foreach (var nodeModel in NodeModels)
            {
                if (nodeModel is GraphDataContextNodeModel contextNodeModel
                    && contextNodeModel.graphDataName == BlackboardContextName)
                {
                    GraphHandler.ReconcretizeNode(contextNodeModel.graphDataName);
                    contextNodeModel.DefineNode();

                    // changedNodes.Add(contextNodeModel, new[] { ChangeHint.Unspecified });
                }
            }

            // The referable entry this variable was backed by is removed in ShaderGraphCommandOverrides.HandleDeleteBlackboardItems()
            // In future we want to bring it here

            // var changeDescription = base.DeleteVariableDeclarations(variableModels, deleteUsages);
            // changeDescription.Union(null, changedNodes, null);
            // return changeDescription;
        }

        PortModel HandleRedirectNodesCreation(PortModel toPort, PortModel fromPort, out List<PortModel> resolvedDestinations)
        {
            var resolvedSource = fromPort;
            resolvedDestinations = new List<PortModel>();

            if (toPort is { NodeModel: RedirectNodeModel toRedir })
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
            return GraphHandler.TestConnection(
                src.owner.graphDataName, src.graphDataName,
                dst.owner.graphDataName, dst.graphDataName,
                RegistryInstance.Registry);
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
                src.owner.graphDataName, src.graphDataName,
                dst.owner.graphDataName, dst.graphDataName);
        }

        /// <summary>
        /// Disconnects two GraphData ports at the data level.
        /// </summary>
        /// <param name="src">Source port.</param>
        /// <param name="dst">Destination port.</param>
        public void Disconnect(GraphDataPortModel src, GraphDataPortModel dst)
        {
            GraphHandler.Disconnect(
                src.owner.graphDataName, src.graphDataName,
                dst.owner.graphDataName, dst.graphDataName);
        }

        static bool PortsFormCycle(PortModel fromPort, PortModel toPort)
        {
            var queue = new Queue<PortNodeModel>();
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

        protected override bool IsCompatiblePort(PortModel startPortModel, PortModel compatiblePortModel)
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

        // GTF tries to copy edges over on its own, we don't want to do that,
        // we mostly handle edge duplication on our side of things
        public override WireModel DuplicateWire(WireModel sourceEdge, AbstractNodeModel targetInputNode, AbstractNodeModel targetOutputNode)
        {
            return null;
        }

        /// <summary>
        /// Called by PasteSerializedDataCommand to handle node duplication
        /// </summary>
        /// <param name="sourceNodeModel"> The Original node we are duplicating, that has been JSON serialized/deserialized to create this instance </param>
        /// <param name="delta"> Position delta on the graph between original and duplicated node </param>
        public override AbstractNodeModel DuplicateNode(
            AbstractNodeModel sourceNodeModel,
            Vector2 delta)
        {
            var pastedNodeModel = sourceNodeModel.Clone();
            // Set GraphModel BEFORE OnDefineNode as it is commonly used during it
            pastedNodeModel.GraphModel = this;
            pastedNodeModel.AssignNewGuid();

            switch (pastedNodeModel)
            {
                // We don't want to be able to duplicate context nodes,
                // also they subclass from GraphDataNodeModel so need to handle first
                case GraphDataContextNodeModel:
                    return null;
                case GraphDataNodeModel newCopiedNode when sourceNodeModel is GraphDataNodeModel sourceGraphDataNode:
                {
                    newCopiedNode.graphDataName = newCopiedNode.Guid.ToString();

                    var sourceNodeHandler = GraphHandler.GetNode(sourceGraphDataNode.graphDataName);
                    if (isCutOperation // In a cut operation the original node handlers have since been deleted, so we need to add from scratch
                        || sourceNodeHandler == null) // If no node handler found, we're copying from a different graph so also add from scratch
                    {
                        GraphHandler.AddNode(sourceGraphDataNode.duplicationRegistryKey, newCopiedNode.graphDataName);
                    }
                    else
                    {
                        GraphHandler.DuplicateNode(sourceNodeHandler, false, newCopiedNode.graphDataName);
                    }

                    break;
                }
                case GraphDataVariableNodeModel { DeclarationModel: GraphDataVariableDeclarationModel declarationModel } newCopiedVariableNode:
                {
                    // if the blackboard property/keyword this variable node is referencing
                    // doesn't exist in the graph, it has probably been copied from another graph
                    if (!VariableDeclarations.Contains(declarationModel))
                    {
                        // Search for the equivalent property/keyword that GTF code
                        // will have created to replace the missing reference
                        newCopiedVariableNode.DeclarationModel = VariableDeclarations.FirstOrDefault(model => model.Guid == declarationModel.Guid);
                        // Restore the Guid from its graph data name (as currently we need to align the Guids and graph data names)
                        newCopiedVariableNode.graphDataName = new SerializableGUID(newCopiedVariableNode.graphDataName.Replace("_", String.Empty)).ToString();
                        // Make sure this reference is up to date
                        declarationModel = (GraphDataVariableDeclarationModel)newCopiedVariableNode.DeclarationModel;
                    }
                    else
                        newCopiedVariableNode.graphDataName = newCopiedVariableNode.Guid.ToString();

                    // Every time a variable node is duplicated, add a reference node pointing back
                    // to the property/keyword that is wrapped by the VariableDeclarationModel, on the CLDS level
                    GraphHandler.AddReferenceNode(newCopiedVariableNode.graphDataName, declarationModel.contextNodeName, declarationModel.graphDataName);
                    break;
                }
            }

            pastedNodeModel.Position += delta;
            AddNode(pastedNodeModel);
            pastedNodeModel.OnDuplicateNode(sourceNodeModel);

            if (pastedNodeModel is IGraphElementContainer container)
            {
                foreach (var element in container.GraphElementModels)
                    RegisterElement(element);
            }

            return pastedNodeModel;
        }

        public static PortModel FindInputPortByName(AbstractNodeModel nodeModel, string portID)
        {
            return ((NodeModel)nodeModel).InputsById.FirstOrDefault(input => input.Key == portID).Value;
        }

        public static PortModel FindOutputPortByName(AbstractNodeModel nodeModel, string portID)
        {
            return ((NodeModel)nodeModel).OutputsById.FirstOrDefault(input => input.Key == portID).Value;
        }

        public override TDeclType DuplicateGraphVariableDeclaration<TDeclType>(TDeclType sourceModel, bool keepGuid = false)
        {
            var sourceDataVariable = sourceModel as GraphDataVariableDeclarationModel;
            Assert.IsNotNull(sourceDataVariable);

            var sourceShaderGraphConstant = sourceDataVariable.InitializationModel as BaseShaderGraphConstant;
            Assert.IsNotNull(sourceShaderGraphConstant);

            var copiedVariable = sourceDataVariable.Clone();
            // Only assign new guids if there is a conflict,
            // this handles the case of a variable node being copied over to a graph where its source blackboard item doesn't exist yet
            // the blackboard item will be duplicated, but if a new guid gets the assigned that variable node now is invalid
            if (VariableDeclarations.Any(declarationModel => declarationModel.Guid == sourceDataVariable.Guid))
                copiedVariable.AssignNewGuid();
            else
                copiedVariable.SetGuid(sourceDataVariable.Guid);

            /* Init variable declaration model */
            InitVariableDeclarationModel(
                copiedVariable,
                copiedVariable.DataType,
                sourceDataVariable.Title,
                sourceDataVariable.Modifiers,
                sourceDataVariable.IsExposed,
                copiedVariable.InitializationModel,
                copiedVariable.Guid,
                null);

            /* Init variable constant value */
            if (copiedVariable.InitializationModel is BaseShaderGraphConstant baseShaderGraphConstant)
            {
                copiedVariable.CreateInitializationValue();
                copiedVariable.InitializationModel.ObjectValue = baseShaderGraphConstant.GetStoredValueForCopy();
            }

            AddVariableDeclaration(copiedVariable);

            if (sourceModel.ParentGroup != null && sourceModel.ParentGroup.GraphModel == this)
                sourceModel.ParentGroup.InsertItem(copiedVariable, -1);
            else
            {
                var section = GetSectionModel(Stencil.GetVariableSection(copiedVariable));
                section.InsertItem(copiedVariable, -1);
            }

            return (TDeclType)((VariableDeclarationModel)copiedVariable);
        }

        protected override VariableDeclarationModel InstantiateVariableDeclaration(
            Type variableTypeToCreate,
            TypeHandle variableDataType,
            string variableName,
            ModifierFlags modifierFlags,
            bool isExposed,
            Constant initializationModel,
            SerializableGUID guid,
            Action<VariableDeclarationModel, Constant> initializationCallback = null
        )
        {
            if (variableTypeToCreate != typeof(GraphDataVariableDeclarationModel))
            {
                return base.InstantiateVariableDeclaration(variableTypeToCreate, variableDataType, variableName, modifierFlags, isExposed, initializationModel, guid, initializationCallback);
            }

            var graphDataVar = new GraphDataVariableDeclarationModel();
            return InitVariableDeclarationModel(graphDataVar, variableDataType, variableName, modifierFlags, isExposed, initializationModel, guid, initializationCallback);
        }

        VariableDeclarationModel InitVariableDeclarationModel(
            GraphDataVariableDeclarationModel graphDataVar,
            TypeHandle variableDataType,
            string variableName,
            ModifierFlags modifierFlags,
            bool isExposed,
            Constant initializationModel,
            SerializableGUID guid,
            Action<VariableDeclarationModel, Constant> initializationCallback)
        {
            // If the guid starts with a number, it will produce an invalid identifier in HLSL.
            var fieldName = "_" + graphDataVar.Guid;
            var displayName = GenerateGraphVariableDeclarationUniqueName(variableName);

            var propertyContext = GraphHandler.GetNode(BlackboardContextName);
            Debug.Assert(propertyContext != null, "Material property context was missing from graph when initializing a variable declaration");

            isExposed &= ((ShaderGraphStencil)Stencil).IsExposable(variableDataType);
            ContextBuilder.AddReferableEntry(
                propertyContext,
                variableDataType.GetBackingDescriptor(),
                fieldName,
                GraphHandler.registry,
                isExposed ? ContextEntryEnumTags.PropertyBlockUsage.Included : ContextEntryEnumTags.PropertyBlockUsage.Excluded,
                source: isExposed ? ContextEntryEnumTags.DataSource.Global : ContextEntryEnumTags.DataSource.Constant,
                displayName: displayName);

            try
            {
                GraphHandler.ReconcretizeNode(propertyContext.ID.FullPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            graphDataVar.contextNodeName = BlackboardContextName;
            graphDataVar.graphDataName = fieldName;

            if (guid.Valid)
                graphDataVar.SetGuid(guid);
            graphDataVar.GraphModel = this;
            graphDataVar.DataType = variableDataType;
            graphDataVar.Title = displayName;
            graphDataVar.IsExposed = isExposed;
            graphDataVar.Modifiers = modifierFlags;

            initializationCallback?.Invoke(graphDataVar, initializationModel);

            return graphDataVar;
        }

        // TODO: (Sai) Would it be better to have a way to gather any variable nodes
        // linked to a blackboard item at a GraphHandler level instead of here?
        public IEnumerable<AbstractNodeModel> GetLinkedVariableNodes(string variableName)
        {
            return NodeModels.Where(
                node => node is GraphDataVariableNodeModel { VariableDeclarationModel: GraphDataVariableDeclarationModel variableDeclarationModel }
                    && variableDeclarationModel.graphDataName == variableName);
        }

        // TODO: Replace with a Preview Service side solution
        bool IsConnectedToTimeNode(GraphDataNodeModel nodeModel)
        {
            foreach (var inputEdge in nodeModel.GetIncomingEdges())
            {
                if (TryGetModelFromGuid(inputEdge.FromNodeGuid, out var inputNode)
                && inputNode is GraphDataNodeModel inputGraphDataNode)
                {
                    // Recursively traverse through all inputs upstream and get if connected to time node
                    if (inputGraphDataNode.DisplayTitle.Contains("Time") || IsConnectedToTimeNode(inputGraphDataNode))
                        return true;
                }
            }

            return false;
        }

        public bool DoesNodeRequireTime(string graphDataName)
        {
            // Special casing for main context node now as we don't use a GTF guid as its CLDS ID
            if (graphDataName == DefaultContextName)
                return IsConnectedToTimeNode(m_DefaultContextNode);

            return TryGetModelFromGuid(new SerializableGUID(graphDataName), out var elementModel)
                && elementModel is GraphDataNodeModel graphDataNodeModel && IsConnectedToTimeNode(graphDataNodeModel);
        }

        public bool DoesNodeRequireTime(GraphDataNodeModel graphDataNodeModel)
        {
            return IsConnectedToTimeNode(graphDataNodeModel);
        }

        // Temporarily hide some unfinished nodes: https://jira.unity3d.com/browse/GSG-1290
        // Should have a feature for managing what types/nodes are exposed i nbuil
        static readonly string[] blacklistNodeNames = new string[] {
            "CustomRenderTextureSelf",
            "CustomRenderTextureSize",
            "CustomRenderTextureSlice",
            "ParallaxOcclusionMapping",
            "LinearBlendSkinning",
            "Reference"
        };
        static readonly string[] blacklistCategories = new string[]
        {
            "DEFAULT_CATEGORY",
            "Test",
            "Tests"
        };

        public bool ShouldBeInSearcher(RegistryKey registryKey)
        {
            if (blacklistNodeNames.Contains(registryKey.Name))
                return false;
            var cat = RegistryInstance.GetNodeUIDescriptor(registryKey).Category;

            if (blacklistCategories.Any(e => cat.Contains(e)))
                return false;

            try
            {
                // TODO: RegistrInstance.Contains(Key)
                var nodeBuilder = RegistryInstance.GetNodeBuilder(registryKey);
                var registryFlags = nodeBuilder.GetRegistryFlags();
                return registryFlags.HasFlag(RegistryFlags.Func);
            }
            catch (Exception exception)
            {
                AssertHelpers.Fail("Failed due to exception:" + exception);
                return false;
            }
        }

        public override VariableNodeModel CreateVariableNode(VariableDeclarationModel declarationModel,
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

        protected override Type GetVariableDeclarationType() => typeof(GraphDataVariableDeclarationModel);
    }
}
