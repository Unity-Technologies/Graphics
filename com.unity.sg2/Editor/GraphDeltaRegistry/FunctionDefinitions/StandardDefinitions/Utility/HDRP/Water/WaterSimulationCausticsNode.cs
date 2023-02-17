using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class WaterSimulationCausticsNode : IStandardNode
    {
        public static string Name => "WaterSimulationCaustics";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"Caustics = EvaluateSimulationCaustics(RefractedPositionWS, positionWS, DistortedWaterNDC);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("positionWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Position),
                new ParameterDescriptor("RefractedPositionWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("DistortedWaterNDC", TYPE.Vec2, Usage.In),
                new ParameterDescriptor("Caustics", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Water Simulation Caustics",
            tooltip: "",
            category: "Utility/HDRP/Water",
            description: "pkg://Documentation~/previews/WaterSimulationCaustics.md",
            synonyms: Array.Empty<string>(),
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "RefractedPositionWS",
                    displayName: "Refracted Position WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "DistortedWaterNDC",
                    displayName: "Distorted Water NDC",
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
