using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class IntegerNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Integer", // Name
            "Out = IntConst;",
            new ParameterDescriptor("IntConst", TYPE.Int, Usage.Static),
            new ParameterDescriptor("Out", TYPE.Int, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Input, Basic" },
            { "Name.Synonyms", "whole number" },
            { "Tooltip", "a constant, single-channel, whole number value" },
            { "Parameters.Out.Tooltip", "a single channel value without a decimal" }
        };
    }
}
