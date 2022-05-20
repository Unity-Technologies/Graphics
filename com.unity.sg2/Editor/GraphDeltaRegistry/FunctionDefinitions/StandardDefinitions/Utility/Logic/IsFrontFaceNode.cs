using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class IsFrontFaceNode : IStandardNode
    {
        static string Name = "IsFrontFace";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
"Out = max(0, FaceSign.x);",
            new ParameterDescriptor("Out", TYPE.Bool, GraphType.Usage.Out),
            new ParameterDescriptor("FaceSign", TYPE.Bool, GraphType.Usage.Local, REF.FaceSign)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Checks if the current face is facing the camera.",
            categories: new string[2] { "Utility", "Logic" },
            hasPreview: false,
            synonyms: new string[2] { "face", "side" },
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Returns true if the current face is facing the camera, and returns false otherwise."

                )
            }
        );
    }
}
