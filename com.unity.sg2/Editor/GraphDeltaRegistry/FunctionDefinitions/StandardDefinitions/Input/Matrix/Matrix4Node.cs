using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Matrix4Node : IStandardNode
    {
        public static string Name => "Matrix4x4";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = Matrix4x4;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Matrix4x4", TYPE.Mat4, Usage.Static),
                new ParameterDescriptor("Out", TYPE.Mat4, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Matrix 4x4",
            tooltip: "make the darks darker and the brights brighter",
            category: "Input/Matrix",
            synonyms: Array.Empty<string>(),
            hasPreview: false,
            description: "pkg://Documentation~/previews/Matrix4x4.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Matrix4x4"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "a 4x4 matrix"
                )
            }
        );
    }
}
