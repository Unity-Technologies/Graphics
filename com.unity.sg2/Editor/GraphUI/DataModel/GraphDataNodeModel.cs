using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using PreviewRenderMode = HeadlessPreviewManager.PreviewRenderMode;

    /// <summary>
    /// GraphDataNodeModel is a model for a node backed by graph data.
    /// It can be used for a node on the graph (with an assigned graph data name)
    /// or a searcher preview (with only an assigned registry key).
    /// </summary>
    public class GraphDataNodeModel : NodeModel, IGraphDataOwner
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
            ((ShaderGraphModel)GraphModel).GraphHandler;

        ShaderGraphRegistry registry =>
            ((ShaderGraphStencil)GraphModel.Stencil).GetRegistry();

        // Need to establish a mapping from port readers to port models,
        // as there currently is no other way to know if they both represent the same underlying port
        // This is an issue because in GTF we only know about port models, but for the preview system we only care about port readers
        Dictionary<PortHandler, IPortModel> m_PortMappings = new();
        public Dictionary<PortHandler, IPortModel> PortMappings => m_PortMappings;

        public bool TryGetPortModel(PortHandler portReader, out IPortModel matchingPortModel)
        {
            foreach (var nodePortReader in PortMappings.Keys)
            {
                if (nodePortReader.LocalID == portReader.LocalID)
                    return PortMappings.TryGetValue(nodePortReader, out matchingPortModel);
            }

            matchingPortModel = null;
            return false;
        }

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

        public bool NodeRequiresTime { get; private set; }

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

        internal int currentVersion => registryKey.Version;

        internal int latestAvailableVersion
        {
            get
            {
                try
                {
                    return graphHandler.registry.BrowseRegistryKeys()
                        .Where(otherKey => otherKey.Name == registryKey.Name)
                        .Select(otherKey => otherKey.Version)
                        .Max();
                }
                catch (Exception e)
                {
                    Debug.Log(e + " thrown while trying to retrieve latestAvailableVersion");
                    return -1;
                }
            }
        }

        public GraphDataNodeModel()
        {
            NodePreviewMode = PreviewRenderMode.Inherit;
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
            m_PortMappings.Clear();

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
                void initCallback(IConstant e)
                {
                    var constant = e as BaseShaderGraphConstant;
                    if (e == null)
                        return;
                    var shaderGraphModel = ((ShaderGraphModel)GraphModel);
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

                IPortModel newPortModel = null;
                if (isInput)
                {
                    newPortModel = this.AddDataInputPort(portReader.LocalID, type, orientation: orientation, initializationCallback: initCallback);
                    // If we were deserialized, the InitCallback doesn't get triggered.
                    if (newPortModel != null)
                        ((BaseShaderGraphConstant)newPortModel.EmbeddedValue).Initialize(((ShaderGraphModel)GraphModel), nodeReader.ID.LocalPath, portReader.LocalID);
                }
                else
                    newPortModel = this.AddDataOutputPort(portReader.LocalID, type, orientation: orientation);

                m_PortMappings.Add(portReader, newPortModel);
            }

            NodeRequiresTime = ShaderGraphModel.DoesNodeRequireTime(this);
            HasPreview = nodeHasPreview;
        }

        protected override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName,
            PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new GraphDataPortModel
            {
                Direction = direction,
                Orientation = orientation,
                PortType = portType,
                DataTypeHandle = dataType,
                Title = portName ?? "",
                UniqueName = portId,
                Options = options,
                NodeModel = this,
                GraphModel = GraphModel
            };
        }
    }
}
