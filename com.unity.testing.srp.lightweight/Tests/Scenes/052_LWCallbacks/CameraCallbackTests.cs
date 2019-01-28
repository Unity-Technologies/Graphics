using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

[CreateAssetMenu]
public class CameraCallbackTests : RenderPassFeature
{
	static RenderTargetHandle beforeAll;
	static RenderTargetHandle afterOpaque;
	static RenderTargetHandle afterOpaquePost;
	static RenderTargetHandle afterSkybox;
	static RenderTargetHandle afterTransparent;
	static RenderTargetHandle afterAll;

    Material m_CopyDepthMaterial;
    Material m_SamplingMaterial;
    
	public CameraCallbackTests()
	{
		beforeAll.Init("_BeforeAll");
		afterOpaque.Init("_AfterOpaque");
		afterOpaquePost.Init("_AfterOpaquePost");
		afterSkybox.Init("_AfterSkybox");
		afterTransparent.Init("_AfterTransparent");
		afterAll.Init("_AfterAll");
	}

	private void OnEnable()
	{
		m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Lightweight Render Pipeline/CopyDepth"));
		m_SamplingMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Lightweight Render Pipeline/Sampling"));
	}

	internal class ClearColorPass : ScriptableRenderPass
    {
        RenderTargetHandle m_ColorHandle;
        ClearFlag m_ClearFlag;

        public ClearColorPass(RenderTargetHandle colorHandle, ClearFlag clearFlag)
        {
            m_ColorHandle = colorHandle;
            m_ClearFlag = clearFlag;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("Clear Color");
            cmd.SetRenderTarget(m_ColorHandle.Identifier());
            cmd.ClearRenderTarget(CoreUtils.HasFlag(m_ClearFlag, ClearFlag.Color), CoreUtils.HasFlag(m_ClearFlag, ClearFlag.Depth), Color.yellow);

            RenderTextureDescriptor opaqueDesc = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            cmd.GetTemporaryRT(beforeAll.id, opaqueDesc, FilterMode.Point);
            cmd.Blit(m_ColorHandle.Identifier(), beforeAll.Identifier());
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public override InjectionPoint injectionPoints =>
	    InjectionPoint.BeforeRenderPasses
	    | InjectionPoint.AfterOpaqueRenderPasses
	    | InjectionPoint.AfterOpaquePostProcessPasses
	    | InjectionPoint.AfterSkyboxPasses
	    | InjectionPoint.AfterTransparentPasses
	    | InjectionPoint.AfterRenderPasses;

    public override ScriptableRenderPass GetPassToEnqueue(
	    InjectionPoint injection, 
	    RenderTextureDescriptor baseDescriptor, 
	    RenderTargetHandle colorAttachmentHandle, 
	    RenderTargetHandle depthAttachmentHandle)
    {
	    CopyColorPass pass;
	    switch (injection)
		{
			case InjectionPoint.BeforeRenderPasses:
				return new ClearColorPass(colorAttachmentHandle, ClearFlag.Color);
			case InjectionPoint.AfterOpaqueRenderPasses:
				pass = new CopyColorPass(m_SamplingMaterial);
				pass.Setup(colorAttachmentHandle, afterOpaque);
				return pass;
			case InjectionPoint.AfterOpaquePostProcessPasses:
				pass = new CopyColorPass(m_SamplingMaterial);;
				pass.Setup(colorAttachmentHandle, afterOpaquePost);
				return pass;
			case InjectionPoint.AfterSkyboxPasses:
				pass = new CopyColorPass(m_SamplingMaterial);
				pass.Setup(colorAttachmentHandle, afterSkybox);
				return pass;
			case InjectionPoint.AfterTransparentPasses:
				pass = new CopyColorPass(m_SamplingMaterial);
				pass.Setup(colorAttachmentHandle, afterTransparent);
				return pass;
			case InjectionPoint.AfterRenderPasses:
				return new BlitPass(colorAttachmentHandle, depthAttachmentHandle);
			default:
				throw new ArgumentOutOfRangeException(nameof(injection), injection, null);
		}
    }

	class BlitPass : ScriptableRenderPass
	{
        private RenderTargetHandle colorHandle;
        private RenderTargetHandle depthHandle;
	    Material m_BlitMaterial;

        public BlitPass(RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
        {
            this.colorHandle = colorHandle;
            this.depthHandle = colorHandle;
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Lightweight Render Pipeline/Blit"));
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderer == null)
				throw new ArgumentNullException("renderer");

		    var pass = new CopyColorPass(CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Lightweight Render Pipeline/Sampling")));
		    pass.Setup(colorHandle, afterAll);
            pass.Execute(renderer, context, ref renderingData);

			CommandBuffer cmd = CommandBufferPool.Get("Blit Pass");
			cmd.SetRenderTarget(colorHandle.id, depthHandle.id);
			cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
			
			cmd.SetViewport(new Rect(0, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", beforeAll.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, m_BlitMaterial);
			
			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterOpaque.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, m_BlitMaterial);
			
			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterOpaquePost.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, m_BlitMaterial);			
						
			cmd.SetViewport(new Rect(0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterSkybox.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, m_BlitMaterial);
			
			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterTransparent.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, m_BlitMaterial);
			
			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterAll.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, m_BlitMaterial);

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
