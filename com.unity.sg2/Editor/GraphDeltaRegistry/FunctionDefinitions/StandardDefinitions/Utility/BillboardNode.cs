using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BillboardNode : IStandardNode
    {
        static string Name => "Billboard";
        static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
                    Version,
                    Name,
                    "Billboard",
                    functions: new FunctionDescriptor[] {
                     new(
                    "Spherical",
    @"Scale = mul(temp, UNITY_MATRIX_M);
    rotationMatrix = UNITY_MATRIX_I_V;
    Scaled_Pos.xyz = Position * Scale;
    Scaled_Pos.w = 0;
    BillboardPosition = TransformWorldToObject(SHADERGRAPH_OBJECT_POSITION + CenterOffset + mul(rotationMatrix, Scaled_Pos).xyz);
    tempNormal.xyz = Normal;
    tempNormal.w = 0;
    tempTangent.xyz = Tangent;
    tempTangent.w = 0;
    BillboardNormal = TransformWorldToObject(mul(rotationMatrix, tempNormal).xyz);
    BillboardTangent = TransformWorldToObject(mul(rotationMatrix, tempTangent).xyz);",
                new ParameterDescriptor[]
                {
                    new ParameterDescriptor("Scale", TYPE.Vec3, GraphType.Usage.Local),
                    new ParameterDescriptor("temp", TYPE.Vec3, GraphType.Usage.Local, new float[] {1f, 1f, 1f }),
                    new ParameterDescriptor("CenterOffset", TYPE.Vec3, GraphType.Usage.Local, new float[] {0f, .5f, 0f }),
                    new ParameterDescriptor("tempNormal", TYPE.Vec4, GraphType.Usage.Local),
                    new ParameterDescriptor("tempTangent", TYPE.Vec4, GraphType.Usage.Local),
                    new ParameterDescriptor("rotationMatrix", TYPE.Mat4, GraphType.Usage.Local),
                    new ParameterDescriptor("Scaled_Pos", TYPE.Vec4, GraphType.Usage.Local),
                    new ParameterDescriptor("Position", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Position),
                    new ParameterDescriptor("Normal", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Normal),
                    new ParameterDescriptor("Tangent", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Tangent),
                    new ParameterDescriptor("BillboardPosition", TYPE.Vec3, GraphType.Usage.Out),
                    new ParameterDescriptor("BillboardNormal", TYPE.Vec3, GraphType.Usage.Out),
                    new ParameterDescriptor("BillboardTangent", TYPE.Vec3, GraphType.Usage.Out)
                }
                  ),
                     new(
                    "Cylindrical",
    @"Scale = mul(temp, UNITY_MATRIX_M);
    rotationMatrix = UNITY_MATRIX_I_V;
    rotationMatrix[1] = UpDir;
    Scaled_Pos.xyz = Position * Scale;
    Scaled_Pos.w = 0;
    BillboardPosition = TransformWorldToObject(SHADERGRAPH_OBJECT_POSITION + CenterOffset + mul(rotationMatrix, Scaled_Pos).xyz);
    tempNormal.xyz = Normal;
    tempNormal.w = 0;
    tempTangent.xyz = Tangent;
    tempTangent.w = 0;
    BillboardNormal = TransformWorldToObject(mul(rotationMatrix, tempNormal).xyz);
    BillboardTangent = TransformWorldToObject(mul(rotationMatrix, tempTangent).xyz);",
                new ParameterDescriptor[]
                {
                    new ParameterDescriptor("Scale", TYPE.Vec3, GraphType.Usage.Local),
                    new ParameterDescriptor("temp", TYPE.Vec3, GraphType.Usage.Local, new float[] {1f, 1f, 1f }),
                    new ParameterDescriptor("UpDir", TYPE.Vec4, GraphType.Usage.Local, new float[] {0f, 1f, 0f, 0f }),
                    new ParameterDescriptor("CenterOffset", TYPE.Vec3, GraphType.Usage.Local, new float[] {0f, .5f, 0f }),
                    new ParameterDescriptor("tempNormal", TYPE.Vec4, GraphType.Usage.Local),
                    new ParameterDescriptor("tempTangent", TYPE.Vec4, GraphType.Usage.Local),
                    new ParameterDescriptor("rotationMatrix", TYPE.Mat4, GraphType.Usage.Local),
                    new ParameterDescriptor("Scaled_Pos", TYPE.Vec4, GraphType.Usage.Local),
                    new ParameterDescriptor("Position", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Position),
                    new ParameterDescriptor("Normal", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Normal),
                    new ParameterDescriptor("Tangent", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Tangent),
                    new ParameterDescriptor("BillboardPosition", TYPE.Vec3, GraphType.Usage.Out),
                    new ParameterDescriptor("BillboardNormal", TYPE.Vec3, GraphType.Usage.Out),
                    new ParameterDescriptor("BillboardTangent", TYPE.Vec3, GraphType.Usage.Out)
                }
            )
        }
    );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Billboard",
            tooltip: "Rotates the vertex posiiton, normal, and tangent to align all three axes with the camera or only the x and z axes.",
            category: "Input/Mesh Deformation",
            hasPreview: false,
            synonyms: new string[] { "align, facing, rotate, pivot" },
            selectableFunctions: new()
            {
                { "Spherical", "Spherical" },
                { "Cylindrical", "Cylindrical" }
            },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "Position",
                    tooltip: "The input vertex postion"
                ),
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "The input vertex Normal"
                ),
                new ParameterUIDescriptor(
                    name: "Tangent",
                    tooltip: "The input vertex Tangent"
                ),
                new ParameterUIDescriptor(
                    name: "BillboardPosition",
                    displayName:"Billboard Position",
                    tooltip: "The billboard vertex position"
                ),
                new ParameterUIDescriptor(
                    name: "BillboardNormal",
                    displayName:"Billboard Normal",
                    tooltip: "The billboard vertex normal"
                ),
                new ParameterUIDescriptor(
                    name: "BillboardTangent",
                    displayName:"Billboard Tangent",
                    tooltip: "The billboard vertex tangent"
                )
            }
        );
    }
}
