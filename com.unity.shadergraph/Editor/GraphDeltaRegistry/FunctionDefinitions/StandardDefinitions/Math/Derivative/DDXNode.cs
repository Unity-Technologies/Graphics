using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class DDXNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,          // Version
            "DDX",    // Name
            "Out = ddx(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "derivative, slope" },
            { "Tooltip", "returns the partial derivative of the input with respect to the screen-space x-coordinate" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the difference between the value of the current pixel and the horizontal neighboring pixel" },
            { "Category", "Math, Derivative" }
        };
    }
}
