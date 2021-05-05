using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "CheckerboardSandbox")]
    class CheckerboardSandboxNode : SandboxNode<CheckerboardNodeDefinition>
    {
    }

    [Serializable]
    class CheckerboardNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("CheckerboardSandbox");

            if (shaderFunc == null)
                shaderFunc = BuildFunction();

            context.SetMainFunction(shaderFunc);
            context.SetPreviewFunction(shaderFunc);
        }

        // statically cached function definition
        static ShaderFunction shaderFunc = null;
        static ShaderFunction BuildFunction()
        {
            var func = new ShaderFunction.Builder("Unity_CheckerboardSB_$precision");
            func.AddInput(Types._float2,        "UV",          Binding.MeshUV0);
            func.AddInput(Types._precision3,    "ColorA",      new Vector3(0.2f, 0.2f, 0.2f));
            func.AddInput(Types._precision3,    "ColorB",      new Vector3(0.7f, 0.7f, 0.7f));
            func.AddInput(Types._precision2,    "Frequency",   new Vector2(1.0f, 1.0f));
            func.AddOutput(Types._precision3,   "Out");             // TODO:  ShaderStageCapability.Fragment   meta data?   scan body code for ddx/ddy/tfetch instructions?

            func.AddLine("UV = (UV.xy + 0.5) * Frequency;");
            func.AddLine("$precision4 derivatives = $precision4(ddx(UV), ddy(UV));");
            func.AddLine("$precision2 duv_length = sqrt($precision2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));");
            func.AddLine("$precision width = 1.0;");
            func.AddLine("$precision2 distance3 = 4.0 * abs(frac(UV + 0.25) - 0.5) - width;");
            func.AddLine("$precision2 scale = 0.35 / duv_length.xy;");
            func.AddLine("$precision freqLimiter = sqrt(clamp(1.1f - max(duv_length.x, duv_length.y), 0.0, 1.0));");
            func.AddLine("$precision2 vector_alpha = clamp(distance3 * scale.xy, -1.0, 1.0);");
            func.AddLine("$precision alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y * freqLimiter);");
            func.AddLine("Out = lerp(ColorA, ColorB, alpha.xxx);");

            return func.Build();
        }
    }
}
