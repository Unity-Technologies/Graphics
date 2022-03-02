using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class RandomRangeNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "RandomRange",
@"
{
    randomno =  frac(sin(dot(Seed, temp))*43758.5453);
    Out = lerp(Min, Max, randomno);
}",
            new ParameterDescriptor("Seed", TYPE.Vec2, Usage.In),
            new ParameterDescriptor("Min", TYPE.Float, Usage.In),
            new ParameterDescriptor("Max", TYPE.Float, Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
            new ParameterDescriptor("randomno", TYPE.Float, Usage.Local),
            new ParameterDescriptor("temp", TYPE.Vec2, Usage.Local, new float[] { 12.9898f, 78.233f })
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Range" },
            { "DisplayName", "Random Range" },
            { "Tooltip", "returns a psuedo-random value base on input Seed between min and max" },
            { "Parameters.Seed.Tooltip", " input Seed" },
            { "Parameters.Min.Tooltip", "minimum value" },
            { "Parameters.Max.Tooltip", "maximum value" },
            { "Parameters.Out.Tooltip", "a psuedo-random value between min and max" }
        };
    }
}
