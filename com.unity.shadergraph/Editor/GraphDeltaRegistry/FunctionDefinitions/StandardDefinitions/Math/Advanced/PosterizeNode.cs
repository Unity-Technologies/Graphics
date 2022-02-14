using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class PosterizeNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Posterize", // Name
            "Out = floor(In / (1 / Steps)) * (1 / Steps);",
            new ParameterDescriptor("In", TYPE.Any, Usage.In),
            new ParameterDescriptor("Steps", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "quantize" },
            { "Tooltip", "returns the quantized value of In" },
            { "Parameters.In.Tooltip", "a value to quantize" },
            { "Parameters.Steps.Tooltip", "the number of quantization steps" },
            { "Parameters.Out.Tooltip", "the quantized value of In" }
        };
    }
}
