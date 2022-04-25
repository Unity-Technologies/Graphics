using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using PreviewRenderMode = HeadlessPreviewManager.PreviewRenderMode;

    /// <summary>
    /// GraphDataNodeModel is a model for a node backed by graph data. It can be used for a node on the graph (with
    /// an assigned graph data name) or a searcher preview (with only an assigned registry key).
    /// </summary>
    public class GraphDataNodeModel : NodeModel, IGraphDataOwner
    {
        [SerializeField]
        string m_GraphDataName;

        /// <summary>
        /// Graph data name associated with this node. If null, this node is a searcher preview with type determined
        /// by the registryKey property.
        /// </summary>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
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
                if (!existsInGraphData) return m_PreviewRegistryKey;

                Assert.IsTrue(TryGetNodeReader(out var reader));
                return reader.GetRegistryKey();
            }
        }

        /// <summary>
        /// Determines whether or not this node has a valid backing representation at the data layer. If false, this
        /// node should be treated as a searcher preview.
        /// </summary>
        public bool existsInGraphData =>
            m_GraphDataName != null && TryGetNodeReader(out _);

        GraphHandler graphHandler =>
            ((ShaderGraphModel)GraphModel).GraphHandler;

        Registry registry =>
            ((ShaderGraphStencil)GraphModel.Stencil).GetRegistry();

        // Need to establish a mapping from port readers to port models,
        // as there currently is no other way to know if they both represent the same underlying port
        // This is an issue because in GTF we only know about port models, but for the preview system we only care about port readers
        Dictionary<PortHandler, IPortModel> m_PortMappings = new();
        public Dictionary<PortHandler, IPortModel> PortMappings => m_PortMappings;

        public bool TryGetNodeWriter(out NodeHandler writer)
        {
            if (graphDataName == null)
            {
                writer = null;
                return false;
            }

            writer = graphHandler.GetNode(graphDataName);
            return writer != null;
        }

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

        public bool TryGetNodeReader(out NodeHandler reader)
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

        public bool HasPreview { get; private set; }

        // By default every node's preview is visible
        // TODO: Handle preview state serialization
        [SerializeField]
        bool m_IsPreviewExpanded = true;

        // By default every node's preview uses the inherit mode
        public PreviewRenderMode NodePreviewMode { get; set; }

        public Texture PreviewTexture { get; private set; }

        public bool PreviewShaderIsCompiling { get; private set; }

        public GraphDataNodeModel()
        {
            NodePreviewMode = PreviewRenderMode.Inherit;
        }

        public bool IsPreviewExpanded
        {
            get => m_IsPreviewExpanded;
            set => m_IsPreviewExpanded = value;
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
            nodeHandler.SetMetadata(
                NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME,
                newFunctionName);
            graphHandler.ReconcretizeNode(graphDataName, registry);
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
            if (!TryGetNodeReader(out var nodeReader))
            {
                Debug.LogErrorFormat("Node \"{0}\" is missing from graph data", graphDataName);
                return;
            }

            NodeUIDescriptor nodeUIDescriptor = new();
            if(GraphModel.Stencil is ShaderGraphStencil shaderGraphStencil)
                nodeUIDescriptor = shaderGraphStencil.GetUIHints(registryKey);

            bool nodeHasPreview = nodeUIDescriptor.HasPreview && existsInGraphData;
            m_PortMappings.Clear();

            // TODO: Convert this to a NodePortsPart maybe?
            foreach (var portReader in nodeReader.GetPorts())
            {
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
                    var shaderGraphModel = ((ShaderGraphModel)GraphModel);
                    var handler = shaderGraphModel.GraphHandler;
                    try
                    {
                        var possiblyNodeReader = handler.GetNode(nodeId);
                    }
                    catch
                    {
                        handler = shaderGraphModel.RegistryInstance.defaultTopologies;
                    }
                    // don't do this, we should have a fixed way of pathing into a port's type information as opposed to its header/port data.
                    // For now, we'll fail to find the property, fall back to the port's body, which will parse it's subfields and populate constants appropriately.
                    // Not sure how that's going to work for data that's from a connection!
                    constant.Initialize(handler, nodeId.LocalPath, portReader.LocalID);
                }

                IPortModel newPortModel = null;
                if (isInput)
                {
                    newPortModel = this.AddDataInputPort(portReader.LocalID, type, orientation: orientation, initializationCallback: initCallback);
                    // If we were deserialized, the InitCallback doesn't get triggered.
                    if (newPortModel != null)
                        ((BaseShaderGraphConstant)newPortModel.EmbeddedValue).Initialize(graphHandler, nodeReader.ID.LocalPath, portReader.LocalID);
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
