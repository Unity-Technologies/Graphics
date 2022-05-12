using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class WhiteBalanceNode : IStandardNode
    {
        public static string Name = "WhiteBalance";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,

@"    // Range ~[-1.67;1.67] works best
    t1 = Temperature * 10 / 6;

    // Get the CIE xy chromaticity of the reference white point.
    // Note: 0.31271 = x value on the D65 white point
    x = 0.31271 - t1 * (t1 < 0 ? 0.1 : 0.05);
    y = (2.87 * x - 3 * x * x - 0.27509507) + (Tint * 10 / 6) * 0.05;

    // CIExyToLMS
    X = 1 * x / y;
    Z = 1 * (1 - x - y) / y;
    w2.x = (0.7328 * X + 0.4296 * 1 - 0.1624 * Z);
    w2.y = (-0.7036 * X + 1.6975 * 1 + 0.0061 * Z);
    w2.z = (0.0030 * X + 0.0136 * 1 + 0.9834 * Z);

    lms = mul(LIN_2_LMS_MAT, In);
    lms.x *= w1.x / w2.x;
    lms.y *= w1.y / w2.y;
    lms.z *= w1.z / w2.z;
    Out = mul(LMS_2_LIN_MAT, lms);",
            new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("Temperature", TYPE.Float, Usage.In),
            new ParameterDescriptor("Tint", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
            new ParameterDescriptor("t1", TYPE.Float, Usage.Local),
            new ParameterDescriptor("x", TYPE.Float, Usage.Local),
            new ParameterDescriptor("y", TYPE.Float, Usage.Local),
            new ParameterDescriptor("w1", TYPE.Vec3, Usage.Local, new float[] { 0.949237f, 1.03542f, 1.08728f }), // D65 white point
            new ParameterDescriptor("X", TYPE.Float, Usage.Local),
            new ParameterDescriptor("Z", TYPE.Float, Usage.Local),
            new ParameterDescriptor("w2", TYPE.Vec3, Usage.Local),
            new ParameterDescriptor("lms", TYPE.Vec3, Usage.Local),
            new ParameterDescriptor("LIN_2_LMS_MAT", TYPE.Mat3, Usage.Local, new float[] { 3.90405e-1f, 5.49941e-1f, 8.92632e-3f,
                                                                                           7.08416e-2f, 9.63172e-1f, 1.35775e-3f,
                                                                                           2.31082e-2f, 1.28021e-1f, 9.36245e-1f }),
            new ParameterDescriptor("LMS_2_LIN_MAT", TYPE.Mat3, Usage.Local, new float[] { 2.85847e+0f, -1.62879e+0f, -2.48910e-2f,
                                                                                           -2.10182e-1f,  1.15820e+0f,  3.24281e-4f,
                                                                                           -4.18120e-2f, -1.18169e-1f,  1.06867e+0f })
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Saturation",
            tooltip: "adjusts temperature and tint",
            categories: new string[2] { "Artistic", "Adjustment" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a color to adjust"
                ),
                new ParameterUIDescriptor(
                    name: "Temperature",
                    tooltip: "shifts towards yellow or blue"
                ),
                 new ParameterUIDescriptor(
                    name: "Tint",
                    tooltip: "shifts towards pink or green"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "color with adjusted white balance"
                )
            }
        );
    }
}
