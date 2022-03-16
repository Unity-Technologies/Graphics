using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class CeilingNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Ceiling",
            "Out = ceil(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "up" },
            { "Tooltip", "rounds the input up to the nearest whole number" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the input rounded up to the nearest whole number" },
            { "Category", "Math, Round" }
        };
    }
}
