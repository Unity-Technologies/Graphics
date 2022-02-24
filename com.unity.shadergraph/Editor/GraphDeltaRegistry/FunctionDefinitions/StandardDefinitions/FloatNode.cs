using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class FloatNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Float",
            "Out = In;",
            new ParameterDescriptor("In", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "a single channel value with a decimal" },
            { "Name.Synonyms", "Vector 1, 1, v1, vec1, scalar" },
            { "Category", "Input, Basic" }
        };
    }
}
