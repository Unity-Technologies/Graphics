using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SceneColorNode : IStandardNode
    {
        static string Name = "SceneColor";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
"    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);",
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("UV", TYPE.Vec4, GraphType.Usage.In, REF.WorldSpace_Normal)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the camera's color buffer.",
            categories: new string[2] { "Input", "Scene" },
            hasPreview: false,
            synonyms: new string[1] { "screen buffer" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "The output color value."
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    options: REF.OptionList.Normals,
                    tooltip: "Normalized screen coordinates."

                )
            }
        );
    }
}
