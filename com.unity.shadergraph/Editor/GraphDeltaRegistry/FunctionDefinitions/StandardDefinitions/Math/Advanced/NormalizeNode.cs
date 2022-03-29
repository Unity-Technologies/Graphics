using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NormalizeNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,              // Version
            "Normalize",    // Name
            "Out = normalize(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Advanced" },
            { "Name.Synonyms", "Unitize" },
            { "Tooltip", "adjusts the input vector to unit length" },
            { "Parameters.In.Tooltip", "a vector to normalize" },
            { "Parameters.Out.Tooltip", "In / Length(In)" }
        };
    }
}
