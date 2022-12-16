using System;
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

                Assert.IsTrue(TryGetNodeHandler(out var reader));
                // Store the registry key to use for node duplication
                duplicationRegistryKey = reader.GetRegistryKey();
                return reader.GetRegistryKey();
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

        protected GraphHandler graphHandler =>
            ((SGGraphModel)GraphModel).GraphHandler;

        ShaderGraphRegistry registry =>
            ((ShaderGraphStencil)GraphModel.Stencil).GetRegistry();

        public bool TryGetNodeHandler(out NodeHandler reader)
        {
            try
            {
                if (graphDataName == null)
                {
                    reader = registry.GetDefaultTopology(m_PreviewRegistryKey);
                    return true;
                }
                reader = graphHandler.GetNode(graphDataName);
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
        [ModelSetting]
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
            graphModel.GetNodeViewModel(registryKey, out var nodeViewModel);
            return nodeViewModel;
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
            NodeHandler nodeHandler = graphHandler.GetNode(graphDataName);
            string fieldName = NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME;
            FieldHandler selectedFunctionField = nodeHandler.GetField<string>(fieldName);
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

        protected override void OnDefineNode()
        {
            if (!TryGetNodeHandler(out var nodeReader))
            {
                Debug.LogErrorFormat("Node \"{0}\" is missing from graph data", graphDataName);
                return;
            }

            NodeUIDescriptor nodeUIDescriptor = new();
            if(GraphModel.Stencil is ShaderGraphStencil shaderGraphStencil)
                nodeUIDescriptor = shaderGraphStencil.GetUIHints(registryKey, nodeReader);

            bool nodeHasPreview = nodeUIDescriptor.HasPreview && existsInGraphData;

            // TODO: Convert this to a NodePortsPart maybe?
            foreach (var portReader in nodeReader.GetPorts().Where(e => !e.LocalID.Contains("out_")))
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
                var nodeId = nodeReader.ID;
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
                    constant.BindTo(nodeId.LocalPath, portReader.LocalID);
                }

                if (isInput)
                {
                    var newPortModel = this.AddDataInputPort(portReader.LocalID, type, orientation: orientation, initializationCallback: initCallback);
                    // If we were deserialized, the InitCallback doesn't get triggered.
                    if (newPortModel != null)
                        ((BaseShaderGraphConstant)newPortModel.EmbeddedValue).BindTo(nodeReader.ID.LocalPath, portReader.LocalID);
                }
                else
                    this.AddDataOutputPort(portReader.LocalID, type, orientation: orientation);

            }

            HasPreview = nodeHasPreview;
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
