using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class MatrixTransposeNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "MatrixTranspose",
            "Out = transpose(In);",
            new ParameterDescriptor("In", TYPE.Matrix, Usage.In, new float[] { 1f, 0f, 0f, 1f}),
            new ParameterDescriptor("Out", TYPE.Matrix, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "Transpose" },
            { "Tooltip", "returns the transposed value of the input matrix" },
            { "Parameters.In.Tooltip", "input matrix" },
            { "Parameters.Out.Tooltip", "the transposed value of the input matrix" },
            { "Category", "Math, Matrix" },
            { "DisplayName", "Matrix Transpose" }
        };
    }
}
