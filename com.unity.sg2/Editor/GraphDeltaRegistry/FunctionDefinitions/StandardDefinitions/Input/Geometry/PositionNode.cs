using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class PositionNode : IStandardNode
    {
        static string Name = "Position";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = Position;",
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("Position", TYPE.Vec3, GraphType.Usage.Static, REF.WorldSpace_Position)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the location of the point in object, view, world, or tangent space.",
            categories: new string[2] { "Input", "Geometry" },
            synonyms: new string[1] { "location" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Position",
                    options: REF.OptionList.Positions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the location of the point in the selected space."
                )
            }
        );
    }
}
