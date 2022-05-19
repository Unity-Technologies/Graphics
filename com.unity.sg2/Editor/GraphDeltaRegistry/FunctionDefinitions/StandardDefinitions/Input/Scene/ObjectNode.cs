using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ObjectNode : IStandardNode
    {
        static string Name = "Object";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
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
Position = SHADERGRAPH_OBJECT_POSITION;
",
            new ParameterDescriptor("Position", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("Scale", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("temp1", TYPE.Vec3, GraphType.Usage.Local),
            new ParameterDescriptor("temp2", TYPE.Vec3, GraphType.Usage.Local),
            new ParameterDescriptor("temp3", TYPE.Vec3, GraphType.Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Provides access to the current object's position and scale.",
            categories: new string[2] { "Input", "Scene" },
            hasPreview: false,
            synonyms: new string[2] { "position", "scale" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Position",
                    tooltip: "The Object position in world space."
                ),
                new ParameterUIDescriptor(
                    name: "Scale",
                    tooltip: "The object scale in world space."
                )
            }
        );
    }
}
