using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TangentVectorNode : IStandardNode
    {
        static string Name = "TangentVector";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = TangentVector;",
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("TangentVector", TYPE.Vec3, GraphType.Usage.Static, REF.WorldSpace_Tangent)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a tangent vector in the selected space.",
            categories: new string[2] { "Input", "Geometry" },
            synonyms: new string[0] { },
            displayName: "Tangent Vector",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "TangentVector",
                    options: REF.OptionList.Tangents
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Mesh's tangent vector in selected space."
                )
            }
        );
    }
}
