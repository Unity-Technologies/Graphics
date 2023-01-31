using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class MainLightDirectionNode : IStandardNode
    {
        public static string Name => "MainLightDirection";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"#if SHADERGRAPH_PREVIEW
    Direction = half3(-0.5, -0.5, 0);
#else
    Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
#endif",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Direction", TYPE.Vec3, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the direction of the main directional light in the scene.",
            category: "Input/Lighting",
            hasPreview: false,
            displayName: "Main Light Direction",
            synonyms: new string[1] { "sun" },
            description: "pkg://Documentation~/previews/MainLightDirection.md",
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Direction",
                    tooltip: "The direction of the main directional light in the scene."
                )
            }
        );
    }
}
