using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HyperbolicTangentNode : IStandardNode
    {
        public static string Name => "HyperbolicTangent";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Default",
                    "Out = tanh(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new(
                    "Fast",
                    "Out = ((In - ((In * In * In) / 3)) + ((2 * In * In * In * In * In) / 15)) - ((17 / 315) * (In * In * In * In * In * In * In));",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Hyperbolic Tangent",
            tooltip: "Calculates the hyperbolic tangent of the input.",
            category: "Math/Trigonometry",
            synonyms: new string[1] { "tanh" },
            description: "pkg://Documentation~/previews/HyperbolicTangent.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Default", "Default" },
                { "Fast", "Fast" }
            },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the hyperbolic tangent of the input"
                )
            }
        );
    }
}
