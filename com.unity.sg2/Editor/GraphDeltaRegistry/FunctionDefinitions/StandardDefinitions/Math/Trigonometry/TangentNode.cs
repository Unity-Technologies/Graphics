using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TangentNode : IStandardNode
    {
        public static string Name = "Tangent";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Default",
                    "Out = tan(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                ),
                new(
                    1,
                    "Fast",
@"
{
    In *= 0.1592;
    a = In + 0.25;
    Out = ((8.0 - 16.0 * abs(In)) * In) / ((8.0 - 16.0 * abs(a)) * a);
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
            tooltip: "returns the tangent of the input",
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
                    tooltip: "the tangent of the input"
                )
            }
        );
    }
}
