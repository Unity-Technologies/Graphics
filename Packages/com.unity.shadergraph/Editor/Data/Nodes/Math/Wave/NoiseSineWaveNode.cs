using UnityEngine;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Wave", "Noise Sine Wave")]
    class NoiseSineWaveNode : CodeFunctionNode
    {
        public override int latestVersion => 1;

        public NoiseSineWaveNode()
        {
            name = "Noise Sine Wave";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            // Always make the latest version the default case, this gets around otherwise not returning in all codepaths.
            switch (sgVersion)
            {
                case 0:
                    return GetType().GetMethod("NoiseSineWave_V0", BindingFlags.Static | BindingFlags.NonPublic);

                case 1:
                default:
                    return GetType().GetMethod("NoiseSineWave_V1", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string NoiseSineWave_V0(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None, -0.5f, 0.5f, 1, 1)] Vector2 MinMax,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    $precision sinIn = sin(In);
    $precision sinInOffset = sin(In + 1.0);
    $precision randomno =  frac(sin((sinIn - sinInOffset) * (12.9898 + 78.233))*43758.5453);
    $precision noise = lerp(MinMax.x, MinMax.y, randomno);
    Out = sinIn + noise;
}
";
        }

        static string NoiseSineWave_V1(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None, -0.5f, 0.5f, 1, 1)] Vector2 MinMax,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    $precision{slot2dimension} sinIn = sin(In);
    $precision{slot2dimension} sinInOffset = sin(In + 1.0);
    $precision{slot2dimension} randomno =  frac(sin((sinIn - sinInOffset) * (12.9898 + 78.233))*43758.5453);
    $precision{slot2dimension} noise = lerp(MinMax.x, MinMax.y, randomno);
    Out = sinIn + noise;
}
";
        }
    }
}
