using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class MainLightDirectionNode : IStandardNode
    {
        static string Name = "MainLightDirection";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"#if SHADERGRAPH_PREVIEW
Direction = half3(-0.5, -0.5, 0);
#else
Direction = SHADERGRAPH_MAIN_LIGHT_DIRECTION();
#endif
",
            new ParameterDescriptor("Direction", TYPE.Vec3, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the direction of the main directional light in the scene.",
            categories: new string[2] { "Input", "Lighting" },
            hasPreview: false,
            synonyms: new string[1] { "sun" },
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Direction",
                    tooltip: "The direction of the main directional light in the scene."
                )
            }
        );
    }
}
