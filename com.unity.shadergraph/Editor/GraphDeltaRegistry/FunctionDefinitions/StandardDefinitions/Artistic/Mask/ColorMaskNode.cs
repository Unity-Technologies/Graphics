using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class ColorMaskNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "ColorMask", // Name
@"
{
    Out = saturate(1 - (distance(MaskColor, In) - Range) / max(Fuzziness, 1e-5));
}",
            new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("MaskColor", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("Range", TYPE.Float, Usage.In),
            new ParameterDescriptor("Fuzziness", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );
        public static Dictionary<string, float> UIHints => new()
        {
            { "MaskColor.UseColor", 1 } // this doesn't work yet
        };

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Artistic, Mask" },
            { "Name.Synonyms", "intensity" },
            { "Tooltip", "creates a mask where the input values match the selected color" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.MaskColor.Tooltip", "color to use for mask" },
            { "Parameters.Range.Tooltip", "select colors within this range from input Mask Color" },
            { "Parameters.Fuzziness.Tooltip", "feather edges around selection. Higher values result in a softer selection mask" },
            { "Parameters.Out.Tooltip", "mask produced from color matches in the input" }
        };
    }
}
