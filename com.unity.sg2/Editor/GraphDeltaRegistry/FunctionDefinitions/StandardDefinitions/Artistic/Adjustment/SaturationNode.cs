using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SaturationNode : IStandardNode
    {
        public static string Name => "Saturation";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            @"
{
    luma = dot(In, sRGBD65);
    Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
}
",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("Saturation", TYPE.Float, Usage.In, new float[] { 1f }),
                new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("luma", TYPE.Float, Usage.Local),
                new ParameterDescriptor("sRGBD65", TYPE.Vec3, Usage.Local, new float[] { 0.2126729f, 0.7151522f, 0.0721750f })
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Saturation",
            tooltip: "Adjusts the color intensity.",
            category: "Artistic/Adjustment",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/Saturation.md",
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a color to adjust"
                ),
                new ParameterUIDescriptor(
                    name: "Saturation",
                    tooltip: "less than 1 is less saturated, more than 1 is more saturated"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "color with adjusted saturation"
                )
            }
        );
    }
}
