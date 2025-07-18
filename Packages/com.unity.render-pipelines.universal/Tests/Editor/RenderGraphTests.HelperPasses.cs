using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

#if UNITY_EDITOR
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.Tests
{
    internal partial class RenderGraphTests : RenderGraphTestsCore
    {
        static GraphicsFormat[] depthBlitTestFormats =
        {
            GraphicsFormat.D32_SFloat,
            GraphicsFormat.D24_UNorm,
            GraphicsFormat.D16_UNorm,
        };

        public enum MSAAState
        {
            MSAA_None,
            MSAA_4X,
        }

        static MSAAState[] depthBlitTestMSAA = { MSAAState.MSAA_None, MSAAState.MSAA_4X, };

        RTHandle CreateDepthTexture(int width, int height, float depthValue, GraphicsFormat depthFormat, bool msaa = false, bool force2D = false)
        {
            // Create a new RTHandle texture
            var depthHandle = RTHandles.Alloc(width, height,
                depthFormat,
                dimension: force2D ? TextureDimension.Tex2D : TextureXR.dimension,
                useMipMap: false,
                autoGenerateMips: false,
                msaaSamples: (msaa) ? MSAASamples.MSAA4x : MSAASamples.None,
                bindTextureMS: msaa,
                name: "Depth Texture 32 bit");

            var cmd = new CommandBuffer();
            cmd.name = "Clear Depth RT";
            cmd.SetRenderTarget(depthHandle);
            cmd.ClearRenderTarget(true, true, Color.black, depthValue);
            Graphics.ExecuteCommandBuffer(cmd);

            return depthHandle;
        }

        [Test]
        public void UtilityPasses_CopyDepth(
            [ValueSource(nameof(depthBlitTestFormats))] GraphicsFormat depthFormat,
            [ValueSource(nameof(depthBlitTestMSAA))] MSAAState msaa
            )
        {
            const int kWidth = 4;
            const int kHeight = 4;

            // Verify if the test is being executed in a non-URP project (e.g pipeline asset set to null)
            if (!EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<UniversalRenderPipelineRuntimeShaders, UniversalRenderPipeline>(out var shaders))
                return;

            if (Blitter.GetBlitMaterial(TextureDimension.Tex2D) == null && shaders != null)
            {
                Blitter.Initialize(shaders.coreBlitPS, shaders.coreBlitColorAndDepthPS);
            }

            float expectedDepthValue = 0.20f;
            float gpuDepthValue = expectedDepthValue;

            if (SystemInfo.usesReversedZBuffer)
                gpuDepthValue = 1 - expectedDepthValue;

            var sourceDepth = CreateDepthTexture(kWidth, kHeight, gpuDepthValue, depthFormat, msaa != MSAAState.MSAA_None);
            var destinationDepth = CreateDepthTexture(kWidth, kHeight, 0.0f, depthFormat);
            var depthAsColor = RTHandles.Alloc(kWidth, kHeight,
                GraphicsFormat.R32_SFloat,
                dimension: TextureXR.dimension,
                useMipMap: false,
                autoGenerateMips: false,
                name: "Depth As Color Texture 32 bit");

            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                // Initialize the RTHandle system if necessary
                RTHandles.Initialize(kWidth, kHeight);

                var source = m_RenderGraph.ImportTexture(sourceDepth);
                var intermediate = m_RenderGraph.ImportTexture(destinationDepth);
                var destination = m_RenderGraph.ImportTexture(depthAsColor);

                // First do a copy from depth to depth, moving the value of 0.25 depth to the intermediate buffer.
                m_RenderGraph.AddBlitPass(source, intermediate, Vector2.one, Vector2.zero);

                // Then we move the depth value from the intermediate buffer to a color buffer to allow reading back its data to the CPU.
                m_RenderGraph.AddBlitPass(intermediate, destination, Vector2.one, Vector2.zero);
            };

            RenderReadbackAndAssert(depthAsColor, expectedDepthValue);

            sourceDepth.Release();
            destinationDepth.Release();
            depthAsColor.Release();
            Blitter.Cleanup();
        }

        [Test]
        public void UtilityPasses_CopyDepthWithMaterial([ValueSource(nameof(depthBlitTestFormats))] GraphicsFormat depthFormat)
        {
            const int kWidth = 4;
            const int kHeight = 4;

            // Verify if the test is being executed in a non-URP project (e.g pipeline asset set to null)
            if (!EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<UniversalRenderPipelineRuntimeShaders, UniversalRenderPipeline>(out var shaders))
                return;

            if (Blitter.GetBlitMaterial(TextureDimension.Tex2D) == null && shaders != null)
            {
                Blitter.Initialize(shaders.coreBlitPS, shaders.coreBlitColorAndDepthPS);
            }

            float expectedDepthValue = 0.20f;
            float gpuDepthValue = expectedDepthValue;

            if (SystemInfo.usesReversedZBuffer)
                gpuDepthValue = 1 - expectedDepthValue;

            var sourceDepth = CreateDepthTexture(kWidth, kHeight, gpuDepthValue, depthFormat, false, true);
            var destinationDepth = CreateDepthTexture(kWidth, kHeight, 0.0f, depthFormat, false, true);
            var depthAsColor = RTHandles.Alloc(kWidth, kHeight,
                GraphicsFormat.R32_SFloat,
                dimension: TextureXR.dimension,
                useMipMap: false,
                autoGenerateMips: false,
                name: "Depth As Color Texture 32 bit");

            var shader = Shader.Find("Hidden/Core/CustomDepthBlit");
            Assert.IsNotNull(shader);

            var customDepthToColorMaterial = new Material(shader);

            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                // Initialize the RTHandle system if necessary
                RTHandles.Initialize(kWidth, kHeight);

                var source = m_RenderGraph.ImportTexture(sourceDepth);
                var intermediate = m_RenderGraph.ImportTexture(destinationDepth);
                var destination = m_RenderGraph.ImportTexture(depthAsColor);

                // First do a copy from depth to depth, moving the value of 0.25 depth to the intermediate buffer.
                m_RenderGraph.AddBlitPass(new BlitMaterialParameters(source, intermediate, customDepthToColorMaterial, 0));
                m_RenderGraph.AddBlitPass(new BlitMaterialParameters(intermediate, destination, customDepthToColorMaterial, 1));
            };

            RenderReadbackAndAssert(depthAsColor, expectedDepthValue);

            CoreUtils.Destroy(customDepthToColorMaterial);
            sourceDepth.Release();
            depthAsColor.Release();
            Blitter.Cleanup();
        }

        void RenderReadbackAndAssert(RTHandle colorBuffer, float expectedDepthValue)
        {
            ExternalGPUProfiler.BeginGPUCapture();

            m_Camera.Render();

            NativeArray<float> data = default;

            AsyncGPUReadback.Request(colorBuffer, 0, (res) =>
            {
                Assert.That(!res.hasError, "GPU readback failed");
                Assert.That(res.done, "GPU readback not done");
                data = res.GetData<float>();
            });

            AsyncGPUReadback.WaitAllRequests();
            ExternalGPUProfiler.EndGPUCapture();

            Assert.That(data.Length, Is.EqualTo(colorBuffer.rt.width * colorBuffer.rt.height));

            for (int i = 0; i < colorBuffer.rt.width * colorBuffer.rt.height; i++)
            {
                const float tolerance = 0.02f;
                float e = Mathf.Abs(data[i] - expectedDepthValue);
                Assert.True(e < tolerance, $"Depth Data does not match: actual = {data[i]}, expected = {expectedDepthValue}, error = {e}, tolerance = {tolerance}");
            }

            data.Dispose();
        }
    }
}
