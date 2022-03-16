using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class DivideNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Divide", // Name
            "Out = A / B;",
            new ParameterDescriptor("A", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, GraphType.Usage.In, new float[] { 2f, 2f, 2f, 2f }),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Basic" },
            { "Name.Synonyms", "division, /, divided by" },
            { "Tooltip", "splits A by the number of B" },
            { "Parameters.A.Tooltip", "the numerator" },
            { "Parameters.B.Tooltip", "the denominator" },
            { "Parameters.Out.Tooltip", "A divided by B" }
        };
    }
}
