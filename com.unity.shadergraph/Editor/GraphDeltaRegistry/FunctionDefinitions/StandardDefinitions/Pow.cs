using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class Pow : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Pow", // Name
            "Out = pow(In, Exp);",
            new ParameterDescriptor("In", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out),
            new ParameterDescriptor("Exp", TYPE.Any, Usage.In)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "Power, P" },
            { "Tooltip", "Power function" },
            { "Parameters.In.Tooltip", "Input" },
            { "Parameters.Exp.Tooltip", "Exponent" },
            { "Parameters.Out.Tooltip", "In ^ Exp" }
        };
    }
}
