using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CosineNode : IStandardNode
    {
        public static string Name = "Cosine";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Default",
                    "Out = cos(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                ),
                new(
                    1,
                    "Fast",
@"
{
    In = (In + 1.57) * 0.1592;
    Out = (8.0 - 16.0 * abs(In * 0.1592)) * (In * 0.1592);
}",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the cosine of the input",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[0],
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
                    tooltip: "the cosine of the input"
                )
            }
        );
    }
}
