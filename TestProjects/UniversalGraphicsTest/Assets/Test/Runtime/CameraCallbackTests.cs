using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

[CreateAssetMenu]
public class CameraCallbackTests : ScriptableRendererFeature
{
	static RenderTargetHandle beforeAll;
	static RenderTargetHandle afterOpaque;
	static RenderTargetHandle afterSkybox;
    static RenderTargetHandle afterSkybox2;
	static RenderTargetHandle afterTransparent;
	static RenderTargetHandle afterPost;
	static RenderTargetHandle afterAll;

    Material m_SamplingMaterial;

	public CameraCallbackTests()
	{
		beforeAll.Init("_BeforeAll");
		afterOpaque.Init("_AfterOpaque");
		afterSkybox.Init("_AfterSkybox");
	    afterSkybox2.Init("_AfterSkybox2");
		afterTransparent.Init("_AfterTransparent");
		afterPost.Init("_AfterPostProcessTexture");
		afterAll.Init("_AfterAll");
	}

	public override void Create()
    {
        ForwardRendererData data = null;
        if (UniversalRenderPipeline.asset.m_RendererDataList[0] != null)
		    data = UniversalRenderPipeline.asset.m_RendererDataList[0] as ForwardRendererData;

		if (data == null)
			return;

        if (data.shaders == null)
            return;

        if (data.shaders.samplingPS == null)
            return;

        m_SamplingMaterial = CoreUtils.CreateEngineMaterial(data.shaders.samplingPS);
	}

	internal class ClearColorPass : ScriptableRenderPass
    {
        RenderTargetIdentifier m_ColorHandle;

        public ClearColorPass(RenderPassEvent renderPassEvent, RenderTargetIdentifier colorHandle)
        {
            m_ColorHandle = colorHandle;
            this.renderPassEvent = renderPassEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("Clear Color");
            cmd.SetRenderTarget(m_ColorHandle);
            cmd.ClearRenderTarget(true, true, Color.yellow);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Downsampling downSamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;

        var cameraColorTarget = renderer.cameraColorTarget;
        var clearRenderPass = new ClearColorPass(RenderPassEvent.BeforeRenderingOpaques, cameraColorTarget);

        var copyBeforeOpaquePass = new CopyColorPass(RenderPassEvent.BeforeRenderingOpaques, m_SamplingMaterial);
        copyBeforeOpaquePass.Setup(cameraColorTarget, beforeAll, downSamplingMethod);

        var copyAfterOpaquePass = new CopyColorPass(RenderPassEvent.AfterRenderingOpaques, m_SamplingMaterial);
        copyAfterOpaquePass.Setup(cameraColorTarget, afterOpaque, downSamplingMethod);

        var copyAfterSkyboxPass = new CopyColorPass(RenderPassEvent.AfterRenderingSkybox, m_SamplingMaterial);
        copyAfterSkyboxPass.Setup(cameraColorTarget, afterSkybox, downSamplingMethod);

        var copyAfterSkyboxPass2 = new CopyColorPass(RenderPassEvent.AfterRenderingSkybox, m_SamplingMaterial);
        copyAfterSkyboxPass.Setup(cameraColorTarget, afterSkybox2, downSamplingMethod);

        var copyAfterTransparents = new CopyColorPass(RenderPassEvent.AfterRenderingTransparents, m_SamplingMaterial);
        copyAfterTransparents.Setup(cameraColorTarget, afterTransparent, downSamplingMethod);

        var copyAfterEverything = new CopyColorPass(RenderPassEvent.AfterRenderingPostProcessing, m_SamplingMaterial);
        copyAfterEverything.Setup(afterPost.id, afterAll, downSamplingMethod);

        var BlitRenderPassesToScreen = new BlitPass(RenderPassEvent.AfterRendering, cameraColorTarget);

        // Inserts out of order so we also test render passes sort correctly
        renderer.EnqueuePass(copyAfterEverything);
        renderer.EnqueuePass(BlitRenderPassesToScreen);
        renderer.EnqueuePass(copyAfterOpaquePass);
        renderer.EnqueuePass(copyAfterSkyboxPass);
        renderer.EnqueuePass(copyAfterSkyboxPass2);
        renderer.EnqueuePass(copyAfterTransparents);
        renderer.EnqueuePass(clearRenderPass);
        renderer.EnqueuePass(copyBeforeOpaquePass);
    }

	class BlitPass : ScriptableRenderPass
	{
        private RenderTargetIdentifier colorHandle;
	    Material m_BlitMaterial;

        public BlitPass(RenderPassEvent renderPassEvent, RenderTargetIdentifier colorHandle)
        {
            this.colorHandle = colorHandle;
            this.renderPassEvent = renderPassEvent;
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Universal Render Pipeline/Blit"));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var mesh = RenderingUtils.fullscreenMesh;
			CommandBuffer cmd = CommandBufferPool.Get("Blit Pass");
			cmd.SetRenderTarget(colorHandle);
			cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

			cmd.SetViewport(new Rect(0, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", beforeAll.Identifier());
		    cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterOpaque.Identifier());
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterSkybox.Identifier());
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

			cmd.SetViewport(new Rect(0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterSkybox2.Identifier());
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterTransparent.Identifier());
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

			cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
			cmd.SetGlobalTexture("_BlitTex", afterAll.Identifier());
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

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
