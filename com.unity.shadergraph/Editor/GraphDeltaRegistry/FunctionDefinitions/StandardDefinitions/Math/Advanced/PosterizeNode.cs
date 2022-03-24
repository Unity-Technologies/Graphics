using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class PosterizeNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Posterize", // Name
            "Out = floor(In / (1 / Steps)) * (1 / Steps);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Steps", TYPE.Vector, Usage.In, new float[] { 4f, 4f, 4f, 4f }),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Advanced" },
            { "Name.Synonyms", "quantize" },
            { "Tooltip", "returns the quantized value of In" },
            { "Parameters.In.Tooltip", "a value to quantize" },
            { "Parameters.Steps.Tooltip", "the number of quantization steps" },
            { "Parameters.Out.Tooltip", "the quantized value of In" }
        };
    }
}
