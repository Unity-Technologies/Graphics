using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace com.unity.shadergraph.defs
{

    internal class SignNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Sign",
            "Out = sign(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "returns 1 if the input is positive and -1 if the input is negative" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "1 if the input is positive and -1 if the input is negative" },
            { "Category", "Math, Round" }
        };
    }
}
