using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        class AfterPostProcessPassData
        {
            public ShaderVariablesGlobal    globalCB;
            public HDCamera                 hdCamera;
            public RendererListHandle       opaqueAfterPostprocessRL;
            public RendererListHandle       transparentAfterPostprocessRL;
        }

        TextureHandle RenderAfterPostProcessObjects(RenderGraph renderGraph, HDCamera hdCamera, CullingResults cullResults, in PrepassOutput prepassOutput)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess))
                return renderGraph.defaultResources.blackTextureXR;

            // We render AfterPostProcess objects first into a separate buffer that will be composited in the final post process pass
            using (var builder = renderGraph.AddRenderPass<AfterPostProcessPassData>("After Post-Process Objects", out var passData, ProfilingSampler.Get(HDProfileId.AfterPostProcessingObjects)))
            {
                bool useDepthBuffer = !hdCamera.IsTAAEnabled() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.ZTestAfterPostProcessTAA);

                passData.globalCB = m_ShaderVariablesGlobalCB;
                passData.hdCamera = hdCamera;
                passData.opaqueAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque)));
                passData.transparentAfterPostprocessRL = builder.UseRendererList(renderGraph.CreateRendererList(
                    CreateTransparentRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent)));

                var output = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_SRGB, clearBuffer = true, clearColor = Color.black, name = "OffScreen AfterPostProcess" }), 0);
                if (useDepthBuffer)
                    builder.UseDepthBuffer(prepassOutput.resolvedDepthBuffer, DepthAccess.ReadWrite);

                builder.SetRenderFunc(
                    (AfterPostProcessPassData data, RenderGraphContext ctx) =>
                    {
                        // Note about AfterPostProcess and TAA:
                        // When TAA is enabled rendering is jittered and then resolved during the post processing pass.
                        // It means that any rendering done after post processing need to disable jittering. This is what we do with hdCamera.UpdateViewConstants(false);
                        // The issue is that the only available depth buffer is jittered so pixels would wobble around depth tested edges.
                        // In order to avoid that we decide that objects rendered after Post processes while TAA is active will not benefit from the depth buffer so we disable it.
                        data.hdCamera.UpdateAllViewConstants(false);
                        data.hdCamera.UpdateShaderVariablesGlobalCB(ref data.globalCB);

                        UpdateOffscreenRenderingConstants(ref data.globalCB, true, 1);
                        ConstantBuffer.PushGlobal(ctx.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);

                        DrawOpaqueRendererList(ctx.renderContext, ctx.cmd, data.hdCamera.frameSettings, data.opaqueAfterPostprocessRL);
                        // Setup off-screen transparency here
                        DrawTransparentRendererList(ctx.renderContext, ctx.cmd, data.hdCamera.frameSettings, data.transparentAfterPostprocessRL);

                        UpdateOffscreenRenderingConstants(ref data.globalCB, false, 1);
                        ConstantBuffer.PushGlobal(ctx.cmd, data.globalCB, HDShaderIDs._ShaderVariablesGlobal);
                    });

                return output;
            }
        }

        struct PostProcessParameters
        {
            public HDCamera hdCamera;
            public bool postProcessIsFinalPass;
            public bool flipYInPostProcess;
            public BlueNoise blueNoise;
        }

        PostProcessParameters PreparePostProcess(CullingResults cullResults, HDCamera hdCamera)
        {
            PostProcessParameters result = new PostProcessParameters();
            result.hdCamera = hdCamera;
            result.postProcessIsFinalPass = HDUtils.PostProcessIsFinalPass(hdCamera);
            // Y-Flip needs to happen during the post process pass only if it's the final pass and is the regular game view
            // SceneView flip is handled by the editor internal code and GameView rendering into render textures should not be flipped in order to respect Unity texture coordinates convention
            result.flipYInPostProcess = result.postProcessIsFinalPass && (hdCamera.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || hdCamera.isMainGameView);
            result.blueNoise = m_BlueNoise;

            return result;
        }

        TextureHandle RenderPostProcess(RenderGraph     renderGraph,
            in PrepassOutput    prepassOutput,
            TextureHandle       inputColor,
            TextureHandle       backBuffer,
            CullingResults      cullResults,
            HDCamera            hdCamera)
        {
            TextureHandle afterPostProcessBuffer = RenderAfterPostProcessObjects(renderGraph, hdCamera, cullResults, prepassOutput);

            PostProcessParameters parameters = PreparePostProcess(cullResults, hdCamera);
            TextureHandle dest = HDUtils.PostProcessIsFinalPass(parameters.hdCamera) ? backBuffer : renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, false, true) { colorFormat = GetColorBufferFormat(), name = "Intermediate Postprocess buffer" });

            var motionVectors = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors) ? prepassOutput.resolvedMotionVectorsBuffer : renderGraph.defaultResources.blackTextureXR;
            m_PostProcessSystem.Render(
                renderGraph,
                parameters.hdCamera,
                parameters.blueNoise,
                inputColor,
                afterPostProcessBuffer,
                prepassOutput.resolvedDepthBuffer,
                prepassOutput.depthPyramidTexture,
                prepassOutput.resolvedNormalBuffer,
                motionVectors,
                dest,
                parameters.flipYInPostProcess
            );

            return dest;
        }
    }
}
