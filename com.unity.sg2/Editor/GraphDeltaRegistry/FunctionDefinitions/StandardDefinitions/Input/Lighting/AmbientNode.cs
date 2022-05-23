using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class AmbientNode : IStandardNode
    {
        static string Name = "Ambient";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"Color_Sky = Ambient_Sky;
Equator = Ambient_Equator;
Ground = Ambient_Ground;
",
            new ParameterDescriptor("Color_Sky", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("Equator", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("Ground", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("Ambient_Sky", TYPE.Vec3, GraphType.Usage.Local, REF.Ambient_Sky),
            new ParameterDescriptor("Ambient_Equator", TYPE.Vec3, GraphType.Usage.Local, REF.Ambient_Equator),
            new ParameterDescriptor("Ambient_Ground", TYPE.Vec3, GraphType.Usage.Local, REF.Ambient_Ground)
            );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Accesses the scene's ambient color values.",
            categories: new string[2] { "Input", "Lighting" },
            hasPreview: false,
            synonyms: new string[0] { },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "Color_Sky",
                    displayName:"Color/Sky",
                    tooltip: "Color (Color) or Sky (Gradient) color value."
                ),
                new ParameterUIDescriptor(
                    name: "Equator",
                    tooltip: "Equator (Gradient) color value."
                ),
                new ParameterUIDescriptor(
                    name: "Ground",
                    tooltip: "Ground (Gradient) color value."
                )
            }
        );
    }
}
