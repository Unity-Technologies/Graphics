using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ScreenNode : IStandardNode
    {
        static string Name = "Screen";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"Width = ScreenParams.x;
Height = ScreenParams.y;",
            new ParameterDescriptor("Width", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("Height", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("ScreenParams", TYPE.Vec2, GraphType.Usage.Static, REF.ScreenParams)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Provides access to the screen's width and height parameters.",
            categories: new string[2] { "Input", "Scene" },
            hasPreview: false,
            synonyms: new string[0] { },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "Width",
                    tooltip: "Screen's width in pixels."
                ),
                new ParameterUIDescriptor(
                    name: "Height",
                    options: REF.OptionList.Normals,
                    tooltip: "Screen's height in pixels."

                )
            }
        );
    }
}
