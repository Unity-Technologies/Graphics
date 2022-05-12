using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SpherizeNode : IStandardNode
    {
        public static string Name = "Spherize";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"  delta = UV - Center;
    delta2 = dot(delta.xy, delta.xy);
    delta4 = delta2 * delta2;
    delta_offset = delta4 * Strength;
    Out = UV + delta * delta_offset + Offset;",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, defaultValue: REF.UV0),
            new ParameterDescriptor("Center", TYPE.Vec2, Usage.In, new float[] { 0.5f, 0.5f }),
            new ParameterDescriptor("Strength", TYPE.Vec2, Usage.In, new float[] { 10f, 10f }),
            new ParameterDescriptor("Offset", TYPE.Vec2, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out),
            new ParameterDescriptor("delta", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("delta2", TYPE.Float, Usage.Local),
            new ParameterDescriptor("delta_offset", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("delta4", TYPE.Float, Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Applies a spherical warping effect to the input UVs, similar to a fisheye camera lens.",
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
                    tooltip: "spherically warped UV coordinates"
                )            }
        );
    }
}
