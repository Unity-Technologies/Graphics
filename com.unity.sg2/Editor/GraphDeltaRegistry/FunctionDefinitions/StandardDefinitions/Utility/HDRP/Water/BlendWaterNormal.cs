using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BlendWaterNormalNode : IStandardNode
    {
        public static string Name => "BlendWaterNormal";
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
            displayName: "Blend Water Normal",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "NormalTS",
                    displayName: "NormalTS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "NormalWSin",
                    displayName: "NormalWS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "NormalWS",
                    displayName: "NormalWS",
                    tooltip: ""
                )
            }
        );
    }
}

