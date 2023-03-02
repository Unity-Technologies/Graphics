using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Arctangent2Node : IStandardNode
    {
        public static string Name => "Arctangent2";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Default",
                    "    Out = atan2(A, B);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("B", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new(
                    "Fast",
@"   t0 = max( abs(B), abs(A) );
   t3 = min( abs(B), abs(A) ) / t0;
   t4 = t3 * t3;
   t0 = + 0.0872929;
   t0 = t0 * t4 - 0.301895;
   t0 = t0 * t4 + 1.0;
   t3 = t0 * t3;
   t3 = abs(A) > abs(B) ? (0.5 * 3.1415926) - t3 : t3;
   t3 = B < 0 ? 3.1415926 - t3 : t3;
   t3 = A < 0 ? -t3 : t3;
   Out = t3;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("A", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("B", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("t0", TYPE.Vector, Usage.Local),
                        new ParameterDescriptor("t3", TYPE.Vector, Usage.Local),
                        new ParameterDescriptor("t4", TYPE.Vector, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Arctangent 2",
            tooltip: "Calculates the arctangent of input A divided by input B.",
            category: "Math/Trigonometry",
            synonyms: new string[1] { "atan2" },
            description: "pkg://Documentation~/previews/Arctangent2.md",
            selectableFunctions: new()
            {
                { "Default", "Default" },
                { "Fast", "Fast" }
            },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "the numerator"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "the denominator"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the arctangent of A divided by B"
                )
            }
        );
    }
}
