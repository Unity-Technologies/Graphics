using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EyeIndexNode : IStandardNode
    {
        public static string Name => "EyeIndex";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"
#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    Out = EyeIndex;
#else
    Out = 0;
#endif
",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out),
                new ParameterDescriptor("EyeIndex", TYPE.Float, GraphType.Usage.Local, REF.StereoEyeIndex)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the Eye Index when stereo rendering is enabled.",
            category: "Input/Scene",
            hasPreview: false,
            synonyms: new string[2] { "stereo", "3d" },
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "The eye index for the camera of a stereo draw."
                )
            }
        );
    }
}
