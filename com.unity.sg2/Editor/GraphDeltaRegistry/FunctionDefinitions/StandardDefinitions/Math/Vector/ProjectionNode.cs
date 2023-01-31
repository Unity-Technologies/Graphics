using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ProjectionNode : IStandardNode
    {
        public static string Name => "Projection";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = B * dot(A, B) / dot(B, B);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("A", TYPE.Vector, Usage.In),
                new ParameterDescriptor("B", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Projects input A onto a straight line parallel to input B.",
            category: "Math/Vector",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/Projection.md",
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "Input A"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "Input B"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the result of projecting A onto a straight line parallel to B"
                )
            }
        );
    }
}
