using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class ColorReplacementNode : IStandardNode
    {
        public static string Name => "ColorReplacement";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new (
            Name,
@"  Out = lerp(To, In, saturate((distance(From, In) - Range) / max(Fuzziness, 1e-5f)));",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("From", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("To", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("Range", TYPE.Float, Usage.In),
                new ParameterDescriptor("Fuzziness", TYPE.Float, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Color Replacement",
            tooltip: "Converts the chosen color from an input to another color value.",
            category: "Artistic/Adjustment",
            synonyms: new string[1] { "Replace Color" },
            description: "pkg://Documentation~/previews/ColorReplacement.md",
            parameters: new ParameterUIDescriptor[6]
            {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "From",
                    tooltip: "color to replace",
                    useColor:true
                ),
                new ParameterUIDescriptor(
                    name: "To",
                    tooltip: "color to replace with",
                    useColor:true
                ),
                new ParameterUIDescriptor(
                    name: "Range",
                    tooltip: "replace colors within this range from input From"
                ),
                new ParameterUIDescriptor(
                    name: "Fuzziness",
                    tooltip: "soften edges around selection"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "in color with colors replaced"
                )
            }
        );
    }
}
