using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class UnpackWaterDataNode : IStandardNode
    {
        public static string Name => "UnpackWaterData";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"LowFrequencyHeight = saturate(UV.x);
HorizontalDisplacement = UV.y;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("UV", TYPE.Vec4, Usage.Local, REF.UV1),
                new ParameterDescriptor("LowFrequencyHeight", TYPE.Float, Usage.Out),
                new ParameterDescriptor("HorizontalDisplacement", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Unpack Water Data",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
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
                    name: "SSSMask",
                    displayName: "SSS Mask",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "CustomFoam",
                    displayName: "Custom Foam",
                    tooltip: ""
                )
            }
        );
    }
}
