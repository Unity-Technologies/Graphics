using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    enum TextureType
    {
        Default,
        Normal
    };

    // [FormerName("UnityEditor.ShaderGraph.Texture2DNode")]
    [Title("Input", "Texture", "Sample Texture 2D Sandbox")]
    class SampleTexture2DSandboxNode : SandboxNode<SampleTexture2DNodeDefinition>
    {
    }

    class SampleTexture2DNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("Sample Texture 2D Sandbox");

            // cached generic function
            if (shaderFunc == null)
                shaderFunc = BuildFunction();

            context.SetMainFunction(shaderFunc, declareStaticPins: true);
            context.SetPreviewFunction(shaderFunc);
        }

        // statically cached function definition
        static ShaderFunction shaderFunc = null;
        static ShaderFunction BuildFunction()
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
    }
}
