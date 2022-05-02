using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RotateAboutAxisNode : IStandardNode
    {
        static string Name = "RotateAboutAxis";
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
    sincos(Rotation, s, c);
    one_minus_c = 1.0 - c;
    Axis = normalize(Axis);
	rot_mat[0].x = one_minus_c * Axis.x * Axis.x + c;
	rot_mat[0].y = one_minus_c * Axis.x * Axis.y - Axis.z * s;
	rot_mat[0].z = one_minus_c * Axis.z * Axis.x + Axis.y * s;
	rot_mat[1].x = one_minus_c * Axis.x * Axis.y + Axis.z * s;
	rot_mat[1].y = one_minus_c * Axis.y * Axis.y + c;
	rot_mat[1].z = one_minus_c * Axis.y * Axis.z - Axis.x * s;
	rot_mat[2].x = one_minus_c * Axis.z * Axis.x - Axis.y * s;
	rot_mat[2].y = one_minus_c * Axis.y * Axis.z + Axis.x * s;
	rot_mat[2].z = 	one_minus_c * Axis.z * Axis.z + c;
    Out = mul(rot_mat,  In);
}
",
                    new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Axis", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Rotation", TYPE.Float, Usage.In),
                    new ParameterDescriptor("s", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("c", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("one_minus_c", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("rot_mat", TYPE.Mat3, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                ),
                new(
                    1,
                    "Degrees",
@"
{
    Rotation = radians(Rotation);
    sincos(Rotation, s, c);
    one_minus_c = 1.0 - c;
    Axis = normalize(Axis);
	rot_mat[0].x = one_minus_c * Axis.x * Axis.x + c;
	rot_mat[0].y = one_minus_c * Axis.x * Axis.y - Axis.z * s;
	rot_mat[0].z = one_minus_c * Axis.z * Axis.x + Axis.y * s;
	rot_mat[1].x = one_minus_c * Axis.x * Axis.y + Axis.z * s;
	rot_mat[1].y = one_minus_c * Axis.y * Axis.y + c;
	rot_mat[1].z = one_minus_c * Axis.y * Axis.z - Axis.x * s;
	rot_mat[2].x = one_minus_c * Axis.z * Axis.x - Axis.y * s;
	rot_mat[2].y = one_minus_c * Axis.y * Axis.z + Axis.x * s;
	rot_mat[2].z = 	one_minus_c * Axis.z * Axis.z + c;
    Out = mul(rot_mat,  In);
}
",
                    new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Axis", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Rotation", TYPE.Float, Usage.In),
                    new ParameterDescriptor("s", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("c", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("one_minus_c", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("rot_mat", TYPE.Mat3, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "rotates the input vector around an axis by the given value",
            categories: new string[2] { "Math", "Vector" },
            synonyms: new string[1] { "pivot" },
            selectableFunctions: new()
            {
                { "Radians", "Radians" },
                { "Degrees", "Degrees" }
            },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "the vector to rotate"
                ),
                new ParameterUIDescriptor(
                    name: "Axis",
                    tooltip: "the rotation axis for the vector"
                ),
                new ParameterUIDescriptor(
                    name: "Rotation",
                    tooltip: "the amount of rotation to apply in radians or degrees"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a rotated vector"
                )
            }
        );
    }
}
