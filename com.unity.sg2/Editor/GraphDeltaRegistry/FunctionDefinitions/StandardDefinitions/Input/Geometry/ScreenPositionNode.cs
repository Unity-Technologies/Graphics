using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ScreenPositionNode : IStandardNode
    {
        public static string Name => "ScreenPosition";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = ScreenPosition;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out),
                new ParameterDescriptor("ScreenPosition", TYPE.Vec4, GraphType.Usage.Static, REF.ScreenPosition_Default)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "The location on the screen of the current pixel.",
            category: "Input/Geometry",
            synonyms: new string[0] {},
            displayName: "Screen Position",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "ScreenPosition",
                    displayName: "Space",
                    options: REF.OptionList.ScreenPositions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Mesh's screen position in selected coordinate space."
                )
            }
        );
    }
}
