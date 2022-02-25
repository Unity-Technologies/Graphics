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
    sinIn = sin(In);
    Out = sinIn + lerp(Min, Max, frac(sin((sinIn - sin(In + 1.0)) * (12.9898 + 78.233))*43758.5453));
}
",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Min", TYPE.Float, Usage.In, new float[] { -0.5f }),
            new ParameterDescriptor("Max", TYPE.Float, Usage.In, new float[] { 0.5f }),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out),
            new ParameterDescriptor("sinIn", TYPE.Float, Usage.Local)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "DisplayName", "Noise Sine Wave" },
            { "Category", "Math, Wave" },
            { "Name.Synonyms", "wave, noise, sine" },
            { "Tooltip", "creates a sine wave with noise added to the amplitude for randomness" },
            { "Parameters.Min.Tooltip", "Minimum value for noise intensity" },
            { "Parameters.Max.Tooltip", "Maximum value for noise intensity" },
            { "Parameters.Out.Tooltip", "a sine wave with noise added to the amplitude for randomness" }
        };
    }
}
