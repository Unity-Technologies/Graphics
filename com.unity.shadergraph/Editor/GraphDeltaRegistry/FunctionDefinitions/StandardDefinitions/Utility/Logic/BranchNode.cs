using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace com.unity.shadergraph.defs
{

    internal class BranchNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Branch", // Name
            "Out = Predicate ? True : False;",
            new ParameterDescriptor("Predicate", TYPE.Bool, Usage.In),
            new ParameterDescriptor("True", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f}),
            new ParameterDescriptor("False", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Utility, Logic" },
            { "Name.Synonyms", "switch, if, else" },
            { "Tooltip", "provides a dynamic branch to the shader. Both sides of the branch will be evaluated" },
            { "Parameters.Predicate.Tooltip", "value of predicate" },
            { "Parameters.True.Tooltip", "true branch" },
            { "Parameters.False.Tooltip", "false branch" },
            { "Parameters.Out.Tooltip", "either the True branch or the False branch depending on the value of predicate" }
        };
    }
}
