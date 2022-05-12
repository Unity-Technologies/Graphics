using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class TilingAndOffsetNode : IStandardNode
    {
        public static string Name = "TilingAndOffset";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = UV * Tiling + Offset;",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, defaultValue: REF.UV0),
            new ParameterDescriptor("Tiling", TYPE.Vec2, Usage.In, new float[] { 1f, 1f}),
            new ParameterDescriptor("Offset", TYPE.Vec2, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Tiles and offsets the input UVs.",
            categories: new string[1] { "UV" },
            synonyms: new string[2] { "pan", "scale" },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "input UV value",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Tiling",
                    tooltip: "amount of tiling to apply per channel"
                ),
                new ParameterUIDescriptor(
                    name: "Offset",
                    tooltip: "amount of offset to apply per channel"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "output UV value"
                )
            }
        );
    }
}
