using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Matrix3Node : IStandardNode
    {
        public static string Name => "Matrix3x3";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = Matrix3x3;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Matrix3x3", TYPE.Mat3, Usage.Static),
                new ParameterDescriptor("Out", TYPE.Mat3, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Matrix 3x3",
            tooltip: "creates a static 3x3 matrix",
            category: "Input/Matrix",
            synonyms: Array.Empty<string>(),
            hasPreview: false,
            description: "pkg://Documentation~/previews/Matrix3x3.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Matrix3x3"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "a 3x3 matrix"
                )
            }
        );
    }
}
