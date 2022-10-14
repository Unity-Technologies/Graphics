using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class PackWaterVertexDataNode : IStandardNode
    {
        public static string Name => "PackWaterVertexData";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"PackedWaterData packedWaterData;
ZERO_INITIALIZE(PackedWaterData, packedWaterData);
PackWaterVertexData(PositionWS, Displacement, LowFrequencyHeight, SSSMask, packedWaterData);
PositionOS = packedWaterData.positionOS;
NormalOS = packedWaterData.normalOS;
uv0 = packedWaterData.uv0;
uv1 = packedWaterData.uv1;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("PositionWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("Displacement", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("LowFrequencyHeight", TYPE.Float, Usage.In),
                new ParameterDescriptor("SSSMask", TYPE.Float, Usage.In),
                new ParameterDescriptor("PositionOS", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("NormalOS", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("uv0", TYPE.Vec4, Usage.Out),
                new ParameterDescriptor("uv1", TYPE.Vec4, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Pack Water Vertex Data",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "PositionWS",
                    displayName: "PositionWS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "Displacement",
                    displayName: "Displacement",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LowFrequencyHeight",
                    displayName: "Low Frequency Height",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "SSS Mask",
                    displayName: "SSSMask",
                    tooltip: ""
                ),                new ParameterUIDescriptor(
                    name: "PositionOS",
                    displayName: "PositionOS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "NormalOS",
                    displayName: "NormalOS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "uv0",
                    displayName: "UV0",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "uv1",
                    displayName: "UV1",
                    tooltip: ""
                )
            }
        );
    }
}
