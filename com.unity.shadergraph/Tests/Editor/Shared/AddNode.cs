using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.Test
{

    class AddNode : INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() =>
            new() { Name = "Add", Version = 1 };

        public RegistryFlags GetRegistryFlags() =>
            RegistryFlags.Func;

        public void BuildNode(INodeReader userData, INodeWriter nodeWriter, Registry registry)
        {
            NodeHelpers.MathNodeDynamicResolver(userData, nodeWriter, registry);
        }

        public ShaderFunction GetShaderFunction(
            INodeReader data,
            ShaderFoundry.ShaderContainer container,
            Registry registry)
        {
            return NodeHelpers.MathNodeFunctionBuilder("Add", "+", data, container, registry);
        }
    }
}
