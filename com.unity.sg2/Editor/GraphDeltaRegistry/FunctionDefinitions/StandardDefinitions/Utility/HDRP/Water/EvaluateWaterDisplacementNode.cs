using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EvaluateWaterDisplacementNode : IStandardNode
    {
        public static string Name => "EvaluateWaterDisplacement";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"WaterDisplacementData displacementData;
ZERO_INITIALIZE(WaterDisplacementData, displacementData);
EvaluateWaterDisplacement(PositionWS, displacementData);
Displacement = displacementData.displacement;
LowFrequencyHeight = displacementData.lowFrequencyHeight;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("PositionWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("Displacement", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("LowFrequencyHeight", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Evaluate Water Displacement",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[0],
            description: "pkg://Documentation~/previews/EvaluateWaterDisplacement.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "PositionWS",
                    displayName: "Position WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "BandsMultiplier",
                    displayName: "Bands Multiplier",
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
                    name: "SSSMask",
                    displayName: "SSS Mask",
                    tooltip: ""
                )
            }
        );
    }
}
