using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    /// <summary>
    /// GraphDataNodeModel is a model for a node backed by graph data. It can be used for a node on the graph (with
    /// an assigned graph data name) or a searcher preview (with only an assigned registry key).
    /// </summary>
    public class GraphDataNodeModel : NodeModel
    {
        [SerializeField] string m_GraphDataName;

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
        public bool existsInGraphData => m_GraphDataName != null && TryGetNodeReader(out _);

        IGraphHandler graphHandler => ((ShaderGraphModel)GraphModel).GraphHandler;
        Registry.Registry registry => ((ShaderGraphStencil)GraphModel.Stencil).GetRegistry();

        public bool TryGetNodeWriter(out INodeWriter writer)
        {
            if (graphDataName == null)
            {
                writer = null;
                return false;
            }

            writer = graphHandler.GetNodeWriter(graphDataName);
            return writer != null;
        }

        public bool TryGetNodeReader(out INodeReader reader)
        {
            if (graphDataName == null)
            {
                reader = registry.GetDefaultTopology(m_PreviewRegistryKey);
                return true;
            }

            reader = graphHandler.GetNodeReader(graphDataName);
            return reader != null;
        }

        public bool HasPreview { get; private set; }

        // By default every node's preview is visible
        // TODO: Handle preview state serialization
        [SerializeField] bool m_IsPreviewExpanded = true;

        public bool IsPreviewVisible
        {
            get => m_IsPreviewExpanded;
            set { m_IsPreviewExpanded = value; }
        }

        /// <summary>
        /// Sets the registry key used when previewing this node. Has no effect if graphDataName has been set.
        /// </summary>
        /// <param name="key">Registry key used to preview this node.</param>
        public void SetPreviewRegistryKey(RegistryKey key)
        {
            m_PreviewRegistryKey = key;
        }

        protected override void OnDefineNode()
        {
            if (!TryGetNodeReader(out var reader))
            {
                Debug.LogErrorFormat("Node \"{0}\" is missing from graph data", graphDataName);
                return;
            }

            bool nodeHasPreview = false;

            // TODO: Convert this to a NodePortsPart maybe?
            foreach (var portReader in reader.GetPorts())
            {
                var isInput = portReader.IsInput();
                var orientation = portReader.IsHorizontal()
                    ? PortOrientation.Horizontal
                    : PortOrientation.Vertical;

                var type = ShaderGraphTypes.GetTypeHandleFromKey(portReader.GetRegistryKey());

                if (isInput)
                    this.AddDataInputPort(portReader.GetName(), type, orientation: orientation);
                else
                    this.AddDataOutputPort(portReader.GetName(), type, orientation: orientation);

                // Mark node as containing a preview if any of the ports on it are flagged as a preview port
                if (portReader.TryGetField("_isPreview", out var previewField) && previewField.TryGetValue(out bool previewData) && previewData)
                    nodeHasPreview = true;
            }

            HasPreview = nodeHasPreview;
        }

        public override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName,
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
                AssetModel = AssetModel
            };
        }
    }
}
