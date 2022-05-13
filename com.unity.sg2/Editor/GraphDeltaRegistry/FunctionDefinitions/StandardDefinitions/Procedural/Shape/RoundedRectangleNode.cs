using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class RoundedRectangleNode : IStandardNode
    {
        public static string Name = "RoundedRectangle";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"  temp.x = Width;
    temp.y = Height;
    Radius = max(min(min(abs(Radius * 2), abs(Width)), abs(Height)), 1e-5);
    uv = abs(UV * 2 - 1) - temp + Radius;
    d = length(max(0, uv)) / Radius;
#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate((1 - d) * 1e7);
#else
    Out = saturate((1 - d) / max(fwidth(d), 1e-5));
#endif",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
            new ParameterDescriptor("Width", TYPE.Float, Usage.In, new float[] { 0.5f }),
            new ParameterDescriptor("Height", TYPE.Float, Usage.In, new float[] { 0.5f }),
            new ParameterDescriptor("Radius", TYPE.Float, Usage.In, new float[] { 0.1f }),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out),//should be fragmant stage only
            new ParameterDescriptor("d", TYPE.Float, Usage.Local),
            new ParameterDescriptor("uv", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("temp", TYPE.Vec2, Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Creates a 4-sided polygon with rounded corners.",
            categories: new string[2] { "Procedural", "Shape" },
            synonyms: new string[1] { "square" },
            displayName: "Rounded Rectangle",
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Width",
                    tooltip: "rounded rectangle width"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the input UV",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Height",
                    tooltip: "rounded rectangle height"
                ),
                new ParameterUIDescriptor(
                    name: "Radius",
                    tooltip: "corner radius"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "A rectangle shape with rounded corners with the given dimensions."
                )            }
        );
    }
}
