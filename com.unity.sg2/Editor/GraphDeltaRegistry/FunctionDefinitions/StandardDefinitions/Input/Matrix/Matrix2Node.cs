using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Matrix2Node : IStandardNode
    {
        public static string Name => "Matrix2x2";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = Matrix2x2;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Matrix2x2", TYPE.Mat2, Usage.Static),
                new ParameterDescriptor("Out", TYPE.Mat2, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Matrix 2x2",
            tooltip: "creates a static 2x2 matrix",
            category: "Input/Matrix",
            synonyms: Array.Empty<string>(),
            hasPreview: false,
            description: "pkg://Documentation~/previews/Matrix2x2.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Matrix2x2"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a 2x2 matrix"
                )
            }
        );
    }
}
