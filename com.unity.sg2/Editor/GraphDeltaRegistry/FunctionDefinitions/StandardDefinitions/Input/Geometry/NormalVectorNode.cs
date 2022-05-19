using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NormalVectorNode : IStandardNode
    {
        static string Name = "NormalVector";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = NormalVector;",
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("NormalVector", TYPE.Vec3, GraphType.Usage.Static, REF.WorldSpace_Normal)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a vector that defines the direction the point is facing.",
            categories: new string[2] { "Input", "Geometry" },
            synonyms: new string[1] { "surface direction" },
            displayName: "Normal Vector",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "NormalVector",
                    options: REF.OptionList.Normals
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Mesh's normal vector in selected space."
                )
            }
        );
    }
}
