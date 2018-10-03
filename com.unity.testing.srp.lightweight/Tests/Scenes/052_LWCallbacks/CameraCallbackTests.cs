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

	private class CapturePass : ScriptableRenderPass
	{
		private RenderTargetHandle m_Source;
		private RenderTargetHandle m_Target;

		public void Setup(RenderTargetHandle source, RenderTargetHandle target)
		{
			m_Source = source;
			m_Target = target;
		}

		public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderer == null)
				throw new ArgumentNullException("renderer");
			
			RenderTextureDescriptor opaqueDesc = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);

			var cmd = CommandBufferPool.Get("Capture Pass");
			cmd.GetTemporaryRT(m_Target.id, opaqueDesc);
			cmd.Blit(m_Source.Identifier(), m_Target.Identifier());
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
		
		public override void FrameCleanup(CommandBuffer cmd)
		{
			if (cmd == null)
				throw new ArgumentNullException("cmd");
			
			if (m_Target != RenderTargetHandle.CameraTarget)
			{
				cmd.ReleaseTemporaryRT(m_Target.id);
				m_Target = RenderTargetHandle.CameraTarget;
			}
		}
	}

	class BlitPass : ScriptableRenderPass
	{
		CapturePass m_CopyResult = new CapturePass();
		
		public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderer == null)
				throw new ArgumentNullException("renderer");
			
			m_CopyResult.Setup(RenderTargetHandle.CameraTarget, afterAll);
			m_CopyResult.Execute(renderer, context, ref renderingData);
			
			Material material = renderer.GetMaterial(MaterialHandle.Blit);

			CommandBuffer cmd = CommandBufferPool.Get("Blit Pass");
			cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
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
			
			
			//TODO: Upsidown UV trash, ignore this for now
			// Need to flip UV as we come from a framebuffer.
			/*cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterAll.Identifier());
			cmd.EnableShaderKeyword("FLIP_VERTICAL_UV");
			LightweightPipeline.DrawFullScreen(cmd, material);
			cmd.DisableShaderKeyword("FLIP_VERTICAL_UV");*/
			
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);			
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			if (cmd == null)
				throw new ArgumentNullException("cmd");
			
			base.FrameCleanup(cmd);
			m_CopyResult.FrameCleanup(cmd);
			
		}
	}

	ScriptableRenderPass IAfterRender.GetPassToEnqueue()
	{
		
		
		return new BlitPass();
	}
}
