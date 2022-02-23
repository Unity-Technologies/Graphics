using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class NoiseSineWaveNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "NoiseSineWave", // Name
            @"
{
    float sinIn = sin(In);
    float sinInOffset = sin(In + 1.0);
    float randomno =  frac(sin((sinIn - sinInOffset) * (12.9898 + 78.233))*43758.5453);
    float noise = lerp(MinMax.x, MinMax.y, randomno);
    Out = sinIn + noise;
}
",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("MinMax", TYPE.Vec2, Usage.In, new float[] { -0.5f, 0.5f }),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Wave" },
            { "Name.Synonyms", "wave, noise, sine" },
            { "Tooltip", "creates a sine wave with noise added to the amplitude for randomness" },
            { "Parameters.MinMax.Tooltip", "Minimum and Maximum values for noise intensity" },
            { "Parameters.Out.Tooltip", "a sine wave with noise added to the amplitude for randomness" }
        };
    }
}
