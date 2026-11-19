using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Universal.Tests
{
    // URP decides in C# whether it fills its light data into persistent constant buffers (RenderingUtils.usePersistentConstantBuffer),
    // and the shaders pick the matching layout in LightCookieInput.hlsl.
    // If the two disagree on a platform, URP uploads one layout while the shaders read another.
    // This test compiles Lit for each kind of platform and checks that its light cookie layout is the one
    // usePersistentConstantBuffer picks on that platform, which each test case states.
    class PersistentConstantBufferLayoutTests
    {
        const string k_LitShaderName = "Universal Render Pipeline/Lit";
        const string k_ForwardLitPassName = "ForwardLit";

        // The persistent layout stores the light types in the LightCookies constant buffer as float4 (yzw are padding).
        // The field-wise GLES3 layout declares them as float.
        const string k_LightCookiesBuffer = "LightCookies";
        const string k_LightTypesField = "_AdditionalLightsLightTypes";

        static readonly string[] k_Keywords = { ShaderKeywordStrings.AdditionalLightsPixel, ShaderKeywordStrings.LightCookies };

        // GLES links both stages into one program, whose parameters can be reported with either stage: check both.
        static readonly ShaderType[] k_Stages = { ShaderType.Vertex, ShaderType.Fragment };

        [TestCase(ShaderCompilerPlatform.GLES3x, BuildTarget.WebGL, true)]
        [TestCase(ShaderCompilerPlatform.GLES3x, BuildTarget.Android, false)]
        [TestCase(ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64, true)]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void LightCookieLayoutMatchesRenderingUtils(ShaderCompilerPlatform compilerPlatform, BuildTarget buildTarget, bool expectPersistent)
        {
            Shader shader = Shader.Find(k_LitShaderName);
            Assert.IsNotNull(shader, $"Could not find shader '{k_LitShaderName}'.");

            ShaderData.Pass pass = FindPass(ShaderUtil.GetShaderData(shader), k_ForwardLitPassName);
            Assert.IsNotNull(pass, $"Could not find pass '{k_ForwardLitPassName}' in '{k_LitShaderName}'.");

            bool foundField = false;
            bool persistentLayout = false;
            var reflected = new StringBuilder();
            foreach (var stage in k_Stages)
            {
                if (!pass.HasShaderStage(stage))
                    continue;

                ShaderData.VariantCompileInfo info = pass.CompileVariant(stage, k_Keywords, compilerPlatform, buildTarget);
                Assert.IsTrue(info.Success, $"Compiling the {stage} stage of '{k_ForwardLitPassName}' for {compilerPlatform} ({buildTarget}) failed:\n{FormatMessages(info.Messages)}");

                foreach (var constantBuffer in info.ConstantBuffers)
                {
                    reflected.Append($"  {stage} {constantBuffer.Name}:");
                    foreach (var field in constantBuffer.Fields)
                    {
                        reflected.Append($" {field.Name}");
                        if (!field.Name.StartsWith(k_LightTypesField))
                            continue;

                        foundField = true;
                        persistentLayout |= constantBuffer.Name == k_LightCookiesBuffer && field.Columns == 4;
                    }
                    reflected.AppendLine();
                }
            }

            // The field-wise layout can be loose uniforms, which the reflection may not report: only the persistent layout must be found.
            if (expectPersistent)
                Assert.IsTrue(foundField, $"{k_LightTypesField} not found when compiling for {compilerPlatform} ({buildTarget}). Constant buffers:\n{reflected}");

            Assert.AreEqual(expectPersistent, persistentLayout,
                $"For {compilerPlatform} ({buildTarget}), RenderingUtils.usePersistentConstantBuffer is {expectPersistent}, " +
                $"but the shaders use the {(persistentLayout ? "persistent" : "field-wise")} light cookie layout. " +
                $"Check the URP_LIGHT_COOKIE_PERSISTENT_CBUFFER condition in LightCookieInput.hlsl. Constant buffers:\n{reflected}");
        }

        static ShaderData.Pass FindPass(ShaderData shaderData, string passName)
        {
            for (int subshaderIndex = 0; subshaderIndex < shaderData.SerializedSubshaderCount; ++subshaderIndex)
            {
                ShaderData.Subshader subshader = shaderData.GetSerializedSubshader(subshaderIndex);
                for (int passIndex = 0; passIndex < subshader.PassCount; ++passIndex)
                {
                    ShaderData.Pass pass = subshader.GetPass(passIndex);
                    if (pass.Name == passName)
                        return pass;
                }
            }
            return null;
        }

        static string FormatMessages(ShaderMessage[] messages)
        {
            var builder = new StringBuilder();
            foreach (var message in messages)
                builder.AppendLine($"  {message.message}");
            return builder.ToString();
        }
    }
}
