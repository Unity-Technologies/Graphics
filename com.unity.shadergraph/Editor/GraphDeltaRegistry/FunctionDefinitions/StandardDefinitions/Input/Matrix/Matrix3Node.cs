using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class Matrix3Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Matrix3x3", // Name
            "Out = Matrix3x3;",
            new ParameterDescriptor("Matrix3x3", TYPE.Mat3, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Mat3, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Input, Matrix" },
            { "Tooltip", "creates a static 3x3 matrix" },
            { "Parameters.Out.Tooltip", "a 3x3 matrix" }
        };
    }
}
