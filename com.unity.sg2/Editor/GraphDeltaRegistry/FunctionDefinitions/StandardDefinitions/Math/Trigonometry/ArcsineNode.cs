using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ArcsineNode : IStandardNode
    {
        public static string Name => "Arcsine";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Default",
                    "Out = asin(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new(
                    "Fast",
@"
{
    a = In * In;
    Out = In * (1 + a * (0.166667 + a * (0.075 + a * 0.04464)));
}",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("a", TYPE.Vector, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the arcsine of each component of the input.",
            category: "Math/Trigonometry",
            synonyms: new string[1] { "asine" },
            description: "pkg://Documentation~/previews/Arcsine.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Default", "Default" },
                { "Fast", "Fast" }
            },
            hasModes: true,
            functionSelectorLabel: "Mode",
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
