using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class PosterizeNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Posterize", // Name
            "Out = floor(In / (1 / Steps)) * (1 / Steps);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Steps", TYPE.Vector, GraphType.Usage.In, new float[] { 4f, 4f, 4f, 4f }),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
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
