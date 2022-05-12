using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class TwirlNode : IStandardNode
    {
        public static string Name = "Twirl";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"  delta = UV - Center;
    angle = Strength * length(delta);
    x = cos(angle) * delta.x - sin(angle) * delta.y;
    y = sin(angle) * delta.x + cos(angle) * delta.y;
    Out.x = x + Center.x + Offset.x;
    Out.y = y + Center.y + Offset.y;",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, defaultValue: REF.UV0),
            new ParameterDescriptor("Center", TYPE.Vec2, Usage.In, new float[] { 0.5f, 0.5f }),
            new ParameterDescriptor("Strength", TYPE.Float, Usage.In, new float[] { 10f, 10f }),
            new ParameterDescriptor("Offset", TYPE.Vec2, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out),
            new ParameterDescriptor("delta", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("angle", TYPE.Float, Usage.Local),
            new ParameterDescriptor("x", TYPE.Float, Usage.Local),
            new ParameterDescriptor("y", TYPE.Float, Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Applies a twirl-warping effect to the input UVs, similar to a black hole.",
            categories: new string[1] { "UV" },
            synonyms: new string[0] { },
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Center",
                    tooltip: "center reference point"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the input UV",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Strength",
                    tooltip: "strength of the effect"
                ),
                new ParameterUIDescriptor(
                    name: "Offset",
                    tooltip: "individual channel offsets"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "twirled UV coordinates"
                )            }
        );
    }
}
