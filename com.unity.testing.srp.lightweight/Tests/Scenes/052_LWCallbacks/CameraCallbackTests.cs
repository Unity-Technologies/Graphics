using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering;

[CreateAssetMenu]
public class CameraCallbackTests : RenderPassFeature
{
	static RenderTargetHandle beforeAll;
	static RenderTargetHandle afterOpaque;
	static RenderTargetHandle afterSkybox;
	static RenderTargetHandle afterTransparent;
	static RenderTargetHandle afterAll;

    Material m_SamplingMaterial;
    
	public CameraCallbackTests()
	{
		beforeAll.Init("_BeforeAll");
		afterOpaque.Init("_AfterOpaque");
		afterSkybox.Init("_AfterSkybox");
		afterTransparent.Init("_AfterTransparent");
		afterAll.Init("_AfterAll");
	}

	private void OnEnable()
	{
		m_SamplingMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Lightweight Render Pipeline/Sampling"));
	}

	internal class ClearColorPass : ScriptableRenderPass
    {
        RenderTargetHandle m_ColorHandle;

        public ClearColorPass(RenderPassEvent renderPassEvent, RenderTargetHandle colorHandle)
        {
            m_ColorHandle = colorHandle;
            this.renderPassEvent = renderPassEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("Clear Color");
            cmd.SetRenderTarget(m_ColorHandle.Identifier());
            cmd.ClearRenderTarget(true, true, Color.yellow);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
    public override void AddRenderPasses(List<ScriptableRenderPass> renderPasses,
        RenderTextureDescriptor baseDescriptor,
        RenderTargetHandle colorAttachmentHandle,
        RenderTargetHandle depthAttachmentHandle)
    {
        var clearRenderPass = new ClearColorPass(RenderPassEvent.BeforeRenderingOpaques, colorAttachmentHandle);

        var copyBeforeOpaquePass = new CopyColorPass(m_SamplingMaterial);
        copyBeforeOpaquePass.Setup(colorAttachmentHandle, beforeAll);
        copyBeforeOpaquePass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

        var copyAfterOpaquePass = new CopyColorPass(m_SamplingMaterial);
        copyAfterOpaquePass.Setup(colorAttachmentHandle, afterOpaque);
        copyAfterOpaquePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        var copyAfterSkyboxPass = new CopyColorPass(m_SamplingMaterial);
        copyAfterSkyboxPass.Setup(colorAttachmentHandle, afterSkybox);
        copyAfterSkyboxPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        var copyAfterTransparents = new CopyColorPass(m_SamplingMaterial);
        copyAfterTransparents.Setup(colorAttachmentHandle, afterTransparent);
        copyAfterTransparents.renderPassEvent = RenderPassEvent.AfterRenderingTransparentPasses;

        var copyAfterEverything = new CopyColorPass(m_SamplingMaterial);
        copyAfterEverything.Setup(colorAttachmentHandle, afterAll);
        copyAfterEverything.renderPassEvent = RenderPassEvent.AfterRendering;

        var BlitRenderPassesToScreen = new BlitPass(RenderPassEvent.AfterRendering, colorAttachmentHandle);

        // Inserts out of order so we also test render passes sort correctly
        renderPasses.Add(copyAfterEverything);
        renderPasses.Add(BlitRenderPassesToScreen);
        renderPasses.Add(copyAfterOpaquePass);
        renderPasses.Add(copyAfterSkyboxPass);
        renderPasses.Add(copyAfterTransparents);
        renderPasses.Add(clearRenderPass);
        renderPasses.Add(copyBeforeOpaquePass);
    }

	class BlitPass : ScriptableRenderPass
	{
        private RenderTargetHandle colorHandle;
        private RenderTargetHandle depthHandle;
	    Material m_BlitMaterial;

        public BlitPass(RenderPassEvent renderPassEvent, RenderTargetHandle colorHandle)
        {
            this.colorHandle = colorHandle;
            this.depthHandle = colorHandle;
            this.renderPassEvent = renderPassEvent;
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Lightweight Render Pipeline/Blit"));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer cmd = CommandBufferPool.Get("Blit Pass");
			cmd.SetRenderTarget(colorHandle.id, depthHandle.id);
			cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

			cmd.SetViewport(new Rect(0, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", beforeAll.Identifier());
		    RenderFullscreenQuad(cmd, m_BlitMaterial);

			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterOpaque.Identifier());
		    RenderFullscreenQuad(cmd, m_BlitMaterial);

			cmd.SetViewport(new Rect(0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterSkybox.Identifier());
		    RenderFullscreenQuad(cmd, m_BlitMaterial);

			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterTransparent.Identifier());
		    RenderFullscreenQuad(cmd, m_BlitMaterial);

			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterAll.Identifier());
		    RenderFullscreenQuad(cmd, m_BlitMaterial);

            context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			if (cmd == null)
				throw new ArgumentNullException("cmd");

			base.FrameCleanup(cmd);
		}
	}
}
