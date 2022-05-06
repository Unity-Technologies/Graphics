using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class PolarCoordinatesNode : IStandardNode
    {
        public static string Name = "PolarCoordinates";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
    delta = UV - Center;
    radius = length(delta) * 2 * RadialScale;
    angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
    Out.x = radius;
    Out.y = angle;
}",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
            new ParameterDescriptor("Center", TYPE.Vec2, Usage.In, new float[] { 0.5f, 0.5f }),
            new ParameterDescriptor("RadialScale", TYPE.Float, Usage.In, new float[] { 1f }),
            new ParameterDescriptor("LengthScale", TYPE.Float, Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out),
            new ParameterDescriptor("delta", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("radius", TYPE.Float, Usage.Local),
            new ParameterDescriptor("angle", TYPE.Float, Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Converts input UVs to polar coordinates.",
            categories: new string[1] { "UV"},
            synonyms: new string[0] {  },
            displayName: "Polar Coordinates",
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Center",
                    tooltip: "center reference point"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the input UV"
                ),
                new ParameterUIDescriptor(
                    name: "RadialScale",
                    tooltip: "scale of distance value"
                ),
                new ParameterUIDescriptor(
                    name: "LengthScale",
                    tooltip: "scale of angle value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "UVs converted to polar coordinates"
                )            }
        );
    }
}
