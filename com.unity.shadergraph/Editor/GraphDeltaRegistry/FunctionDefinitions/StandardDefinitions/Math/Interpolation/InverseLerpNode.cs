using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class InverseLerpNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "InverseLerp",
            "Out = (T - A)/(B - A);",
            new ParameterDescriptor("A", TYPE.Vector, Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("T", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Interpolation" },
            { "DisplayName", "Inverse Lerp" },
            { "Tooltip", "returns the linear parameter that produces the interpolant specified by T within the range of A to B" },
            { "Parameters.A.Tooltip", "out will be this value when T is zero" },
            { "Parameters.B.Tooltip", "out will be this value when T is one" },
            { "Parameters.T.Tooltip", "the blend value" },
            { "Parameters.Out.Tooltip", "the linear parameter that produces the interpolant specified by T within the range of A to B" }
        };
    }
}
