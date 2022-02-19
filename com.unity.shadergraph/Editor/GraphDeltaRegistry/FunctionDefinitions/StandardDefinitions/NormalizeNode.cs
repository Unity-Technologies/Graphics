using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class NormalizeNode : IStandardNode
    {
        public FunctionDescriptor FunctionDescriptor => new(
            1,              // Version
            "Normalize",    // Name
            "Out = normalize(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "Normalize, Unitize" },
            { "Tooltip", "Normalization Function" },
            { "Parameters.In.Tooltip", "Input" },
            { "Parameters.Out.Tooltip", "In / Length(In)" }
        };
    }
}
