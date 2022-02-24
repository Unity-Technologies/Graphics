using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class ArccosineNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Arccosine",
            "Out = acos(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] {1f, 0f, 0f, 0f}),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "acos" },
            { "Tooltip", "returns the arccosine of each component of the input" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the arccosine of each component of the input" },
            { "Category", "Math, Trigonometry" }
        };
    }
}
