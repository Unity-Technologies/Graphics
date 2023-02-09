using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class HyperbolicSineNode : IStandardNode
    {
        public static string Name => "HyperbolicSine";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Default",
                    "Out = sinh(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new(
                    "Fast",
                    "Out = (In + ((In * In * In) / 6)) + ((In * In * In * In * In) / 120);",
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
            displayName: "Hyperbolic Sine",
            tooltip: "Calculates the hyperbolic sine of the input.",
            category: "Math/Trigonometry",
            synonyms: new string[1] { "sinh" },
            description: "pkg://Documentation~/previews/HyperbolicSine.md",
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
                    tooltip: "the hyperbolic sine of the input"
                )
            }
        );
    }
}
