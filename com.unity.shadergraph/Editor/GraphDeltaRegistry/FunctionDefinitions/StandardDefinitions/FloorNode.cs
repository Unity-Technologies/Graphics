using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class FloorNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "Floors",
            "Out = floor(In);",
            new ParameterDescriptor("In", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "down" },
            { "Tooltip", "subtracts the input from one" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the input subtracted from one" },
            { "Category", "Math, Range" },
            { "DisplayName", "One Minus" }
        };
    }
}
