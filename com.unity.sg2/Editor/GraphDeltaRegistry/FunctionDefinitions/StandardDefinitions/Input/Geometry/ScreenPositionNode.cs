using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ScreenPositionNode : IStandardNode
    {
        static string Name = "ScreenPosition";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = ScreenPosition;",
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out),
            new ParameterDescriptor("ScreenPosition", TYPE.Vec4, GraphType.Usage.Static, REF.ScreenPosition_Default)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "The location on the screen of the current pixel.",
            categories: new string[2] { "Input", "Geometry" },
            synonyms: new string[0] {},
            displayName: "Screen Position",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "ScreenPosition",
                    options: REF.OptionList.ScreenPositions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Mesh's screen position in selected coordinate space."
                )
            }
        );
    }
}
