using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class UVNode : IStandardNode
    {
        public static string Name => "UV";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "   UV = Channel.xy; XYZW = Channel;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Channel", TYPE.Vec4, GraphType.Usage.Static, REF.UV0),
                new ParameterDescriptor("UV", TYPE.Vec2, GraphType.Usage.Out),
                new ParameterDescriptor("XYZW", TYPE.Vec4, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the texture coordinates from the selected channel.",
            category: "Input/Geometry",
            synonyms: new string[3] { "texcoords", "coords", "coordinates" },
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "Texture coordinate."

                ),
                new ParameterUIDescriptor(
                    name: "XYZW",
                    tooltip: "The complete vec4 texture coordinate."

                ),
                new ParameterUIDescriptor(
                    name: "Channel",
                    options: REF.OptionList.UVs
                )
            }
        );
    }
}
