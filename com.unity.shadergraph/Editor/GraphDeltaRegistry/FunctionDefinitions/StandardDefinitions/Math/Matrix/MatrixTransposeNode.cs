using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class MatrixTransposeNode : IStandardNode
    {
        public static string Name = "MatrixTranspose";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = transpose(In);",
            new ParameterDescriptor("In", TYPE.Matrix, Usage.In, new float[] { 1f, 0f, 0f, 1f}),
            new ParameterDescriptor("Out", TYPE.Matrix, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Matrix Transpose",
            tooltip: "returns the transposed value of the input matrix",
            categories: new string[2] { "Math", "Matrix" },
            synonyms: new string[1] { "Transpose" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input matrix"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the transposed value of the input matrix"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "Transpose" },
            { "Tooltip", "returns the transposed value of the input matrix" },
            { "Category", "Math, Matrix" },
            { "DisplayName", "Matrix Transpose" },
            { "Parameters.In.Tooltip", "input matrix" },
            { "Parameters.Out.Tooltip", "the transposed value of the input matrix" }
        };

        public static Dictionary<string, float> UIHints => new()
        {
            { "Preview.Exists", 0 }
        };
    }
}
