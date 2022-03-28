using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class TriangleWaveNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "TriangleWave",
            "Out = 2.0 * abs( 2 * (In - floor(0.5 + In)) ) - 1.0;",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "sawtooth wave" },
            { "Tooltip", "creates a linear wave form with triangular shapes" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "a triangle wave" },
            { "Category", "Math, Wave" },
            { "DisplayName", "Triangle Wave" }
        };
    }
}
