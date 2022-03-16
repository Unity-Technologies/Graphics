using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class OneMinusNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "OneMinus",
            "Out = 1 - In;",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In, new float[] {1f, 1f, 1f, 1f}),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "complement, invert, opposite" },
            { "Tooltip", "subtracts the input from one" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the input subtracted from one" },
            { "Category", "Math, Range" },
            { "DisplayName", "One Minus" }
        };
    }
}
