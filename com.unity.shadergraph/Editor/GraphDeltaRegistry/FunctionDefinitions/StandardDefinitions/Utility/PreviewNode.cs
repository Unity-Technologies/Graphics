using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class PreviewNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Preview", // Name
            "Out = In;",
            new ParameterDescriptor("In", TYPE.Any, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Utility" },
            { "Tooltip", "enables you to inspect a preview at a specific point" },
            { "Parameters.Out.Tooltip", "the exact same value as the input" }
        };
    }
}
