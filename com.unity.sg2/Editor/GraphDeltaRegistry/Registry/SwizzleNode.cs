using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.Defs;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    class SwizzleNodeUI : INodeUIDescriptorBuilder
    {
        public NodeUIDescriptor CreateDescriptor(NodeHandler node)
        {
            var key = node.GetRegistryKey();

            return new NodeUIDescriptor(
                name: key.Name,
                version: key.Version,
                tooltip: "swaps, duplicates, or reorders the channels of a vector",
                category: "Channel",
                synonyms: new string[] { "swap", "reorder", "component mask" },
                description: "pkg://Documentation~/previews/Swizzle.md",
                parameters: new ParameterUIDescriptor[]
                {
                    new(name: SwizzleNode.kInput, tooltip: "input value"),
                    new(name: SwizzleNode.kOutput, tooltip: "vector with components rearranged"),
                }
            );
        }
    }

    class SwizzleNode : INodeDefinitionBuilder
    {
        public const string kMask = "Mask";
        public const string kDefaultMask = "xyzw";

        const string kComponentsVec = "xyzw";
        const string kComponentsColor = "rgba";
        public const string kAllowedMaskComponents = kComponentsVec + kComponentsColor;

        public const string kInput = "In";
        public const string kOutput = "Out";

        public RegistryKey GetRegistryKey() => new() {Name = "Swizzle", Version = 1};
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Func;

        public void BuildNode(NodeHandler node, Registry registry)
        {
            node.AddField(kMask, kDefaultMask, reconcretizeOnDataChange: true);
            var mask = node.GetField<string>(kMask).GetData();

            var inputPort = node.AddPort<GraphType>(kInput, true, registry);
            GraphTypeHelpers.InitGraphType(
                field: inputPort.GetTypeField(),
                lengthDynamic: true,
                primitiveDynamic: false,
                precisionDynamic: true
            );

            var outputPort = node.AddPort<GraphType>(kOutput, false, registry);
            GraphTypeHelpers.InitGraphType(
                field: outputPort.GetTypeField(),
                length: (GraphType.Length)Mathf.Clamp(mask.Length, 1, 4),
                primitiveDynamic: false,
                precisionDynamic: true
            );

            GraphTypeHelpers.ResolveDynamicPorts(node);
        }

        static bool MaskIsValid(string maskInput, int inputChannels)
        {
            if (maskInput.Length <= 0 || maskInput.Length > 4)
            {
                return false;
            }

            maskInput = maskInput.ToLowerInvariant();
            for (var i = 0; i < maskInput.Length; i++)
            {
                var c = maskInput[i];
                var componentIndex = kComponentsVec.IndexOf(c);

                if (componentIndex == -1)
                {
                    componentIndex = kComponentsColor.IndexOf(c);
                }

                if (componentIndex == -1 || componentIndex >= inputChannels)
                {
                    return false;
                }
            }

            return true;
        }

        static string NormalizeMask(string maskInput)
        {
            // Assumes MaskIsValid(maskInput) is true.
            maskInput = maskInput.ToLowerInvariant();

            // Convert "rgba" input to "xyzw" to avoid mismatching
            var resultMask = new char[maskInput.Length];
            for (var i = 0; i < maskInput.Length; i++)
            {
                var colorIndex = kComponentsColor.IndexOf(maskInput[i]);
                resultMask[i] = colorIndex == -1 ? maskInput[i] : kComponentsVec[colorIndex];
            }

            return new string(resultMask);
        }

        public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry, out INodeDefinitionBuilder.Dependencies outputs)
        {
            outputs = new INodeDefinitionBuilder.Dependencies();
            var builder = new ShaderFunction.Builder(container, "Swizzle");

            foreach (var port in node.GetPorts())
            {
                var field = port.GetTypeField();
                var shaderType = registry.GetTypeBuilder(field.GetRegistryKey()).GetShaderType(field, container, registry);
                var param = new FunctionParameter.Builder(container, port.LocalID, shaderType, input: port.IsInput, output: !port.IsInput).Build();
                builder.AddParameter(param);
            }

            var mask = node.GetField<string>(kMask).GetData();
            var length = GraphTypeHelpers.GetLength(node.GetPort(kInput).GetTypeField());

            if (!MaskIsValid(mask, (int)length))
            {
                // TODO: When error handling is available, report that the mask is invalid.
                // https://jira.unity3d.com/browse/GSG-734
                builder.AddLine($"{kOutput} = 0;");
            }
            else
            {
                builder.AddLine($"{kOutput} = {kInput}.{NormalizeMask(mask)};");
            }

            return builder.Build();
        }
    }
}
