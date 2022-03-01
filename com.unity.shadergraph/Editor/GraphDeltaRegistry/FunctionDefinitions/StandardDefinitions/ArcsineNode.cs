using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class ArcsineNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Arcsine",
            "Out = asin(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "asine" },
            { "Tooltip", "returns the arcsine of each component of the input" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the arcsine of each component of the input" },
            { "Category", "Math, Trigonometry" }
        };
    }
}
