using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EyeIndexNode : IStandardNode
    {
        static string Name = "EyeIndex";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    Out = EyeIndex;
#else
    Out = 0;
#endif
",//should we use REF.StereoEyeIndex?
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("EyeIndex", TYPE.Float, GraphType.Usage.Static, REF.StereoEyeIndex)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the Eye Index when stereo rendering is enabled.",
            categories: new string[2] { "Input", "Scene" },
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
