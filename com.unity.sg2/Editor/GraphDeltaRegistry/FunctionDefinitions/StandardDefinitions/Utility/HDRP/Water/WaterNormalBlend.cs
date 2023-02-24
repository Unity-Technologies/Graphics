using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class WaterNormalBlendNode : IStandardNode
    {
        public static string Name => "WaterNormalBlend";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            @"   NormalWS = BlendWaterNormal(NormalTS, NormalWSin);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("NormalTS", TYPE.Vec3, Usage.In, new float[] { 1.0f, 1.0f, 1.0f}),
                new ParameterDescriptor("NormalWSin", TYPE.Vec3, Usage.In, new float[] { 1.0f, 1.0f, 1.0f}),
                new ParameterDescriptor("NormalWS", TYPE.Vec3, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Water Normal Blend ",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[1] { "Blend Water Normal" },
            description: "pkg://Documentation~/previews/WaterNormalBlend.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "NormalTS",
                    displayName: "Normal TS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "NormalWSin",
                    displayName: "Normal WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "NormalWS",
                    displayName: "Normal WS",
                    tooltip: ""
                )
            }
        );
    }
}

