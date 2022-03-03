using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class DDXYNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,          // Version
            "DDXY",    // Name
            "Out = abs(ddx(In)) + abs(ddy(In));",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "derivative, slope" },
            { "Tooltip", "returns the sum of both partial derivatives of the input" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the sum of both partial derivatives of the input" },
            { "Category", "Math, Derivative" }
        };
    }
}
