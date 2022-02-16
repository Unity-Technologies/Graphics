using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class LerpNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,      // Version
            "Lerp", // Name
            "Out = lerp(A, B, T);",
            new ParameterDescriptor("A", TYPE.Any, Usage.In),
            new ParameterDescriptor("B", TYPE.Any, Usage.In),
            new ParameterDescriptor("T", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Interpolation" },
            { "Name.Synonyms", "Mix, Blend, Interpolate, Extrapolate, Linear Interpolate" },
            { "Tooltip", "blends between A and B given the value of T" },
            { "Parameters.A.Tooltip", "Out will be this value when T is zero" },
            { "Parameters.B.Tooltip", "Out will be this value when T is one" },
            { "Parameters.T.Tooltip", "the blend value.  Will return A when this is 0 and B when this is 1" },
            { "Parameters.Out.Tooltip", "A * (1-T) + B * T" }
        };
    }
}
