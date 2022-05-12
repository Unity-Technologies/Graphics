using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class MatrixDeterminantNode : IStandardNode
    {
        public static string Name = "MatrixDeterminant";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = determinant(In);",
            new ParameterDescriptor("In", TYPE.Matrix, Usage.In, new float[] { 1f, 0f, 0f, 1f}),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Matrix Determinant",
            tooltip: "Calculates the determinant of the matrix defined by the input.",
            categories: new string[2] { "Math", "Matrix" },
            synonyms: new string[1] { "Determinant" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input matrix"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the determinant of the input matrix"
                )
            }
        );
    }
}
