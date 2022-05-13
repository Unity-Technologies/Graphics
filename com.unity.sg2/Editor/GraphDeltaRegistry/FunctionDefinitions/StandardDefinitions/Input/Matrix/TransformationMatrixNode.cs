using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TransformationMatrixNode : IStandardNode
    {
        static string Name = "TransformationMatrix";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = Matrix;",
            new ParameterDescriptor("Out", TYPE.Mat4, GraphType.Usage.Out),
            new ParameterDescriptor("Matrix", TYPE.Mat4, GraphType.Usage.Static, REF.M)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a transformation matrix from one of model, view, projection, or view space.",
            categories: new string[2] { "Input", "Matrix" },
            synonyms: new string[0] { },
            hasPreview: false,
            displayName: "Transformation Matrix",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Matrix",
                    options: REF.OptionList.Matrices
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the selected transform matrix"
                )
            }
        );
    }
}
