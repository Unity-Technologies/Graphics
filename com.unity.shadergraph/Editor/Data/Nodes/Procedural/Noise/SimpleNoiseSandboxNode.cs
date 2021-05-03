using System.Reflection;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Simple Noise Sandbox")]
    class NoiseSandboxNode : SandboxNode<SimpleNoiseNodeDefinition>
    {
    }

    class SimpleNoiseNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("Simple Noise Sandbox");

            // cached generic function
            if (shaderFunc == null)
                shaderFunc = BuildUnityNoiseFunction();

            context.SetMainFunction(shaderFunc, declareStaticPins: true);
            context.SetPreviewFunction(shaderFunc);
        }

        // statically cached function definition
        static ShaderFunction shaderFunc = null;
        static ShaderFunction BuildUnityNoiseFunction()
        {
            var func = new ShaderFunction.Builder($"Unity_NoiseSB_$precision");
            func.AddInput(Types._precision2, "UV", Binding.MeshUV0);
            func.AddInput(Types._precision, "Scale", 500.0f);
            func.AddOutput(Types._precision, "Out");

            var valueNoise = BuildValueNoise();

            func.AddLine("Out = 0.0;");
            func.AddLine("$precision freq = pow(2.0, $precision(0));");
            func.AddLine("$precision amp = pow(0.5, $precision(3 - 0));");
            func.AddLine("$precision octave;");
            func.CallFunction(valueNoise, "UV.xy * (Scale / freq)", "octave");
            func.AddLine("Out += octave * amp;");
            func.AddLine("freq = pow(2.0, $precision(1));");
            func.AddLine("amp = pow(0.5, $precision(3 - 1));");
            func.CallFunction(valueNoise, "UV.xy * (Scale / freq)", "octave");
            func.AddLine("Out += octave * amp;");
            func.AddLine("freq = pow(2.0, $precision(2));");
            func.AddLine("amp = pow(0.5, $precision(3 - 2));");
            func.CallFunction(valueNoise, "UV.xy * (Scale / freq)", "octave");
            func.AddLine("Out += octave * amp;");

            return func.Build();
        }

        static ShaderFunction BuildValueNoise()
        {
            var func = new ShaderFunction.Builder($"Unity_NoiseSB_ValueNoise_$precision");
            func.AddInput(Types._precision2, "uv");
            func.AddOutput(Types._precision, "Out");

            func.AddLine("$precision2 i = floor(uv);");
            func.AddLine("$precision2 f = frac(uv);");

            func.AddLine("f = f * f * (3.0 - 2.0 * f);");
            func.AddLine("uv = abs(frac(uv) - 0.5);");

            func.AddLine("$precision2 c0 = i + $precision2(0.0, 0.0);");
            func.AddLine("$precision2 c1 = i + $precision2(1.0, 0.0);");
            func.AddLine("$precision2 c2 = i + $precision2(0.0, 1.0);");
            func.AddLine("$precision2 c3 = i + $precision2(1.0, 1.0);");

            var randFunc = BuildRandomValue();
            func.AddLine("$precision r0, r1, r2, r3;");
            func.CallFunction(randFunc, "c0", "r0");
            func.CallFunction(randFunc, "c1", "r1");
            func.CallFunction(randFunc, "c2", "r2");
            func.CallFunction(randFunc, "c3", "r3");

            var interpolateFunc = BuildInterpolate();

            func.AddLine("$precision bottomOfGrid, topOfGrid;");
            func.CallFunction(interpolateFunc, "r0", "r1", "f.x", "bottomOfGrid");
            func.CallFunction(interpolateFunc, "r2", "r3", "f.x", "topOfGrid");
            func.CallFunction(interpolateFunc, "bottomOfGrid", "topOfGrid", "f.y", "Out");

            return func.Build();
        }

        static ShaderFunction BuildRandomValue()
        {
            var func = new ShaderFunction.Builder($"Unity_NoiseSB_RandomValue_$precision");
            func.AddInput(Types._precision2, "uv");
            func.AddOutput(Types._precision, "Out");

            func.AddLine("$precision angle = dot(uv, $precision2(12.9898, 78.233));");

            // 'sin()' has bad precision on Mali GPUs for inputs > 10000
            func.AddLine("#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))");
            func.AddLine("angle = fmod(angle, TWO_PI);"); // Avoid large inputs to sin()
            func.AddLine("#endif");
            func.AddLine("Out = frac(sin(angle) * 43758.5453);");

            return func.Build();
        }

        static ShaderFunction BuildInterpolate()
        {
            var func = new ShaderFunction.Builder($"Unity_NoiseSB_Interpolate_$precision");
            func.AddInput(Types._precision, "a");
            func.AddInput(Types._precision, "b");
            func.AddInput(Types._precision, "t");
            func.AddOutput(Types._precision, "Out");
            func.AddLine("Out = (1.0 - t) * a + (t * b);");

            return func.Build();
        }
    }
}
