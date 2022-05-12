using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class RadialShearNode : IStandardNode
    {
        public static string Name = "RadialShear";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"  delta = UV - Center;
    delta2 = dot(delta.xy, delta.xy);
    delta_offset = delta2 * Strength;
    temp.x = delta.y;
    temp.y = -delta.x;
    Out = UV + temp * delta_offset + Offset;",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, defaultValue: REF.UV0),
            new ParameterDescriptor("Center", TYPE.Vec2, Usage.In, new float[] { 0.5f, 0.5f }),
            new ParameterDescriptor("Strength", TYPE.Vec2, Usage.In, new float[] { 10f, 10f }),
            new ParameterDescriptor("Offset", TYPE.Vec2, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out),
            new ParameterDescriptor("delta", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("delta2", TYPE.Float, Usage.Local),
            new ParameterDescriptor("delta_offset", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("temp", TYPE.Vec2, Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Applies a radial shear warping effect to the UVs, similar to a wave.",
            categories: new string[1] { "UV" },
            synonyms: new string[0] { },
            displayName:"Radial Shear",
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
                    tooltip: "radially warped UV coordinates"
                )            }
        );
    }
}
