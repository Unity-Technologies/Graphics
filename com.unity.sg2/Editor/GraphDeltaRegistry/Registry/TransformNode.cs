using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    class TransformNodeUI : INodeUIDescriptorBuilder
    {
        public NodeUIDescriptor CreateDescriptor(NodeHandler node)
        {
            var key = node.GetRegistryKey();

            return new NodeUIDescriptor(
                name: key.Name,
                version: key.Version,
                tooltip: "converts a point or vector from one space to another",
                category: "Math/Vector",
                synonyms: new[] { "world", "object", "tangent", "screen", "view", "convert" },
                parameters: new ParameterUIDescriptor[] {
                    new(TransformNode.kInput, "In", tooltip: "Input value"),
                    new(TransformNode.kOutput, "Out", tooltip: "Output value"),
                }
            );
        }
    }

    class TransformNode : INodeDefinitionBuilder
    {
        // Ports
        public const string kInput = "In";
        public const string kOutput = "Out";

        // Fields
        public const string kSourceSpace = "Source";
        public const string kDestinationSpace = "Destination";
        public const string kType = "Type";

        public RegistryKey GetRegistryKey() => new() {Name = "Transform", Version = 1};
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public void BuildNode(NodeHandler node, Registry registry)
        {
            var inPort = node.AddPort<GraphType>(kInput, isInput: true, registry);
            var outPort = node.AddPort<GraphType>(kOutput, isInput: false, registry);

            GraphTypeHelpers.InitGraphType(inPort.GetTypeField(), GraphType.Length.Three);
            GraphTypeHelpers.InitGraphType(outPort.GetTypeField(), GraphType.Length.Three);
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry, out INodeDefinitionBuilder.Dependencies outputs)
        {
            outputs = new INodeDefinitionBuilder.Dependencies();
            var builder = new ShaderFunction.Builder(container, "Multiply");

            foreach (var port in node.GetPorts())
            {
                var field = port.GetTypeField();
                var shaderType = registry.GetTypeBuilder(field.GetRegistryKey()).GetShaderType(field, container, registry);
                var param = new FunctionParameter.Builder(container, port.LocalID, shaderType, input: port.IsInput, output: !port.IsInput).Build();
                builder.AddParameter(param);
            }

            // TODO: Implement the node!
            builder.AddLine("Out = In;");
            return builder.Build();
        }
    }
}
