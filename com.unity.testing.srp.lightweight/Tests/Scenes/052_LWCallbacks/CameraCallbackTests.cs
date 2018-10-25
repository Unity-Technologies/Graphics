using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

public class CameraCallbackTests : MonoBehaviour
	, IAfterDepthPrePass
	, IAfterOpaquePass
	, IAfterOpaquePostProcess
	, IAfterSkyboxPass
	, IAfterTransparentPass
	, IAfterRender
{
	
	static RenderTargetHandle afterDepth;
	static RenderTargetHandle afterOpaque;
	static RenderTargetHandle afterOpaquePost;
	static RenderTargetHandle afterSkybox;
	static RenderTargetHandle afterTransparent;
	static RenderTargetHandle afterAll;

	public CameraCallbackTests()
	{
		afterDepth.Init("_AfterDepth");
		afterOpaque.Init("_AfterOpaque");
		afterOpaquePost.Init("_AfterOpaquePost");
		afterSkybox.Init("_AfterSkybox");
		afterTransparent.Init("_AfterTransparent");
		afterAll.Init("_AfterAll");
	}
	
	
	ScriptableRenderPass IAfterDepthPrePass.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthAttachmentHandle)
	{
		var pass = new CopyDepthPass();
		pass.Setup(depthAttachmentHandle, afterDepth);
		return pass;
	}

	ScriptableRenderPass IAfterOpaquePass.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle,
		RenderTargetHandle depthAttachmentHandle)
	{
		var pass = new CopyColorPass();
		pass.Setup(colorAttachmentHandle, afterOpaque);
		return pass;
	}

	ScriptableRenderPass IAfterOpaquePostProcess.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle,
		RenderTargetHandle depthHandle)
	{
		var pass = new CopyColorPass();;
		pass.Setup(colorHandle, afterOpaquePost);
		return pass;
	}

	ScriptableRenderPass IAfterSkyboxPass.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle,
		RenderTargetHandle depthHandle)
	{
		var pass = new CopyColorPass();
		pass.Setup(colorHandle, afterSkybox);
		return pass;
	}

	ScriptableRenderPass IAfterTransparentPass.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle,
		RenderTargetHandle depthHandle)
	{
		var pass = new CopyColorPass();
		pass.Setup(colorHandle, afterTransparent);
		return pass;
	}

	class BlitPass : ScriptableRenderPass
	{
        private RenderTargetHandle colorHandle;
        private RenderTargetHandle depthHandle;

        public BlitPass(RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
        {
            this.colorHandle = colorHandle;
            this.depthHandle = colorHandle;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderer == null)
				throw new ArgumentNullException("renderer");

		    var pass = new CopyColorPass();
		    pass.Setup(colorHandle, afterAll);
            pass.Execute(renderer, context, ref renderingData);

            Material material = renderer.GetMaterial(MaterialHandle.Blit);

			CommandBuffer cmd = CommandBufferPool.Get("Blit Pass");
			cmd.SetRenderTarget(colorHandle.id, depthHandle.id);
			cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
			
			cmd.SetViewport(new Rect(0, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterDepth.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, material);
			
			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterOpaque.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, material);
			
			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterOpaquePost.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, material);			
						
			cmd.SetViewport(new Rect(0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterSkybox.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, material);
			
			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterTransparent.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, material);
			
			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterAll.Identifier());
		    ScriptableRenderer.RenderFullscreenQuad(cmd, material);

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

	ScriptableRenderPass IAfterRender.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle)
	{
		return new BlitPass(colorHandle, depthHandle);
	}
}
