using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class GraphDataNodeModel : NodeModel
    {
        string m_Name;

        public string graphDataName
        {
            get => m_Name ??= Guid.ToString();
            set => m_Name = value;
        }

        IGraphHandler graphHandler => ((ShaderGraphModel) GraphModel).GraphHandler;
        public bool existsInGraphData => TryGetNodeReader(out _);

        public bool TryGetNodeWriter(out INodeWriter writer)
        {
            writer = graphHandler.GetNodeWriter(graphDataName);
            return writer != null;
        }

        public bool TryGetNodeReader(out INodeReader reader)
        {
            reader = graphHandler.GetNode(graphDataName);
            return reader != null;
        }

        public RegistryKey registryKey => TryGetNodeReader(out var reader) ? reader.GetRegistryKey() : default;

        public bool HasPreview { get; private set; }

        // By default every node's preview is visible
        // TODO: Handle preview state serialization
        [SerializeField]
        bool m_IsPreviewExpanded = true;

        public bool IsPreviewVisible
        {
            get => m_IsPreviewExpanded;
            set
            {
                m_IsPreviewExpanded = value;
            }
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
                var isInput = portReader.GetFlags().isInput;
                var orientation = portReader.GetFlags().isHorizontal
                    ? PortOrientation.Horizontal
                    : PortOrientation.Vertical;

                var type = ShaderGraphTypes.GetTypeHandleFromKey(portReader.GetRegistryKey());

                if (isInput)
                    this.AddDataInputPort(portReader.GetName(), type, orientation: orientation);
                else
                    this.AddDataOutputPort(portReader.GetName(), type, orientation: orientation);

                // Mark node as containing a preview if any of the ports on it are flagged as a preview port
                if(portReader.GetFlags().IsPreview)
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
