using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RectangleNode : IStandardNode
    {
        static string Name = "Rectangle";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Fastest",
@"
{
    w.x = Width;
	w.y = Height;
	d = abs(UV * 2 - 1) - w;
#if defined(SHADER_STAGE_RAY_TRACING)
    d = saturate((1 - saturate(d * 1e7)));
#else
    d = saturate(1 - d / fwidth(d));
#endif
    Out = min(d.x, d.y);
}",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("Width", TYPE.Float, Usage.In, new float[] {0.5f}),
                    new ParameterDescriptor("Height", TYPE.Float, Usage.In, new float[] {0.5f}),
                    new ParameterDescriptor("w", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("d", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                ),
                new(
                    1,
                    "Nicest",
@"
{
    UV = UV * 2.0 - 1.0;
    w.x = Width;
	w.y = Height;
#if defined(SHADER_STAGE_RAY_TRACING)
    o = saturate(0.5f + 1e7 * (w - abs(UV)));
    o = min(o, 1e7 * w * 2.0f);
#else
    k = 1.0f / (min(fwidth(UV), 0.5f));
    o = saturate(0.5f + k * (w - abs(UV)));
    o = min(o, k * w * 2.0f);
#endif
    Out = o.x * o.y;
}",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("Width", TYPE.Float, Usage.In, new float[] {0.5f}),
                    new ParameterDescriptor("Height", TYPE.Float, Usage.In, new float[] {0.5f}),
                    new ParameterDescriptor("w", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("d", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("o", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("k", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Creates a 4-sided polygon.",
            categories: new string[2] { "Procedural", "Shape" },
            synonyms: new string[1] { "square" },
            selectableFunctions: new()
            {
                { "Fastest", "Fastest" },
                { "Nicest", "Nicest" }
            },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the coordinates used to create the rectangle"
                ),
                new ParameterUIDescriptor(
                    name: "Width",
                    tooltip: "the width of the rectangle"
                ),
                new ParameterUIDescriptor(
                    name: "Height",
                    tooltip: "the height of the rectangle"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a 4-sided polygon"
                )
            }
        );
    }
}
