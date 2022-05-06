using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ArcsineNode : IStandardNode
    {
        public static string Name = "Arcsine";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Default",
                    "Out = asin(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                ),
                new(
                    1,
                    "Fast",
@"
{
    a = In * In;
    Out = In * (1 + a * (0.166667 + a * (0.075 + a * 0.04464)));
}",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                    new ParameterDescriptor("a", TYPE.Vector, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Contrast",
            tooltip: "Calculates the arcsine of each component of the input.",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[1] { "asine" },
            selectableFunctions: new()
            {
                { "Default", "Default" },
                { "Fast", "Fast" }
            },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the arcsine of each component of the input"
                )
            }
        );
    }
}
