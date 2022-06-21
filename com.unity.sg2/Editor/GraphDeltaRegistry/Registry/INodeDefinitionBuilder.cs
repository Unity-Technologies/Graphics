using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal interface INodeDefinitionBuilder : IRegistryEntry
    {
        public struct Dependencies
        {
            public List<ShaderFunction> localFunctions;
            public List<ShaderFoundry.IncludeDescriptor> includes;
        }

        void BuildNode(NodeHandler node, Registry registry);

        ShaderFunction GetShaderFunction(
            NodeHandler node,
            ShaderContainer container,
            Registry registry,
            out Dependencies outputs);

        static PortHandler CopyPort(PortHandler srcPort, NodeHandler dstNode, Registry registry)
        {
            if (!srcPort.IsHorizontal)
                return null;

            var dstPort = dstNode.AddPort(srcPort.LocalID, srcPort.IsInput, srcPort.IsHorizontal);
            var dstField = dstPort.AddTypeField();
            ITypeDefinitionBuilder.CopyTypeField(srcPort.GetTypeField(), dstField, registry);
            return dstPort;
        }
    }
}
