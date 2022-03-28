using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class MatrixDeterminantNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "MatrixDeterminant",
            "Out = determinant(In);",
            new ParameterDescriptor("In", TYPE.Matrix, Usage.In, new float[] { 1f, 0f, 0f, 1f}),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "Determinant" },
            { "Tooltip", "returns the determinant of the matrix defined by the input" },
            { "Parameters.In.Tooltip", "input matrix" },
            { "Parameters.Out.Tooltip", "the determinant of the input matrix" },
            { "Category", "Math, Matrix" },
            { "DisplayName", "Matrix Determinant" }
        };

        public static Dictionary<string, float> UIHints => new()
        {
            { "Preview.Exists", 0 }
        };
    }
}
