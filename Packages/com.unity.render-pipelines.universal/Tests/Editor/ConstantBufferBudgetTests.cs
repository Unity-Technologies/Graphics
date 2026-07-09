using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Universal.Tests
{
    // Devices allow a maximum number of constant buffers bound per shader stage.
    // If a URP shader variant exceeds that budget it will fail to run,
    // this test compile the worst-case variant of the shader assumely most at risk (Lit) and fails if the budget is blown.
    class ConstantBufferBudgetTests
    {
        // Maximum number of constant buffers that can be bound per shader stage on the most
        // constrained Android mobile devices we support (see maxPerStageDescriptorUniformBuffers for Vulkan).
        const int k_MaxConstantBuffersPerStage = 12;

        const string k_LitShaderName = "Universal Render Pipeline/Lit";

        // Unity shader D3D compiler provides metadata at shader stage granularity, Vulkan doesn't.
        const ShaderCompilerPlatform k_CompilerPlatform = ShaderCompilerPlatform.D3D;

        // We use Standalone Win as a build target for simplicity, it should not affect the number of CBs used.
        const BuildTarget k_BuildTarget = BuildTarget.StandaloneWindows64;

        static readonly ShaderType[] k_Stages = { ShaderType.Vertex, ShaderType.Fragment };

        // Manually selecting keywords in order to try to maximize constant buffer budget.
        // This not perfect and might miss some costly variants,
        // but we lack a shader API to get the list of all keywords that can coexist,
        // and even then it would be too costly to compile all of the variants.
        // Also, we purposefully don't use any XR keyword (for i.e. STEREO_INSTANCING_ON), as it would increase the count of CB by 1
        // but XR devices have a higher max CB count around 15.
        static readonly string[] k_Keywords = {
            ShaderKeywordStrings.MainLightShadowCascades,
            ShaderKeywordStrings.AdditionalLightsPixel,
            ShaderKeywordStrings.LightLayers,
            ShaderKeywordStrings.ClusterLightLoop,
            ShaderKeywordStrings.ShadowsShadowMask,
            ShaderKeywordStrings.LIGHTMAP_ON,
            ShaderKeywordStrings.AdditionalLightShadows,
            ShaderKeywordStrings.ScreenSpaceReflection,
            ShaderKeywordStrings.ScreenSpaceOcclusion,
            ShaderKeywordStrings.DBufferMRT3,
            ShaderKeywordStrings.LightCookies,
            ShaderKeywordStrings.WriteRenderingLayers,
            ShaderKeywordStrings.DEBUG_DISPLAY,
            ShaderKeywordStrings._ALPHATEST_ON,
            ShaderKeywordStrings.ReflectionProbeBlending,
            ShaderKeywordStrings.ReflectionProbeAtlas,
            "DOTS_INSTANCING_ON", // engine keyword - 4 CBs
            "INSTANCING_ON" }; // engine keyword

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void LitShaderWorstCaseVariantStaysWithinConstantBufferBudget()
        {
            Shader shader = Shader.Find(k_LitShaderName);
            Assert.IsNotNull(shader, $"Could not find shader '{k_LitShaderName}'.");

            ShaderData shaderData = ShaderUtil.GetShaderData(shader);
            Assert.IsNotNull(shaderData, $"Could not get ShaderData for '{k_LitShaderName}'.");

            TestContext.WriteLine($"Keywords enabled for every variant ({k_Keywords.Length}): {FormatKeywords(k_Keywords)}");

            var budgetViolations = new List<string>();
            int worstCount = 0;

            for (int subshaderIndex = 0; subshaderIndex < shaderData.SerializedSubshaderCount; ++subshaderIndex)
            {
                ShaderData.Subshader subshader = shaderData.GetSerializedSubshader(subshaderIndex);

                for (int passIndex = 0; passIndex < subshader.PassCount; ++passIndex)
                {
                    ShaderData.Pass pass = subshader.GetPass(passIndex);

                    foreach (var stage in k_Stages)
                    {
                        if (!pass.HasShaderStage(stage))
                            continue;

                        ShaderData.VariantCompileInfo info = pass.CompileVariant(stage, k_Keywords, k_CompilerPlatform, k_BuildTarget);

                        string location = $"subshader {subshaderIndex}, pass {passIndex} ('{pass.Name}'), {stage} stage";

                        if (!info.Success)
                        {
                            budgetViolations.Add($"{location}: compilation failed:\n{FormatMessages(info.Messages)}");
                            continue;
                        }

                        int count = info.ConstantBuffers.Length;
                        worstCount = Mathf.Max(worstCount, count);

                        TestContext.WriteLine(
                            $"{location}: {count} constant buffers\n" +
                            $"  Constant buffers:\n{FormatConstantBuffers(info.ConstantBuffers)}");

                        if (count > k_MaxConstantBuffersPerStage)
                        {
                            budgetViolations.Add(
                                $"{location}: {count} constant buffers (budget is {k_MaxConstantBuffersPerStage}):\n" +
                                FormatConstantBuffers(info.ConstantBuffers));
                        }
                    }
                }
            }

            TestContext.WriteLine(
                $"Worst-case constant buffer count for '{k_LitShaderName}' was {worstCount} " +
                $"(budget is {k_MaxConstantBuffersPerStage}).");

            Assert.IsEmpty(budgetViolations,
                $"'{k_LitShaderName}' exceeds the {k_MaxConstantBuffersPerStage} constant buffers/stage budget:\n" +
                string.Join("\n\n", budgetViolations));
        }

        static string FormatKeywords(string[] keywords)
        {
            return keywords.Length == 0 ? "<none>" : string.Join(", ", keywords);
        }

        static string FormatConstantBuffers(ShaderData.ConstantBufferInfo[] constantBuffers)
        {
            var builder = new StringBuilder();

            foreach (var constantBuffer in constantBuffers)
                builder.AppendLine($"  {constantBuffer.Name} ({constantBuffer.Size} bytes)");

            return builder.ToString();
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
