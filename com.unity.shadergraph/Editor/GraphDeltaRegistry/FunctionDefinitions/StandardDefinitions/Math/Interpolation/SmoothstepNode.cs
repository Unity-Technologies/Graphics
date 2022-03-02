using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class SmoothstepNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Smoothstep",
            "Out = smoothstep(Edge1, Edge2, In);",
            new ParameterDescriptor("Edge1", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Edge2", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Interpolation" },
            { "Name.Synonyms", "curve" },
            { "Tooltip", "returns the result of a smooth Hermite interpolation between 0 and 1" },
            { "Parameters.Edge1.Tooltip", "	minimum step value" },
            { "Parameters.Edge2.Tooltip", "maximum step value" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the result of a smooth Hermite interpolation between 0 and 1" }
        };
    }
}
