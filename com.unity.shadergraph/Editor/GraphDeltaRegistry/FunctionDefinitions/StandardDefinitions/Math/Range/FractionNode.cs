using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class FractionNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Fraction",
            "Out = frac(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "remainder" },
            { "Tooltip", "returns the decimal portion of the input without the integer portion" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the decimal portion of the input" },
            { "Category", "Math, Range" }
        };
    }
}
