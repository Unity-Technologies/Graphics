using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class MatrixDeterminantNode : IStandardNode
    {
        public static string Name => "MatrixDeterminant";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = determinant(In);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Matrix, Usage.In, new float[] { 1f, 0f, 0f, 1f}),
                new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Matrix Determinant",
            tooltip: "Calculates the determinant of the matrix defined by the input.",
            category: "Math/Matrix",
            synonyms: new string[1] { "Determinant" },
            hasPreview: false,
            description: "pkg://Documentation~/previews/MatrixDeterminant.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: string.Empty,
                    tooltip: "input matrix"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the determinant of the input matrix"
                )
            }
        );
    }
}
