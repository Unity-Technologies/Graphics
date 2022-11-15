using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BillboardNode : IStandardNode
    {
        static string Name = "Billboard";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
                    Version,
                    Name,
                    functions: new FunctionDescriptor[] {
                new(
                    "Cylindrical",

@"temp1.x = UNITY_MATRIX_M[0].x;
    temp1.y = UNITY_MATRIX_M[1].x;
    temp1.z = UNITY_MATRIX_M[2].x;
    temp2.x = UNITY_MATRIX_M[0].y;
    temp2.y = UNITY_MATRIX_M[1].y;
    temp2.z = UNITY_MATRIX_M[2].y;
    temp3.x = UNITY_MATRIX_M[0].z;
    temp3.y = UNITY_MATRIX_M[1].z;
    temp3.z = UNITY_MATRIX_M[2].z;
    Scale.x = length(temp1);
    Scale.y = length(temp2);
    Scale.z = length(temp3);
    rotationMatrix[0] = UNITY_MATRIX_I_V[0];
    rotationMatrix[1] = UpDir;
    rotationMatrix[2] = UNITY_MATRIX_I_V[2];
    rotationMatrix[3] = UNITY_MATRIX_I_V[3];
    Scaled_Pos.xyz = IN.ObjectSpacePosition * Scale;
    Scaled_Pos.w = 0;
    Out = TransformWorldToObject(SHADERGRAPH_OBJECT_POSITION + mul(rotationMatrix, Scaled_Pos).xyz);
",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Scale", TYPE.Vec3, GraphType.Usage.Local),
                        new ParameterDescriptor("temp1", TYPE.Vec3, GraphType.Usage.Local),
                        new ParameterDescriptor("temp2", TYPE.Vec3, GraphType.Usage.Local),
                        new ParameterDescriptor("temp3", TYPE.Vec3, GraphType.Usage.Local),
                        new ParameterDescriptor("UpDir", TYPE.Vec4, GraphType.Usage.Local, new float[] {0f, 1f, 0f, 0f }),
                        new ParameterDescriptor("rotationMatrix", TYPE.Mat4, GraphType.Usage.Local),
                        new ParameterDescriptor("Scaled_Pos", TYPE.Vec4, GraphType.Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Spherical",

@"temp1.x = UNITY_MATRIX_M[0].x;
    temp1.y = UNITY_MATRIX_M[1].x;
    temp1.z = UNITY_MATRIX_M[2].x;
    temp2.x = UNITY_MATRIX_M[0].y;
    temp2.y = UNITY_MATRIX_M[1].y;
    temp2.z = UNITY_MATRIX_M[2].y;
    temp3.x = UNITY_MATRIX_M[0].z;
    temp3.y = UNITY_MATRIX_M[1].z;
    temp3.z = UNITY_MATRIX_M[2].z;
    Scale.x = length(temp1);
    Scale.y = length(temp2);
    Scale.z = length(temp3);
    Scaled_Pos.xyz = IN.ObjectSpacePosition * Scale;
    Scaled_Pos.w = 0;
    Out = TransformWorldToObject(SHADERGRAPH_OBJECT_POSITION + mul(UNITY_MATRIX_I_V, Scaled_Pos).xyz);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Scale", TYPE.Vec3, GraphType.Usage.Local),
                        new ParameterDescriptor("temp1", TYPE.Vec3, GraphType.Usage.Local),
                        new ParameterDescriptor("temp2", TYPE.Vec3, GraphType.Usage.Local),
                        new ParameterDescriptor("temp3", TYPE.Vec3, GraphType.Usage.Local),
                        new ParameterDescriptor("Scaled_Pos", TYPE.Vec4, GraphType.Usage.Local),
                            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                )
                    }
                );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Billboard",
            tooltip: "Rotate the vertex posiiton to align all three axes with the camera or only the x and z axes.",
            category: "Utility",
            hasPreview: false,
            synonyms: new string[1] { "" },
            selectableFunctions: new()
        {
            { "Cylindrical", "Cylindrical" },
            { "Spherical", "Spherical" }
        },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Billboard vertex position in object space"
                )
            }
        );
    }
}
