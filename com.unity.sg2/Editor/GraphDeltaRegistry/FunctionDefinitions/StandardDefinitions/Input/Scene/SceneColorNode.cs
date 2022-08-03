using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SceneColorNode : IStandardNode
    {
        public static string Name => "SceneColor";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
"    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
                new ParameterDescriptor("UV", TYPE.Vec4, GraphType.Usage.In, REF.ScreenPosition_Default)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Scene Color",
            tooltip: "Gets the camera's color buffer.",
            category: "Input/Scene",
            hasPreview: false,
            synonyms: new string[1] { "screen buffer" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "The output color value."
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    options: REF.OptionList.ScreenPositions,
                    tooltip: "Normalized screen coordinates."

                )
            }
        );
    }
}
