using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class PowerNode : IStandardNode
    {
        public FunctionDescriptor FunctionDescriptor => new(
            1,          // Version
            "Power",    // Name
            "Out = pow(Base, Exp);",
            new ParameterDescriptor("Base", TYPE.Any, Usage.In),
            new ParameterDescriptor("Exp", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "Power, Exponentiation" },
            { "Tooltip", "Power function" },
            { "Parameters.Base.Tooltip", "Base" },
            { "Parameters.Exp.Tooltip", "Exponent" },
            { "Parameters.Out.Tooltip", "Base ^ Exp" }
        };
    }
}
