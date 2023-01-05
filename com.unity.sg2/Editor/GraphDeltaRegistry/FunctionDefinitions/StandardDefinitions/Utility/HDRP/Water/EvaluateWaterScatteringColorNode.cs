using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EvaluateWaterScatteringColorNode : IStandardNode
    {
        public static string Name => "EvaluateWaterScatteringColor";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"ScatteringColor = EvaluateScatteringColor(LowFrequencyHeight, HorizontalDisplacement, AbsorptionTint, DeepFoam);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("AbsorptionTint", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("LowFrequencyHeight", TYPE.Float, Usage.In),
                new ParameterDescriptor("HorizontalDisplacement", TYPE.Float, Usage.In),
                new ParameterDescriptor("DeepFoam", TYPE.Float, Usage.In),
                new ParameterDescriptor("ScatteringColor", TYPE.Vec3, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Evaluate Water Scattering Color",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "AbsorptionTint",
                    displayName: "Absorption Tint",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LowFrequencyHeight",
                    displayName: "Low Frequency Height",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "HorizontalDisplacement",
                    displayName: "Horizontal Displacement",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "DeepFoam",
                    displayName: "Deep Foam",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ScatteringColor",
                    displayName: "Scattering Color",
                    tooltip: ""
                )
            }
        );
    }
}
