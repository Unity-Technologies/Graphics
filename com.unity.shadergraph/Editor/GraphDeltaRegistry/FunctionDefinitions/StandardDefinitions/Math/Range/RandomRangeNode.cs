using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class RandomRangeNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "RandomRange",
@"
{
    Out = lerp(Min, Max, frac(sin(dot(Seed, temp))*43758.5453));
}",
            new ParameterDescriptor("Seed", TYPE.Vec2, GraphType.Usage.In),
            new ParameterDescriptor("Min", TYPE.Float, GraphType.Usage.In),
            new ParameterDescriptor("Max", TYPE.Float, GraphType.Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("temp", TYPE.Vec2, GraphType.Usage.Local, new float[] { 12.9898f, 78.233f })
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Range" },
            { "DisplayName", "Random Range" },
            { "Tooltip", "returns a psuedo-random value base on input Seed between Min and Max" },
            { "Parameters.Seed.Tooltip", "input Seed" },
            { "Parameters.Min.Tooltip", "minimum value" },
            { "Parameters.Max.Tooltip", "maximum value" },
            { "Parameters.Out.Tooltip", "a psuedo-random value between min and max" }
        };
    }
}
