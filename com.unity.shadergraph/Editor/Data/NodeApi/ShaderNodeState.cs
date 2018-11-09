using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    class ShaderNodeState
    {
        public int id;
        public AbstractMaterialGraph owner;
        public IShaderNode shaderNode;
        public NodeTypeDescriptor type;
        public List<InputPortDescriptor> inputPorts = new List<InputPortDescriptor>();
        public List<OutputPortDescriptor> outputPorts = new List<OutputPortDescriptor>();
        public List<HlslSource> hlslSources = new List<HlslSource>();
        public List<ProxyShaderNode> createdNodes = new List<ProxyShaderNode>();
        public List<ProxyShaderNode> deserializedNodes = new List<ProxyShaderNode>();

        public bool isDirty => createdNodes.Any() || deserializedNodes.Any();

        public void ClearChanges()
        {
            createdNodes.Clear();
            deserializedNodes.Clear();
        }
    }
}
