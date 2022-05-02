using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HyperbolicCosineNode : IStandardNode
    {
        public static string Name = "HyperbolicCosine";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Default",
                    "Out = cosh(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                ),
                new(
                    1,
                    "Fast",
                    "Out = ((1 + ((In * In) / 2))+((In * In * In * In) / 24)) + ((In * In * In * In * In * In) / 720);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Hyperbolic Cosine",
            tooltip: "returns the hyperbolic cosine of the input",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[1] { "cosh" },
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
                    tooltip: "the hyperbolic cosine of the input"
                )
            }
        );
    }
}
