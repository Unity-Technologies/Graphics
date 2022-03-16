using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SubtractNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Subtract", // Name
            "Out = A - B;",
            new ParameterDescriptor("A", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("B", TYPE.Vector, GraphType.Usage.In, new float[] { 1f, 1f, 1f, 1f }),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Basic" },
            { "Name.Synonyms", "subtraction, remove, -, minus" },
            { "Tooltip", "removes the value of B from A" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "A minus B" }
        };
    }
}
