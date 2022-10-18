using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EvaluateWaterRefractionDataNode : IStandardNode
    {
        public static string Name => "EvaluateWaterRefractionData";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"ComputeWaterRefractionParams(positionWS, NormalWS, LowFrequencyNormalWS, screenPos.xy, viewWS, true, _MaxRefractionDistance, _TransparencyColor.xyz, _OutScatteringCoefficient, RefractedPositionWS, DistortedWaterNDC, refractedDistance, AbsorptionTint);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("refractedDistance", TYPE.Float, Usage.Local),
                new ParameterDescriptor("positionWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Position),
                new ParameterDescriptor("viewWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_ViewDirection),
                new ParameterDescriptor("screenPos", TYPE.Vec2, Usage.Local, REF.ScreenPosition),
                new ParameterDescriptor("NormalWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("LowFrequencyNormalWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("RefractedPositionWS", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("DistortedWaterNDC", TYPE.Vec2, Usage.Out),
                new ParameterDescriptor("AbsorptionTint", TYPE.Vec3, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Evaluate Water Refraction Data",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "NormalWS",
                    displayName: "Normal WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LowFrequencyNormalWS",
                    displayName: "Low Frequency Normal WS",
                    tooltip: ""
                ),
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
                    name: "AbsorptionTint",
                    displayName: "Absorption Tint",
                    tooltip: ""
                )
            }
        );
    }
}
