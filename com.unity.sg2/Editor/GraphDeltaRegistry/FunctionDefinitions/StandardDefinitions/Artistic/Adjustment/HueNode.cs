using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HueNode : IStandardNode
    {
        static string Name = "Hue";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Degrees",
@"
    temp1.rg = In.bg;
    temp1.ba = K.wz;
    temp2.rg = In.gb;
    temp2.ba = K.xy;
    P = lerp(temp1, temp2, step(In.b, In.g));
    temp1.xyz = P.xyw;
    temp1.w = In.r;
    temp2.r = In.r;
    temp2.gba = P.yzx;
    Q = lerp(temp1, temp2, step(P.x, In.r));
    D = Q.x - min(Q.w, Q.y);
    hsv.x = abs(Q.z + (Q.w - Q.y)/(6.0 * D + E));
	hsv.y = D / (Q.x + E);
	hsv.z = (D == 0) ? Q.x : (Q.x + E);
    hue = hsv.x + Offset / 360;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;
    // HSV to RGB
    Out = hsv.z * lerp(K2.xxx, saturate((abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www)) - K2.xxx), hsv.y);
",
                    new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Offset", TYPE.Float, Usage.In),
                    new ParameterDescriptor("temp1", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("temp2", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("K", TYPE.Vec4, Usage.Local, new float[] { 0.0f, (float)(-1.0 / 3.0), (float)(2.0 / 3.0), -1f }),
                    new ParameterDescriptor("K2", TYPE.Vec4, Usage.Local, new float[] { 1.0f, (float)(2.0 / 3.0), (float)(1.0 / 3.0), 3f }),
                    new ParameterDescriptor("P", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("Q", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("D", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("E", TYPE.Float, Usage.Local, new float[] { 0.0000000001f }),
                    new ParameterDescriptor("hsv", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("hue", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                ),
                 new(
                    1,
                    "Normalized",
@"
    temp1.rg = In.bg;
    temp1.ba = K.wz;
    temp2.rg = In.gb;
    temp2.ba = K.xy;
    P = lerp(temp1, temp2, step(In.b, In.g));
    temp1.xyz = P.xyw;
    temp1.w = In.r;
    temp2.r = In.r;
    temp2.gba = P.yzx;
    Q = lerp(temp1, temp2, step(P.x, In.r));
    D = Q.x - min(Q.w, Q.y);
    hsv.x = abs(Q.z + (Q.w - Q.y)/(6.0 * D + E));
	hsv.y = D / (Q.x + E);
	hsv.z = (D == 0) ? Q.x : (Q.x + E);
    hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;
    // HSV to RGB
    Out = hsv.z * lerp(K2.xxx, saturate((abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www)) - K2.xxx), hsv.y);
",
                    new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] { 0.5f }),
                    new ParameterDescriptor("temp1", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("temp2", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("K", TYPE.Vec4, Usage.Local, new float[] { 0.0f, -0.333333f, 0.666667f, -1f }),
                    new ParameterDescriptor("K2", TYPE.Vec4, Usage.Local, new float[] { 1.0f, 0.666667f, 0.333333f, 3f }),
                    new ParameterDescriptor("P", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("Q", TYPE.Vec4, Usage.Local),
                    new ParameterDescriptor("D", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("E", TYPE.Float, Usage.Local, new float[] { 0.0000000001f }),
                    new ParameterDescriptor("hsv", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("hue", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "shifts the color to a different point on the color wheel",
            categories: new string[2] { "Artistic", "Adjustment" },
            synonyms: new string[0] {  },
            selectableFunctions: new()
            {
                { "Degrees", "Degrees" },
                { "Normalized", "Normalized" }
            },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "an input color"
                ),
                new ParameterUIDescriptor(
                    name: "Offset",
                    tooltip: "a hue offset value in degrees or between 0-1 depending on the selection"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "color with adjusted hue"
                )
            }
        );
    }
}
