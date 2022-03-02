using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class CombineNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Combine",
@"
{
    RGBA.r = R;
    RGBA.g = G;
    RGBA.b = B;
    RGBA.a = A;
    RGB.r = R;
    RGB.g = G;
    RGB.b = B;
    RG.r = R;
    RG.g = G;
}
",
            new ParameterDescriptor("R", TYPE.Float, Usage.In),
            new ParameterDescriptor("G", TYPE.Float, Usage.In),
            new ParameterDescriptor("B", TYPE.Float, Usage.In),
            new ParameterDescriptor("A", TYPE.Float, Usage.In),
            new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
            new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),
            new ParameterDescriptor("RG", TYPE.Vec2, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Channel" },
            { "Name.Synonyms", "append" },
            { "Tooltip", "merges two or more float values into a vector" },
            { "Parameters.R.Tooltip","red channel of output" },
            { "Parameters.G.Tooltip", "green channel of output" },
            { "Parameters.B.Tooltip", "blue channel of output" },
            { "Parameters.A.Tooltip", "alpha channel of output" },
            { "Parameters.RGBA.Tooltip", "A vector4 formed by the input values" },
            { "Parameters.RGB.Tooltip", "A vector3 formed by the input values" },
            { "Parameters.RG.Tooltip", "A vector2 formed by the input values" },
        };
    }
}
