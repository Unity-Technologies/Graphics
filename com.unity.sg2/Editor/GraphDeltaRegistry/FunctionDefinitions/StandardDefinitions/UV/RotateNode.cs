using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RotateNode : IStandardNode
    {
        static string Name = "Rotate";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Radians",
@"
{
    UV -= Center;
	sincos(Rotation, s, c);
	rMatrix[0].x = c;
	rMatrix[0].y = -s;
	rMatrix[1].x = s;
	rMatrix[1].y = c;
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    Out = UV;
}",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("Center", TYPE.Vec2, Usage.In, new float[] { 0.5f, 0.5f}),
                    new ParameterDescriptor("Rotation", TYPE.Float, Usage.In),
                    new ParameterDescriptor("s", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("c", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("rMatrix", TYPE.Mat2, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
                ),
                new(
                    1,
                    "Degrees",
@"
{
    Rotation = radians(Rotation);
    UV -= Center;
	sincos(Rotation, s, c);
	rMatrix[0].x = c;
	rMatrix[0].y = -s;
	rMatrix[1].x = s;
	rMatrix[1].y = c;
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    Out = UV;
}",
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("Center", TYPE.Vec2, Usage.In, new float[] { 0.5f, 0.5f}),
                    new ParameterDescriptor("Rotation", TYPE.Float, Usage.In),
                    new ParameterDescriptor("s", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("c", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("rMatrix", TYPE.Mat2, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out)
                ),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "rotates UVs around a pivot point ",
            categories: new string[1] { "UV" },
            synonyms: new string[0] {  },
            selectableFunctions: new()
            {
                { "Radians", "Radians" },
                { "Degrees", "Degrees" }
            },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the UVs to rotate"
                ),
                new ParameterUIDescriptor(
                    name: "Center",
                    tooltip: "the pivot point of the rotation"
                ),
                new ParameterUIDescriptor(
                    name: "Rotation",
                    tooltip: "the amount of rotation to apply in radians or degrees"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "rotated UVs"
                )
            }
        );
    }
}
