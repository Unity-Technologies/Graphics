using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class SquareWaveNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "SquareWave",
            "Out = 1.0 - 2.0 * round(frac(In));",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
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
