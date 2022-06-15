using UnityEditor.ShaderFoundry;


namespace UnityEditor.ShaderGraph.GraphDelta
{
    class TestAddNode : INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() =>
            new() { Name = "TestAdd", Version = 1 };

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public void BuildNode(NodeHandler node, Registry registry)
        {
            NodeHelpers.MathNodeDynamicResolver(node, registry);
        }

        public ShaderFunction GetShaderFunction(
            NodeHandler node,
            ShaderContainer container,
            Registry registry,
            out INodeDefinitionBuilder.Dependencies deps)
        {
            deps = new();
            return NodeHelpers.MathNodeFunctionBuilder(node.ID.LocalPath + "TestAdd", "+", node, container, registry);
        }
    }
}
