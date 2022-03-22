using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace com.unity.shadergraph.defs
{

    internal class OneMinusNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "OneMinus",
            "Out = 1 - In;",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] {1f, 1f, 1f, 1f}),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
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
