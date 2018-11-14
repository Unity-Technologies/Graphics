using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    class NodeTypeState
    {
        public int id;
        public AbstractMaterialGraph owner;
        public IShaderNodeType shaderNodeType;
        public NodeTypeDescriptor type;
        public List<InputPortDescriptor> inputPorts = new List<InputPortDescriptor>();
        public List<OutputPortDescriptor> outputPorts = new List<OutputPortDescriptor>();
        public List<HlslSource> hlslSources = new List<HlslSource>();

        #region Change lists for consumption by IShaderNode implementation

        public List<ProxyShaderNode> createdNodes = new List<ProxyShaderNode>();
        public List<ProxyShaderNode> deserializedNodes = new List<ProxyShaderNode>();
        public List<NodeRef> changedNodes = new List<NodeRef>();

        #endregion

        public bool isDirty => createdNodes.Any() || deserializedNodes.Any();

        public void ClearChanges()
        {
            createdNodes.Clear();
            deserializedNodes.Clear();
        }
    }
}
