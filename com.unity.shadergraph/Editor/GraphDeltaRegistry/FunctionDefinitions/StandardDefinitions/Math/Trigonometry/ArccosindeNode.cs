using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class ArccosineNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Arccosine",
            "Out = acos(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In, new float[] {1f, 1f, 1f, 1f}),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
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
