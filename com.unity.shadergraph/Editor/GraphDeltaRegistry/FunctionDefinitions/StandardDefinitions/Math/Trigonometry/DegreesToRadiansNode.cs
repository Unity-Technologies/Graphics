using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class DegreesToRadiansNode : IStandardNode
    {
        public static string Name = "DegreesToRadians";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = radians(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Degrees To Radians",
            tooltip: "converts degrees to radians",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[3] { "degtorad", "radians", "convert" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a value in degrees"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the input converted to radians"
                )
            }
        );
    }
}
