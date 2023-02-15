using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using PreviewRenderMode = PreviewService.PreviewRenderMode;

    /// <summary>
    /// GraphDataNodeModel is a model for a node backed by graph data.
    /// It can be used for a node on the graph (with an assigned graph data name)
    /// or a searcher preview (with only an assigned registry key).
    /// </summary>
    [Serializable]
    class SGNodeModel : NodeModel, IGraphDataOwner<SGNodeModel>, IPreviewUpdateListener
    {
        [SerializeField]
        RegistryKey m_RegistryKey;

        [SerializeField]
        string m_GraphDataName;

        /// <summary>
        /// The <see cref="IGraphDataOwner{T}"/> interface for this object.
        /// </summary>
        public IGraphDataOwner<SGNodeModel> graphDataOwner => this;

        /// <summary>
        /// The identifier/unique name used to represent this entity and retrieve info. regarding it from CLDS.
        /// </summary>
        /// <remarks>If null, the node is a preview node (show in the searcher)  with type determined by the
        /// <see cref="registryKey"/> property.</remarks>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        public override string DisplayTitle
        {
            get
            {
                var baseDisplayTitle = base.DisplayTitle;
                if (latestAvailableVersion > currentVersion)
                    baseDisplayTitle += $" (Legacy v{currentVersion})";
                return baseDisplayTitle;
            }
        }

        /// <summary>
        /// The <see cref="RegistryKey"/> that represents the concrete type within the Registry, of this object.
        /// </summary>
        public RegistryKey registryKey
        {
            get
            {
                if (!m_RegistryKey.Valid())
                {
                    m_RegistryKey = this.GetRegistryKeyFromNodeHandler();
                }

                return m_RegistryKey;
            }
            private set => m_RegistryKey = value;
        }

        internal void SyncRegistryKeyFromNodeHandler()
        {
            m_RegistryKey = this.GetRegistryKeyFromNodeHandler();
        }

        public NodeHandler GetNodeHandler()
        {
            // Use the default topology handler for preview nodes.
            var isPreview = graphDataName == null;
            return isPreview ?
                graphDataOwner.registry.GetDefaultTopology(registryKey) :
                graphDataOwner.graphHandler.GetNode(graphDataName);
        }

        public virtual bool HasPreview { get; private set; }

        // By default every node's preview is visible
        [SerializeField]
        bool m_IsPreviewExpanded = true;

        // By default every node's preview uses the inherit mode
        [SerializeField]
        [NodeOption(true)]
        [Tooltip("Controls the way the preview output is rendered for this node")]
        PreviewRenderMode m_NodePreviewMode;
        public PreviewRenderMode NodePreviewMode
        {
            get => m_NodePreviewMode;
            set => m_NodePreviewMode = value;
        }

        public Texture PreviewTexture { get; private set; }

        public bool PreviewShaderIsCompiling { get; private set; }

        public bool IsPreviewExpanded
        {
            get => m_IsPreviewExpanded;
            set => m_IsPreviewExpanded = value;
        }

        int m_DismissedUpgradeVersion;
        public int dismissedUpgradeVersion
        {
            get => m_DismissedUpgradeVersion;
            set => m_DismissedUpgradeVersion = value;
        }

        List<string> m_Modes = new();
        public override List<string> Modes => m_Modes;

        internal SGGraphModel graphModel => GraphModel as SGGraphModel;

        internal int currentVersion => registryKey.Version;

        internal int latestAvailableVersion
        {
            get
            {
                var latest = 0;

                foreach (var key in graphDataOwner.graphHandler.registry.BrowseRegistryKeys())
                {
                    if (key.Name != registryKey.Name)
                    {
                        continue;
                    }

                    if (key.Version > latest)
                    {
                        latest = key.Version;
                    }
                }

                return latest;
            }
        }

        [NonSerialized]
        SGNodeViewModel m_NodeViewModel;

        /// </summary>
        /// <param name="key">The CLDS registry key to use for this node.</param>
        /// <param name="spawnFlags">The node spawn flags.</param>
        public void Initialize(RegistryKey key, SpawnFlags spawnFlags)
        {
            registryKey = key;

            if (!spawnFlags.IsOrphan())
            {
                graphDataName = Guid.ToString();
                graphDataOwner.graphHandler.AddNode(registryKey, graphDataName);
            }
        }

        public void UpgradeToLatestVersion()
        {
            if (!graphDataOwner.existsInGraphData)
            {
                return;
            }

            if (latestAvailableVersion < currentVersion)
            {
                Debug.LogError($"Node version ({currentVersion}) is greater than latest version in registry ({latestAvailableVersion})");
                return;
            }

            if (latestAvailableVersion == currentVersion)
            {
                return;
            }

            registryKey = new RegistryKey {Name = registryKey.Name, Version = latestAvailableVersion};
            var nodeHandler = GetNodeHandler();
            nodeHandler.SetMetadata(GraphDelta.GraphDelta.kRegistryKeyName, registryKey);
            try
            {
                graphDataOwner.graphHandler.ReconcretizeNode(graphDataName);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            DefineNode();
        }

        public SGNodeViewModel GetViewModel()
        {
            return m_NodeViewModel;
        }

        internal void ChangeNodeFunction(string newFunctionName)
        {
            if (!graphDataOwner.existsInGraphData)
            {
                return;
            }

            var nodeHandler = GetNodeHandler();
            var fieldName = NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME;
            var selectedFunctionField = nodeHandler.GetField<string>(fieldName);

            if (selectedFunctionField == null)
            {
                Debug.LogError("Unable to update selected function. Node has no selected function field.");
                return;
            }
            selectedFunctionField.SetData(newFunctionName);
            // TODO (Brett) Directly calling reconcretize should not be needed
            // because the field is set up with reconcretize on change.
            // See: NodeDescriptorNodeBuilder.BuildNode
            try
            {
                graphDataOwner.graphHandler.ReconcretizeNode(graphDataName);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            DefineNode();
        }

        public override void ChangeMode(int newModeIndex)
        {
            if (newModeIndex < 0 || newModeIndex >= Modes.Count)
            {
                Debug.LogError("Unable to update selected function. Index is out of bounds.");
                return;
            }
            ChangeNodeFunction(Modes[newModeIndex]);
            base.ChangeMode(newModeIndex);
        }

        /// <summary>
        /// Sets a port's value from its parameter descriptor's Options list.
        /// </summary>
        /// <param name="portName">Port name.</param>
        /// <param name="optionIndex">Index of the Option in the port's parameter descriptor to use.</param>
        public void SetPortOption(string portName, int optionIndex)
        {
            // If not backed by real data (i.e., we are a searcher preview), changing options doesn't make sense.
            if (!graphDataOwner.existsInGraphData)
            {
                return;
            }

            var nodeHandler = GetNodeHandler();
            var parameterInfo = GetViewModel().GetParameterInfo(portName);
            var (_, optionValue) = parameterInfo.Options[optionIndex];

            if (optionValue is not ReferenceValueDescriptor desc)
            {
                Debug.LogError("SetPortOption not implemented for options that are not ReferenceValueDescriptors");
                return;
            }

            var port = nodeHandler.GetPort(portName);
            var existing = GetCurrentPortOption(portName);
            if (existing != -1)
            {
                var (_, existingValue) = parameterInfo.Options[existing];
                if (existingValue is ReferenceValueDescriptor existingDesc)
                {
                    graphDataOwner.graphHandler.graphDelta.RemoveDefaultConnection(existingDesc.ContextName, port.ID, graphDataOwner.registry.Registry);
                }
            }

            graphDataOwner.graphHandler.graphDelta.AddDefaultConnection(desc.ContextName, port.ID, graphDataOwner.registry.Registry);
            graphDataOwner.graphHandler.ReconcretizeNode(graphDataName);
        }

        /// <summary>
        /// Gets the currently selected option for the given port.
        /// </summary>
        /// <param name="portName">Port name.</param>
        /// <returns>Index into the Options list for the given port, or -1 if there are no options or no option is selected.</returns>
        public int GetCurrentPortOption(string portName)
        {
            if (!graphDataOwner.TryGetNodeHandler(out var handler)) return -1;
            if (string.IsNullOrEmpty(m_GraphDataName)) return 0;  // default to first option

            var paramInfo = GetViewModel().GetParameterInfo(portName);
            if (paramInfo.Options == null || paramInfo.Options.Count < 1) return -1;

            var port = handler.GetPort(portName);

            var connection = graphDataOwner.graphHandler.graphDelta.GetDefaultConnectionToPort(port.ID);
            if (connection == null) return -1;

            for (var i = 0; i < paramInfo.Options.Count; i++)
            {
                var (_, value) = paramInfo.Options[i];
                if (value is not ReferenceValueDescriptor desc) continue;
                if (connection == desc.ContextName) return i;
            }

            return -1;
        }

        public void OnPreviewTextureUpdated(Texture newTexture)
        {
            PreviewTexture = newTexture;
            PreviewShaderIsCompiling = false;
        }

        SGNodeViewModel CreateNodeViewModel(NodeUIDescriptor nodeUIInfo, NodeHandler node)
        {
            var portViewModels = new List<SGPortViewModel>();

            // By default we assume all types need preview output, unless they opt out
            var showPreviewForType = true;

            foreach (var parameter in nodeUIInfo.Parameters)
            {
                var portHandler = node.GetPort(parameter.Name);

                // Current topology might not display all parameters.
                if (portHandler == null)
                {
                    continue;
                }

                if (CreatePortViewModel(portHandler, parameter, out var portViewModel, out showPreviewForType))
                {
                    portViewModels.Add(portViewModel);
                }
            }

            var shouldShowPreview = nodeUIInfo.HasPreview && showPreviewForType;
            var functionDictionary = new Dictionary<string, string>(nodeUIInfo.SelectableFunctions);

            return new SGNodeViewModel(
                nodeUIInfo.Version,
                nodeUIInfo.Name,
                nodeUIInfo.Tooltip,
                nodeUIInfo.Category,
                nodeUIInfo.Synonyms.ToArray(),
                nodeUIInfo.DisplayName,
                shouldShowPreview,
                functionDictionary,
                portViewModels.ToArray(),
                nodeUIInfo.FunctionSelectorLabel);
        }

        /// <summary>
        /// Returns true if we should create a view model for this port, false if it can be skipped
        /// </summary>
        /// <param name="portInfo"> Library port handler for the port</param>
        /// <param name="parameter"> Library-side data struct containing UI info. of the port </param>
        /// <param name="portViewModel"> Tool-side data struct that will contain all UI info of port </param>
        /// <param name="showPreviewForType"> Flag that controls if this port opts into preview output</param>
        bool CreatePortViewModel(PortHandler portInfo, ParameterUIDescriptor parameter, out SGPortViewModel portViewModel, out bool showPreviewForType)
        {
            showPreviewForType = true;
            portViewModel = new SGPortViewModel();

            if (portInfo == null || !portInfo.IsHorizontal)
            {
                return false;
            }

            var staticField = portInfo.GetTypeField().GetSubField<bool>("IsStatic");
            var typeField = portInfo.GetTypeField();
            var typeKey = typeField.GetRegistryKey();

            var isStatic = staticField?.GetData() ?? false;

            var sgRegistry = ((SGGraphModel)GraphModel).RegistryInstance;
            var isGradientType = typeKey.Name == sgRegistry.ResolveKey<GradientType>().Name;
            var isGraphType = typeKey.Name == sgRegistry.ResolveKey<GraphType>().Name;

            if (typeField == null)
            {
                return false;
            }

            var componentLength = (ComponentLength)GraphTypeHelpers.GetLength(typeField);
            var numericType = (NumericType)GraphTypeHelpers.GetPrimitive(typeField);

            var isMatrixType = GraphTypeHelpers.GetHeight(portInfo.GetTypeField()) > GraphType.Height.One;
            var matrixHeight = (int)GraphTypeHelpers.GetHeight(portInfo.GetTypeField());

            if (!isGraphType || portInfo.IsInput)
            {
                showPreviewForType = false;
            }

            portViewModel = new SGPortViewModel(
                parameter.Name,
                parameter.DisplayName,
                parameter.Tooltip,
                parameter.UseColor,
                parameter.IsHdr,
                isStatic,
                isGradientType,
                componentLength,
                numericType,
                isMatrixType,
                matrixHeight,
                parameter.UseSlider,
                parameter.InspectorOnly,
                parameter.Options);

            return true;
        }

        protected override void DefineNodeOptions()
        {
            if (!graphDataOwner.TryGetNodeHandler(out var nodeHandler))
            {
                Debug.LogErrorFormat("Node \"{0}\" is missing from graph data", graphDataName);
                return;
            }

            var nodeUIDescriptor = graphDataOwner.registry.GetNodeUIDescriptor(registryKey, nodeHandler);

            // If the node has selectable functions but does not have modes, the functions are part of a node option.
            if (!nodeUIDescriptor.HasModes && nodeUIDescriptor.SelectableFunctions.Count > 0)
            {
                var selectedFunctionField = nodeHandler.GetField<string>(NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME);
                AddNodeOption(
                    nodeUIDescriptor.FunctionSelectorLabel,
                    TypeHandle.String,
                    c => ChangeNodeFunction(c.ObjectValue.ToString()),
                    initializationCallback: c => c.ObjectValue = selectedFunctionField.GetData(),
                    attributes: new Attribute[] { new EnumAttribute(nodeUIDescriptor.SelectableFunctions.Keys.ToArray()) });
            }
        }

        protected override void OnDefineNode()
        {
            if (!graphDataOwner.TryGetNodeHandler(out var nodeHandler))
            {
                Debug.LogErrorFormat("Node \"{0}\" is missing from graph data", graphDataName);
                return;
            }

            var nodeUIDescriptor = graphDataOwner.registry.GetNodeUIDescriptor(registryKey, nodeHandler);

            if (nodeUIDescriptor.HasModes && m_Modes.Count == 0 && nodeUIDescriptor.SelectableFunctions.Count > 0)
                m_Modes = nodeUIDescriptor.SelectableFunctions.Select(s => s.Key).ToList();

            var nodeHasPreview = nodeUIDescriptor.HasPreview && graphDataOwner.existsInGraphData;
            m_NodeViewModel = CreateNodeViewModel(nodeUIDescriptor, nodeHandler);

            // TODO: Convert this to a NodePortsPart maybe?
            foreach (var portReader in nodeHandler.GetPorts().Where(e => !e.LocalID.Contains("out_")))
            {
                if (!portReader.IsHorizontal)
                    continue;
                var staticField = portReader.GetTypeField().GetSubField<bool>("IsStatic");
                var localField = portReader.GetTypeField().GetSubField<bool>("IsLocal");
                if (staticField != null && staticField.GetData()) continue;
                if (localField != null && localField.GetData()) continue;

                var isInput = portReader.IsInput;
                var orientation = portReader.IsHorizontal ? PortOrientation.Horizontal : PortOrientation.Vertical;

                // var type = ShaderGraphTypes.GetTypeHandleFromKey(portReader.GetRegistryKey());
                var type = ShaderGraphExampleTypes.GetGraphType(portReader);
                var nodeId = nodeHandler.ID;
                void initCallback(Constant e)
                {
                    var constant = e as BaseShaderGraphConstant;
                    if (e == null)
                        return;
                    var shaderGraphModel = ((SGGraphModel)GraphModel);
                    var handler = shaderGraphModel.GraphHandler;
                    try
                    {
                        var possiblyNodeReader = handler.GetNode(nodeId);
                    }
                    catch
                    {
                        handler = shaderGraphModel.RegistryInstance.DefaultTopologies;
                    }
                    // don't do this, we should have a fixed way of pathing into a port's type information as opposed to its header/port data.
                    // For now, we'll fail to find the property, fall back to the port's body, which will parse it's subfields and populate constants appropriately.
                    // Not sure how that's going to work for data that's from a connection!
                    constant.Initialize(shaderGraphModel, nodeId.LocalPath, portReader.LocalID);
                }

                if (isInput)
                {
                    var newPortModel = this.AddDataInputPort(portReader.LocalID, type, orientation: orientation, initializationCallback: initCallback);
                    // If we were deserialized, the InitCallback doesn't get triggered.
                    if (newPortModel != null)
                        ((BaseShaderGraphConstant)newPortModel.EmbeddedValue).Initialize(((SGGraphModel)GraphModel), nodeHandler.ID.LocalPath, portReader.LocalID);
                }
                else
                    this.AddDataOutputPort(portReader.LocalID, type, orientation: orientation);

            }

            HasPreview = nodeHasPreview;
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            if (sourceNode is SGNodeModel sourceGraphDataNode)
            {
                graphDataName = Guid.ToString();

                var sourceNodeHandler = graphModel.GraphHandler.GetNode(sourceGraphDataNode.graphDataName);

                if (sourceNodeHandler == null) // If no node handler found, it is a new node for this graph
                {
                    graphModel.GraphHandler.AddNode(sourceGraphDataNode.registryKey, graphDataName);
                }
                else
                {
                    graphModel.GraphHandler.DuplicateNode(sourceNodeHandler, false, graphDataName);
                }
            }

            base.OnDuplicateNode(sourceNode);
        }

        protected override PortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName,
            PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options, Attribute[] attributes)
        {
            return new SGPortModel(this, direction, orientation, portName ?? "", portType, dataType, portId, options, attributes);
        }

        public void HandlePreviewTextureUpdated(Texture newPreviewTexture)
        {
            OnPreviewTextureUpdated(newPreviewTexture);
            CurrentVersion++;
        }

        public void HandlePreviewShaderErrors(ShaderMessage[] shaderMessages)
        {
            // TODO: Handle displaying shader error messages
            throw new NotImplementedException();
        }

        public int CurrentVersion { get; private set; }

        public string ListenerID => m_GraphDataName;

        /// <inheritdoc />
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            if (graphDataOwner.TryGetNodeHandler(out var reader))
            {
                m_RegistryKey = reader.GetRegistryKey();
            }
        }
    }
}
