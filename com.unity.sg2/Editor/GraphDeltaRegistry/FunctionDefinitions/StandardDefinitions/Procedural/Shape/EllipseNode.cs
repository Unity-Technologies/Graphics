using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class EllipseNode : IStandardNode
    {
        public static string Name = "Ellipse";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
    temp.x = Width;
    temp.y = Height;
#if defined(SHADER_STAGE_RAY_TRACING)
    Out = saturate((1.0 - length((UV * 2 - 1) / temp)) * 1e7);
#else
    d = length((UV * 2 - 1) / temp);
    Out = saturate((1 - d) / fwidth(d));
#endif
",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
            new ParameterDescriptor("Width", TYPE.Float, Usage.In, new float[] { 0.5f }),
            new ParameterDescriptor("Height", TYPE.Float, Usage.In, new float[] { 0.5f }),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
            new ParameterDescriptor("d", TYPE.Float, Usage.Local),
            new ParameterDescriptor("temp", TYPE.Vec2, Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Generates an ellipse shape.",
            categories: new string[2] { "Procedural", "Shape" },
            synonyms: new string[1] { "circle" },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "Width",
                    tooltip: "ellipse width"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the input UV"
                ),
                new ParameterUIDescriptor(
                    name: "Height",
                    tooltip: "ellipse height"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a procedurally-generated ellipse shape"
                )
            }
        );
    }
}
