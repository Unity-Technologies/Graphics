using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RandomRangeNode : IStandardNode
    {
        public static string Name = "RandomRange";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
    Out = lerp(Min, Max, frac(sin(dot(Seed, temp))*43758.5453));
}",
            new ParameterDescriptor("Seed", TYPE.Vec2, Usage.In),
            new ParameterDescriptor("Min", TYPE.Float, Usage.In),
            new ParameterDescriptor("Max", TYPE.Float, Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
            new ParameterDescriptor("temp", TYPE.Vec2, Usage.Local, new float[] { 12.9898f, 78.233f })
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Random Range",
            tooltip: "Creates a psuedo-random value between min and max.",
            categories: new string[2] { "Math", "Range" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "Seed",
                    tooltip: "input Seed"
                ),
                new ParameterUIDescriptor(
                    name: "Min",
                    tooltip: "minimum value"
                ),
                new ParameterUIDescriptor(
                    name: "Max",
                    tooltip: "maximum value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a psuedo-random value between min and max"
                )
            }
        );
    }
}
