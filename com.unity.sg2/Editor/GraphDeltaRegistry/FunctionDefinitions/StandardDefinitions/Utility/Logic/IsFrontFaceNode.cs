using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class IsFrontFaceNode : IStandardNode
    {
        public static string Name => "IsFrontFace";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
"    Out = max(0, FaceSign);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Bool, GraphType.Usage.Out),
                new ParameterDescriptor("FaceSign", TYPE.Bool, GraphType.Usage.Local, REF.FaceSign)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Checks if the current face is facing the camera.",
            category: "Utility/Logic",
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
