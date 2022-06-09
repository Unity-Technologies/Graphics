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
    }
}
