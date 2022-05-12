using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class PolygonNode : IStandardNode
    {
        public static string Name = "Polygon";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"  temp1 = cos(pi / Sides);
    temp.x = Width * temp1;
    temp.y = Height * temp1;
    uv = (UV * 2 - 1) / temp;
    uv.y *= -1;
    pCoord = atan2(uv.x, uv.y);
    r = 2 * pi / Sides;
    dist = cos(floor(0.5 + pCoord / r) * r - pCoord) * length(uv);

#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate((1.0 - dist) * 1e7);
#else
    Out = saturate((1 - dist) / fwidth(dist));
#endif",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
            new ParameterDescriptor("Sides", TYPE.Float, Usage.In, new float[] { 6f }),
            new ParameterDescriptor("Width", TYPE.Float, Usage.In, new float[] { 0.5f }),
            new ParameterDescriptor("Height", TYPE.Float, Usage.In, new float[] { 0.5f }),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out),//should be fragmant stage only
            new ParameterDescriptor("pi", TYPE.Float, Usage.Local, new float[] { 3.14159265359f }),
            new ParameterDescriptor("uv", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("pCoord", TYPE.Float, Usage.Local),
            new ParameterDescriptor("dist", TYPE.Float, Usage.Local),
            new ParameterDescriptor("r", TYPE.Float, Usage.Local),
            new ParameterDescriptor("temp", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("temp1", TYPE.Vec2, Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Creates a polygon of the specified size and shape.",
            categories: new string[2] { "Procedural", "Shape" },
            synonyms: new string[1] { "Shape" },
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Width",
                    tooltip: "polygon width"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the input UV"
                ),
                new ParameterUIDescriptor(
                    name: "Height",
                    tooltip: "polygon height"
                ),
                new ParameterUIDescriptor(
                    name: "Sides",
                    tooltip: "amount of sides"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a polygon shape with the given dimensions"
                )
            }
        );
    }
}
