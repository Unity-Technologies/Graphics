using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Simple Noise")]
    class NoiseNode : CodeFunctionNode
    {
        public NoiseNode()
        {
            name = "Simple Noise";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SimpleNoise", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SimpleNoise(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 500f, 500f, 500f, 500f)] Vector1 Scale,
            [Slot(2, Binding.None)] out Vector1 Out)
        {
            return
@"
{
    $precision t = 0.0;

    $precision freq = pow(2.0, $precision(0));
    $precision amp = pow(0.5, $precision(3-0));
    t += Unity_SimpleNoise_ValueNoise_$precision($precision2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, $precision(1));
    amp = pow(0.5, $precision(3-1));
    t += Unity_SimpleNoise_ValueNoise_$precision($precision2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    freq = pow(2.0, $precision(2));
    amp = pow(0.5, $precision(3-2));
    t += Unity_SimpleNoise_ValueNoise_$precision($precision2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

    Out = t;
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction($"Unity_SimpleNoise_RandomValue_{concretePrecision.ToShaderString()}", s => s.Append(@"
inline $precision Unity_SimpleNoise_RandomValue_$precision ($precision2 uv)
{
    return frac(sin(dot(uv, $precision2(12.9898, 78.233)))*43758.5453);
}"));

            registry.ProvideFunction($"Unity_SimpleNnoise_Interpolate_{concretePrecision.ToShaderString()}", s => s.Append(@"
inline $precision Unity_SimpleNnoise_Interpolate_$precision ($precision a, $precision b, $precision t)
{
    return (1.0-t)*a + (t*b);
}
"));

            registry.ProvideFunction($"Unity_SimpleNoise_ValueNoise_{concretePrecision.ToShaderString()}", s => s.Append(@"
inline $precision Unity_SimpleNoise_ValueNoise_$precision ($precision2 uv)
{
    $precision2 i = floor(uv);
    $precision2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);

    uv = abs(frac(uv) - 0.5);
    $precision2 c0 = i + $precision2(0.0, 0.0);
    $precision2 c1 = i + $precision2(1.0, 0.0);
    $precision2 c2 = i + $precision2(0.0, 1.0);
    $precision2 c3 = i + $precision2(1.0, 1.0);
    $precision r0 = Unity_SimpleNoise_RandomValue_$precision(c0);
    $precision r1 = Unity_SimpleNoise_RandomValue_$precision(c1);
    $precision r2 = Unity_SimpleNoise_RandomValue_$precision(c2);
    $precision r3 = Unity_SimpleNoise_RandomValue_$precision(c3);

    $precision bottomOfGrid = Unity_SimpleNnoise_Interpolate_$precision(r0, r1, f.x);
    $precision topOfGrid = Unity_SimpleNnoise_Interpolate_$precision(r2, r3, f.x);
    $precision t = Unity_SimpleNnoise_Interpolate_$precision(bottomOfGrid, topOfGrid, f.y);
    return t;
}"));

            base.GenerateNodeFunction(registry, generationMode);
        }
    }
}
