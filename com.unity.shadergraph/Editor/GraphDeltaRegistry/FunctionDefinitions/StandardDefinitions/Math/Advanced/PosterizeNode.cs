using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class PosterizeNode : IStandardNode
    {
        public static string Name = "Posterize";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = floor(In / (1 / Steps)) * (1 / Steps);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Steps", TYPE.Vector, Usage.In, new float[] { 4f, 4f, 4f, 4f }),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the quantized value of In",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[1] { "quantize" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a value to quantize"
                ),
                new ParameterUIDescriptor(
                    name: "Steps",
                    tooltip: "the number of quantization steps"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the quantized value of In"
                )
            }
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
