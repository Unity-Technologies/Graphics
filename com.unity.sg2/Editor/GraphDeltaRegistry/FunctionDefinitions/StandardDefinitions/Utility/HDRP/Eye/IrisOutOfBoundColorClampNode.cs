using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class IrisOutOfBoundColorClampNode : IStandardNode
    {
        public static string Name => "IrisOutOfBoundColorClamp";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Color = (IrisUV.x < 0.0 || IrisUV.y < 0.0 || IrisUV.x > 1.0 || IrisUV.y > 1.0) ? ClampColor : IrisColor;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("IrisUV", TYPE.Vec2, Usage.In),
                new ParameterDescriptor("IrisColor", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("ClampColor", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("Color", TYPE.Vec3, Usage.Out),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Iris Out Of Bound Color Clamp",
            tooltip: "clamps the color of the Iris to a given color",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "IrisUV",
                    displayName: "Iris UV",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "IrisColor",
                    displayName: "Iris Color",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ClampColor",
                    displayName: "Clamp Color",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "Color",
                    tooltip: ""
                )
            }
        );
    }
}
