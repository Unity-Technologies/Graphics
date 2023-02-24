using System;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TransformationMatrixNode : IStandardNode
    {
        public static string Name => "TransformationMatrix";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = Matrix;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Mat4, GraphType.Usage.Out),
                new ParameterDescriptor("Matrix", TYPE.Mat4, GraphType.Usage.Static, REF.M)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a transformation matrix from one of model, view, projection, or view space.",
            category: "Input/Matrix",
            synonyms: Array.Empty<string>(),
            hasPreview: false,
            displayName: "Transformation Matrix",
            description: "pkg://Documentation~/previews/TransformationMatrix.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Matrix",
                    options: REF.OptionList.Matrices
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the selected transform matrix"
                )
            }
        );
    }
}
