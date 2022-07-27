using System;
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

    class TestOutputNode : INodeDefinitionBuilder 
    {
        public RegistryKey GetRegistryKey() =>
        new () { Name = "TestOutput", Version = 1 };

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public void BuildNode(NodeHandler node, Registry registry)
        {

        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry, out INodeDefinitionBuilder.Dependencies outputs)
        {
            var outField = node.GetPort("Out").GetTypeField();
            var typeBuilder = registry.GetTypeBuilder(GraphType.kRegistryKey);

            var shaderType = typeBuilder.GetShaderType(outField, container, registry);

            var funcName = $"Test_{shaderType.Name}";
            var builder = new ShaderFunction.Builder(container, funcName);

            builder.AddOutput(shaderType, "Out");
            builder.AddLine($"Out = {typeBuilder.GetInitializerList(outField, registry)};");
            outputs = new INodeDefinitionBuilder.Dependencies();
            return builder.Build();
        }
    }

    class TestInputNode : INodeDefinitionBuilder
    {
        public RegistryKey GetRegistryKey() =>
        new () { Name = "TestInput", Version = 1 };

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public void BuildNode(NodeHandler node, Registry registry)
        {

        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry, out INodeDefinitionBuilder.Dependencies outputs)
        {
            var outField = node.GetPort("Out").GetTypeField();
            var typeBuilder = registry.GetTypeBuilder(GraphType.kRegistryKey);

            var shaderType = typeBuilder.GetShaderType(outField, container, registry);

            var funcName = $"Test_{shaderType.Name}";
            var builder = new ShaderFunction.Builder(container, funcName);

            builder.AddOutput(shaderType, "Out");
            builder.AddInput(shaderType, "In");
            builder.AddLine($"Out = In;");
            outputs = new INodeDefinitionBuilder.Dependencies();
            return builder.Build();
        }
    }


}
