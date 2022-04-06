using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SmoothstepNode : IStandardNode
    {
        public static string Name = "Smoothstep";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = smoothstep(Edge1, Edge2, In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Edge1", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Edge2", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the result of a smooth Hermite interpolation between 0 and 1",
            categories: new string[2] { "Math", "Interpolation" },
            synonyms: new string[1] { "curve" },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Edge1",
                    tooltip: "minimum step value"
                ),
                new ParameterUIDescriptor(
                    name: "Edge2",
                    tooltip: "maximum step value"
                ),

                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the result of a smooth Hermite interpolation between 0 and 1"
                )
            }
        );
    }
}
