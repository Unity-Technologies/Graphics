using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SquareWaveNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "SquareWave",
            "Out = 1.0 - 2.0 * round(frac(In));",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Tooltip", "creates a wave form in which the amplitude alternates at a steady frequency between fixed min and max values" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "a square wave" },
            { "Category", "Math, Wave" },
            { "DisplayName", "Square Wave" }
        };
    }
}
