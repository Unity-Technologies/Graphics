using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class SaturationNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Saturation", // Name
            @"
{
    float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
}
",
            new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("Saturation", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Artistic, Adjustment" },
            { "Tooltip", "adjusts the color intensity" },
            { "Parameters.In.Tooltip", "a color to adjust" },
            { "Parameters.Saturation.Tooltip", "less than 1 is less saturated, more than 1 is more saturated" },
            { "Parameters.Out.Tooltip", "color with adjusted saturation" }
        };
    }
}
