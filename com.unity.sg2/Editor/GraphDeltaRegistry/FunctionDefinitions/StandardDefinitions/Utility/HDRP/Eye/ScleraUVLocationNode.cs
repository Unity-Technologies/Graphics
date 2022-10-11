using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ScleraUVLocationNode : IStandardNode
    {
        public static string Name => "ScleraUVLocation";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    ScleraUV =  PositionOS.xy + float2(0.5, 0.5);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("PositionOS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("ScleraUV", TYPE.Vec2, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Sclera UV Location",
            tooltip: "Converts the object position of the sclera to UVs.",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "PositionOS",
                    displayName: "Position OS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ScleraUV",
                    displayName: "Sclera UV",
                    tooltip: ""
                )
            }
        );
    }
}
