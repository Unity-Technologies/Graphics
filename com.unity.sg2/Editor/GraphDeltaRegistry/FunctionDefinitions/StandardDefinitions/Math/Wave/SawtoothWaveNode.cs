using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SawtoothWaveNode : IStandardNode
    {
        public static string Name = "SawtoothWave";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = 2 * (In - floor(0.5 + In));",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Sawtooth Wave",
            tooltip: "creates a wave form with a slow, linear ramp up and then an instant drop",
            categories: new string[2] { "Math", "Wave" },
            synonyms: new string[1] { "triangle wave" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a sawtooth wave"
                )
            }
        );
    }
}
