using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BitangentVectorNode : IStandardNode
    {
        static string Name = "BitangentVector";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = BitangentVector;",
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("BitangentVector", TYPE.Vec3, GraphType.Usage.Static, REF.WorldSpace_Bitangent)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a bitangent vector in the selected space.",
            categories: new string[2] { "Input", "Geometry" },
            synonyms: new string[1] { "binormal" },
            displayName: "Bitangent Vector",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "BitangentVector",
                    options: REF.OptionList.Bitangents
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "The bitangent vector in the selected space."
                )
            }
        );
    }
}
