using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.Defs;
using UnityEngine;

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

        // Hidden ports for referables used by transform calculations
        const string kWorldTangent = "WorldTangent";
        const string kWorldBiTangent = "WorldBiTangent";
        const string kWorldNormal = "WorldNormal";
        const string kWorldPosition = "WorldPosition";

        // Fields
        const string kSourceSpace = "Source";
        const string kDestinationSpace = "Destination";
        const string kType = "ConversionType";

        public RegistryKey GetRegistryKey() => new() {Name = "Transform", Version = 1};
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        static void AddLocalReferable(NodeHandler node, string portName, string contextEntryName, Registry registry)
        {
            var port = node.AddPort<GraphType>(portName, isInput: true, registry);
            var typeField = port.GetTypeField();
            typeField.AddSubField("IsLocal", true);
            GraphTypeHelpers.InitGraphType(typeField, GraphType.Length.Three);
            node.Owner.AddDefaultConnection(contextEntryName, port.ID, registry);
        }

        public void BuildNode(NodeHandler node, Registry registry)
        {
            var inPort = node.AddPort<GraphType>(kInput, isInput: true, registry);
            var outPort = node.AddPort<GraphType>(kOutput, isInput: false, registry);

            GraphTypeHelpers.InitGraphType(inPort.GetTypeField(), GraphType.Length.Three);
            GraphTypeHelpers.InitGraphType(outPort.GetTypeField(), GraphType.Length.Three);
            GraphTypeHelpers.ResolveDynamicPorts(node);

            AddLocalReferable(node, kWorldTangent, "WorldSpaceTangent", registry);
            AddLocalReferable(node, kWorldBiTangent, "WorldSpaceBiTangent", registry);
            AddLocalReferable(node, kWorldNormal, "WorldSpaceNormal", registry);
            AddLocalReferable(node, kWorldPosition, "WorldSpacePosition", registry);

            node.AddField(kSourceSpace, CoordinateSpace.Object, reconcretizeOnDataChange: true);
            node.AddField(kDestinationSpace, CoordinateSpace.World, reconcretizeOnDataChange: true);
            node.AddField(kType, ConversionType.Position, reconcretizeOnDataChange: true);
        }

        static SpaceTransform GetTransform(NodeHandler node)
        {
            var source = node.GetField<CoordinateSpace>(kSourceSpace).GetData();
            var destination = node.GetField<CoordinateSpace>(kDestinationSpace).GetData();
            var type = node.GetField<ConversionType>(kType).GetData();

            return new SpaceTransform(source, destination, type);
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry, out INodeDefinitionBuilder.Dependencies outputs)
        {
            outputs = new INodeDefinitionBuilder.Dependencies();
            var builder = new ShaderFunction.Builder(container, "Transform");

            foreach (var port in node.GetPorts())
            {
                var field = port.GetTypeField();
                var shaderType = registry.GetTypeBuilder(field.GetRegistryKey()).GetShaderType(field, container, registry);
                var param = new FunctionParameter.Builder(container, port.LocalID, shaderType, input: port.IsInput, output: !port.IsInput).Build();
                builder.AddParameter(param);
            }

            var generationInfo = new SpaceTransformUtils.GenerationArgs
            {
                Input = kInput,
                OutputVariable = kOutput,
                WorldTangent = kWorldTangent,
                WorldBiTangent = kWorldBiTangent,
                WorldNormal = kWorldNormal,
                WorldPosition = kWorldPosition,
            };

            SpaceTransformUtils.GenerateTransform(GetTransform(node), generationInfo, builder);
            return builder.Build();
        }
    }
}
