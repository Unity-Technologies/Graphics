using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using PreviewRenderMode = PreviewService.PreviewRenderMode;

    /// <summary>
    /// GraphDataNodeModel is a model for a node backed by graph data.
    /// It can be used for a node on the graph (with an assigned graph data name)
    /// or a searcher preview (with only an assigned registry key).
    /// </summary>
    class SGNodeModel : NodeModel, IGraphDataOwner, IPreviewUpdateListener
    {
        [SerializeField]
        string m_GraphDataName;

        /// <summary>
        /// Graph data name associated with this node.
        /// If null, this node is a searcher preview with type determined by the
        /// registryKey property.
        /// </summary>
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

        RegistryKey m_PreviewRegistryKey;

        /// <summary>
        /// This node's registry key. If graphDataName is set, this is read from the graph. Otherwise, it is set
        /// manually using SetPreviewRegistryKey.
        /// </summary>
        public RegistryKey registryKey
        {
            get
            {
                if (!existsInGraphData)
                    return m_PreviewRegistryKey;

                Debug.Assert(TryGetNodeHandler(out var handler));

                // Store the registry key to use for node duplication
                duplicationRegistryKey = handler.GetRegistryKey();
                return handler.GetRegistryKey();
            }
        }

        /// <summary>
        /// GTF handles copy/pasting of graph elements by serializing the original graph element models to JSON
        /// and deserializing that JSON to get an instance that can be cloned to create our new node model
        /// We need a field that can copy the registry key in order to use for creating the duplicated node
        /// See ShaderGraphModel.DuplicateNode() and ViewSelection.DuplicateSelection
        /// </summary>
        [field: SerializeField]
        public RegistryKey duplicationRegistryKey { get; private set; }

        /// <summary>
        /// Determines whether or not this node has a valid backing representation at the data layer. If false, this
        /// node should be treated as a searcher preview.
        /// </summary>
        public bool existsInGraphData =>
            m_GraphDataName != null && TryGetNodeHandler(out _);

        GraphHandler graphHandler =>
            ((SGGraphModel)GraphModel).GraphHandler;

        ShaderGraphRegistry registry =>
            ((SGGraphModel)GraphModel).RegistryInstance;

        public bool TryGetNodeHandler(out NodeHandler reader)
        {
            try
            {
                reader = graphDataName == null ?
                    registry.GetDefaultTopology(m_PreviewRegistryKey) :
                    graphHandler.GetNode(graphDataName);

                return reader != null;
            }
            catch (Exception exception)
            {
                AssertHelpers.Fail("Failed to retrieve node due to exception:" + exception);
                reader = null;
                return false;
            }
        }

        public virtual bool HasPreview { get; private set; }

        // By default every node's preview is visible
        [SerializeField]
        bool m_IsPreviewExpanded = true;

        // By default every node's preview uses the inherit mode
        [SerializeField]
        [NodeOption]
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

        internal SGGraphModel graphModel => GraphModel as SGGraphModel;

        internal int currentVersion => registryKey.Version;

        internal int latestAvailableVersion
        {
            get
            {
                var latest = 0;

                foreach (var key in graphHandler.registry.BrowseRegistryKeys())
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

        public void UpgradeToLatestVersion()
        {
            var nodeHandler = graphHandler.GetNode(graphDataName);

            if (latestAvailableVersion < currentVersion)
            {
                Debug.LogError($"Node version ({currentVersion}) is greater than latest version in registry ({latestAvailableVersion})");
                return;
            }

            if (latestAvailableVersion == currentVersion)
            {
                return;
            }

            var newKey = new RegistryKey {Name = registryKey.Name, Version = latestAvailableVersion};
            nodeHandler.SetMetadata(GraphDelta.GraphDelta.kRegistryKeyName, newKey);
            try
            {
                graphHandler.ReconcretizeNode(graphDataName);
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

        /// <summary>
        /// Sets the registry key used when previewing this node. Has no effect if graphDataName has been set.
        /// </summary>
        /// <param name="key">Registry key used to preview this node.</param>
        public void SetSearcherPreviewRegistryKey(RegistryKey key)
        {
            m_PreviewRegistryKey = key;
        }

        public void ChangeNodeFunction(string newFunctionName)
        {
            if (!existsInGraphData)
            {
                Debug.LogError("Attempted to change the function of a node that doesn't exist on the graph.");
                return;
            }

            // existsInGraphData implies that TryGetNodeHandler will succeed.
            Debug.Assert(TryGetNodeHandler(out var nodeHandler));

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
                graphHandler.ReconcretizeNode(graphDataName);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            DefineNode();
        }

        /// <summary>
        /// Sets a port's value from its parameter descriptor's Options list.
        /// </summary>
        /// <param name="portName">Port name.</param>
        /// <param name="optionIndex">Index of the Option in the port's parameter descriptor to use.</param>
        public void SetPortOption(string portName, int optionIndex)
        {
            if (!TryGetNodeHandler(out var handler)) return;
            var parameterInfo = GetViewModel().GetParameterInfo(portName);
            var (_, optionValue) = parameterInfo.Options[optionIndex];

            if (optionValue is not ReferenceValueDescriptor desc)
            {
                Debug.LogError("SetPortOption not implemented for options that are not ReferenceValueDescriptors");
                return;
            }

            var port = handler.GetPort(portName);
            var existing = GetCurrentPortOption(portName);
            if (existing != -1)
            {
                var (_, existingValue) = parameterInfo.Options[existing];
                if (existingValue is ReferenceValueDescriptor existingDesc)
                {
                    graphHandler.graphDelta.RemoveDefaultConnection(existingDesc.ContextName, port.ID, registry.Registry);
                }
            }

            graphHandler.graphDelta.AddDefaultConnection(desc.ContextName, port.ID, registry.Registry);
            graphHandler.ReconcretizeNode(graphDataName);
        }

        /// <summary>
        /// Gets the currently selected option for the given port.
        /// </summary>
        /// <param name="portName">Port name.</param>
        /// <returns>Index into the Options list for the given port, or -1 if there are no options or no option is selected.</returns>
        public int GetCurrentPortOption(string portName)
        {
            var paramInfo = GetViewModel().GetParameterInfo(portName);
            if (!existsInGraphData) return 0;  // default to first option

            if (!TryGetNodeHandler(out var handler)) return -1;
            var port = handler.GetPort(portName);

            var connection = graphHandler.graphDelta.GetDefaultConnectionToPort(port.ID);
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

        public void OnPreviewShaderCompiling()
        {
            PreviewShaderIsCompiling = true;
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

            var selectedFunctionID = string.Empty;

            // If the node has multiple possible topologies, show the selector.
            if (nodeUIInfo.SelectableFunctions.Count > 0)
            {
                var fieldName = NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME;
                var selectedFunctionField = node.GetField<string>(fieldName);
                selectedFunctionID = selectedFunctionField.GetData<string>();
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
                nodeUIInfo.FunctionSelectorLabel,
                selectedFunctionID);
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

        protected override void OnDefineNode()
        {
            if (!TryGetNodeHandler(out var nodeHandler))
            {
                Debug.LogErrorFormat("Node \"{0}\" is missing from graph data", graphDataName);
                return;
            }

            var nodeUIDescriptor = registry.GetNodeUIDescriptor(registryKey, nodeHandler);
            var nodeHasPreview = nodeUIDescriptor.HasPreview && existsInGraphData;
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
                    graphModel.GraphHandler.AddNode(sourceGraphDataNode.duplicationRegistryKey, graphDataName);
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
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new SGPortModel(this, direction, orientation, portName ?? "", portType, dataType, portId, options);
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
    }
}
