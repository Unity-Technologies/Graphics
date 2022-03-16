using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class ReplaceColorNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "ReplaceColor",
@"
{
    Out = lerp(To, In, saturate((distance(From, In) - Range) / max(Fuzziness, 1e-5f)));
}",
            new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.In),
            new ParameterDescriptor("From", TYPE.Vec3, GraphType.Usage.In),//TODO: Need to be color picker 
            new ParameterDescriptor("To", TYPE.Vec3, GraphType.Usage.In),//Need to be color picker 
            new ParameterDescriptor("Range", TYPE.Float, GraphType.Usage.In),
            new ParameterDescriptor("Fuzziness", TYPE.Float, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Artistic, Adjustment" },
            { "DisplayName", "Replace Color" },
            { "Tooltip", "converts the chosen colors to another color value" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.From.Tooltip", "color to replace" },
            { "Parameters.Range.Tooltip", "replace colors within this range from input From" },
            { "Parameters.Fuzziness.Tooltip", "soften edges around selection" },
            { "Parameters.Out.Tooltip", "in color with colors replaced" }
        };
    }
}
