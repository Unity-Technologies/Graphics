using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EvaluateWaterSimulationDisplacementNode : IStandardNode
    {
        public static string Name => "EvaluateWaterSimulationDisplacement";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"WaterDisplacementData displacementData;
ZERO_INITIALIZE(WaterDisplacementData, displacementData);
EvaluateWaterDisplacement(PositionWS, BandsMultiplier, displacementData);
Displacement = displacementData.displacement;
LowFrequencyHeight = displacementData.lowFrequencyHeight;
SSSMask = displacementData.sssMask;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("PositionWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("BandsMultiplier", TYPE.Vec4, Usage.In, new float[] { 1f, 1f, 1f, 1f}),
                new ParameterDescriptor("Displacement", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("LowFrequencyHeight", TYPE.Float, Usage.Out),
                new ParameterDescriptor("SSSMask", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Evaluate Water Simulation Displacement",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[0],
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
