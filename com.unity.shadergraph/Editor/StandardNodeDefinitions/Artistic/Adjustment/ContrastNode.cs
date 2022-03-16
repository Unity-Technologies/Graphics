using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class ContrastNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Contrast", // Name
@"
{
    midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}",
            new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.In),
            new ParameterDescriptor("Contrast", TYPE.Float, GraphType.Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("midpoint", TYPE.Float, GraphType.Usage.Local)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Artistic, Adjustment" },
            { "Name.Synonyms", "intensity" },
            { "Tooltip", "make the darks darker and the brights brighter" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Contrast.Tooltip", "less than 1 reduces contrast, greater than 1 increases it" },
            { "Parameters.Out.Tooltip", "color with adjusted contrast" }
        };
    }
}
