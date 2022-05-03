using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class CheckerboardNode : IStandardNode
    {
        public static string Name = "Checkerboard";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
#if defined(SHADER_STAGE_RAY_TRACING)
    int2 checker = frac(Frequency * UV) > 0.5;
    Out = checker.x ^ checker.y ? ColorA : ColorB;
#else
    UV = (UV.xy + 0.5) * Frequency;
    distance3 = 4.0 * abs(frac(UV + 0.25) - 0.5) - 1.0;
    derivatives.xy = ddx(UV);
    derivatives.zw = ddy(UV);
    duv_length.x = dot(derivatives.xz, derivatives.xz);
    duv_length.y = dot(derivatives.yw, derivatives.yw);
    duv_length = sqrt(duv_length);
    scale = 0.35 / duv_length.xy;
    freqLimiter = sqrt(clamp(1.1f - max(duv_length.x, duv_length.y), 0.0, 1.0));
    vector_alpha = clamp(distance3 * scale.xy, -1.0, 1.0);
    alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y * freqLimiter);
    Out = lerp(ColorA, ColorB, alpha.xxx);
#endif
}",
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
            new ParameterDescriptor("ColorA", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("ColorB", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("Frequency", TYPE.Vec2, Usage.Out, new float[] { 1f, 1f }),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
            new ParameterDescriptor("distance3", TYPE.Vec2, Usage.Local),
            //new ParameterDescriptor("derivatives", TYPE.Vec4, Usage.Local),
            new ParameterDescriptor("duv_length", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("scale", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("freqLimiter", TYPE.Float, Usage.Local),
            new ParameterDescriptor("vector_alpha", TYPE.Vec2, Usage.Local),
            new ParameterDescriptor("alpha", TYPE.Float, Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates a pattern of alternating black and white grid squares",
            categories: new string[1] { "Procedural" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Frequency",
                    tooltip: "the scale of checkerboard per axis"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the input UV"
                ),
                new ParameterUIDescriptor(
                    name: "ColorA",
                    tooltip: "the first checker color",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "ColorB",
                    tooltip: "the second checker color",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a pattern of alternating black and white grid squares"
                )
            }
        );
    }
}
