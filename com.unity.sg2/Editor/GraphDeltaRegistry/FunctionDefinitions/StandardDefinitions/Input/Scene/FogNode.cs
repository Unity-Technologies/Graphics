using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class FogNode : IStandardNode
    {
        public static string Name => "Fog";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    SHADERGRAPH_FOG(Position, Color, Density);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Position", TYPE.Vec3, GraphType.Usage.In, REF.Object_Position),
                new ParameterDescriptor("Color", TYPE.Vec4, GraphType.Usage.Out),
                new ParameterDescriptor("Density", TYPE.Float, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the scene's Fog parameters.",
            category: "Input/Scene",
            hasPreview: false,
            synonyms: new string[2] { "stereo", "3d" },
            description: "pkg://Documentation~/previews/Fog.md",
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "Position",
                    tooltip: "The mesh vertex/fragment's position.",
                    options: REF.OptionList.Positions
                ),
                new ParameterUIDescriptor(
                    name: "Color",
                    tooltip: "The fog color."
                ),
                new ParameterUIDescriptor(
                    name: "Density",
                    tooltip: "Fog density based on depth. Returns a value between 0 and 1, where 0 is no fog and 1 is full fog."
                )
            }
        );
    }
}
