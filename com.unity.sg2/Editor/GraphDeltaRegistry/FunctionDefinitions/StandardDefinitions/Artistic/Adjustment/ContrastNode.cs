using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class ContrastNode : IStandardNode
    {
        public static string Name => "Contrast";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Cheap",
            @"
                midpoint = pow(0.5, 2.2);
                Out =  (In - midpoint) * Contrast + midpoint;
            ",
                    new ParameterDescriptor[]
                    {

                        new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("Contrast", TYPE.Float, Usage.In, new float[] { 1f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
                        new ParameterDescriptor("midpoint", TYPE.Float, Usage.Local)
                    }
                ),
                new (
                    "Quality",
            @"
                folded = (In > 0.5) ? 1 - In : In;
                curved = pow(folded * 2, Contrast) / 2;
                Out = (In > 0.5) ? 1 - curved : curved;
            ",
                    new ParameterDescriptor[]
                    {

                        new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("Contrast", TYPE.Float, Usage.In, new float[] { 1f }),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
                        new ParameterDescriptor("folded", TYPE.Vec3, Usage.Local),
                        new ParameterDescriptor("curved", TYPE.Vec3, Usage.Local)
                    }
                )
            }

        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName : "Contrast",
            tooltip: "Make the darks darker and the brights brighter.",
            category: "Artistic/Adjustment",
            description: "pkg://Documentation~/previews/Contrast.md",
            synonyms: new string[1] { "intensity" },
            selectableFunctions: new()
            {
                { "Cheap", "Cheap" },
                { "Quality", "Quality" }
            },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Contrast",
                    tooltip: "less than 1 reduces contrast, greater than 1 increases it"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "color with adjusted contrast"
                )
            }
        );
    }
}
