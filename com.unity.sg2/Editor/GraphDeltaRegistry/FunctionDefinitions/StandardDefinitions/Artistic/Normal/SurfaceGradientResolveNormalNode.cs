using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SurfaceGradientResolveNormalNode : IStandardNode
    {
        public static string Name => "SurfaceGradientResolveNormal";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = SafeNormalize(Normal - SurfaceGradient);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Normal", TYPE.Vec3, GraphType.Usage.In),
                new ParameterDescriptor("SurfaceGradient", TYPE.Vec3, GraphType.Usage.In),
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Combines a base normal with a surface gradient",
            category: "Artistic/Normal",
            synonyms: new string[3] { "blend", "mix", "combine" },
            displayName: "Surface Gradient Resolve Normal",
            hasPreview: false,
            description: "pkg://Documentation~/previews/SurfaceGradientResolveNormal.md",
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "the normal to combine"
                ),
                new ParameterUIDescriptor(
                    name: "SurfaceGradient",
                    displayName: "Surface Gradient",
                    tooltip: "the surface gradient to combine"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the combination of the normal and the surface gradient"
                )
            }
        );
    }
}
