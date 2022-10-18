using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EvaluateWaterSimulationCausticsNode : IStandardNode
    {
        public static string Name => "EvaluateWaterSimulationCaustics";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"Caustics = EvaluateSimulationCaustics(RefractedPositionWS, positionWS, DistordedWaterNDC);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("positionWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Position),
                new ParameterDescriptor("RefractedPositionWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("DistordedWaterNDC", TYPE.Vec2, Usage.In),
                new ParameterDescriptor("Caustics", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Evaluate Water Simulation Caustics",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "RefractedPositionWS",
                    displayName: "Refracted Position WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "DistordedWaterNDC",
                    displayName: "Distorded Water NDC",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "Caustics",
                    displayName: "Caustics",
                    tooltip: ""
                )
            }
        );
    }
}
