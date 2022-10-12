using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// PreviewNodeModel is backed by a registry key, but not graph data. It's only used for previews, and shouldn't
    /// exist on the graph.
    /// </summary>
    class SearcherPreviewNodeModel : NodeModel
    {
        [SerializeField]
        RegistryKey m_RegistryKey;

        public RegistryKey registryKey
        {
            get => m_RegistryKey;
            set => m_RegistryKey = value;
        }

        protected override void OnDefineNode()
        {
            var stencil = (ShaderGraphStencil) GraphModel.Stencil;
            var registry = stencil.GetRegistry();
            var reader = registry.GetDefaultTopology(registryKey);

            if (reader == null) return;
            foreach (var portReader in reader.GetPorts())
            {
                AddPortFromReader(portReader);
            }
        }

        void AddPortFromReader(PortHandler portReader)
        {
            var isInput = portReader.IsInput;
            var orientation = portReader.IsHorizontal
                ? PortOrientation.Horizontal
                : PortOrientation.Vertical;

            var type = ShaderGraphTypes.GetTypeHandleFromKey(portReader.GetTypeField().GetRegistryKey());

            if (isInput)
                this.AddDataInputPort(portReader.LocalID, type, orientation: orientation);
            else
                this.AddDataOutputPort(portReader.LocalID, type, orientation: orientation);
        }
    }
}
