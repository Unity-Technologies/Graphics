using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class SawtoothWaveNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "SawtoothWave",
            "Out = 2 * (In - floor(0.5 + In));",
            new ParameterDescriptor("In", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "triangle wave" },
            { "Tooltip", "creates a wave form with a slow, linear ramp up and then an instant drop" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "a sawtooth wave" },
            { "Category", "Math, Wave" },
            { "DisplayName", "Sawtooth Wave" }
        };
    }
}
