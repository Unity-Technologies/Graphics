using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class Matrix4Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Matrix4x4", // Name
            "Out = Matrix4x4;",
            new ParameterDescriptor("Matrix4x4", TYPE.Mat4, Usage.Static),
            new ParameterDescriptor("Out", TYPE.Mat4, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Input, Matrix" },
            { "Tooltip", "creates a static 4x4 matrix" },
            { "Parameters.Out.Tooltip", "a 4x4 matrix" }
        };
    }
}
