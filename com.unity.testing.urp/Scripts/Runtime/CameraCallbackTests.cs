using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

[CreateAssetMenu]
public class CameraCallbackTests : ScriptableRendererFeature
{
    static readonly RTHandle k_CameraTarget = RTHandles.Alloc(BuiltinRenderTextureType.CameraTarget);

    static RTHandle beforeAll;
    static RTHandle afterOpaque;
    static RTHandle afterSkybox;
    static RTHandle afterSkybox2;
    static RTHandle afterTransparent;
    static RTHandle afterPost;
    static RTHandle afterAll;

    BlitPass m_BlitRenderPassesToScreen;
    Material m_SamplingMaterial;

    public CameraCallbackTests()
    {
        beforeAll = RTHandles.Alloc(Shader.PropertyToID("_BeforeAll"), "_BeforeAll");
        afterOpaque = RTHandles.Alloc(Shader.PropertyToID("_AfterOpaque"), "_AfterOpaque");
        afterSkybox = RTHandles.Alloc(Shader.PropertyToID("_AfterSkybox"), "_AfterSkybox");
        afterSkybox2 = RTHandles.Alloc(Shader.PropertyToID("_AfterSkybox2"), "_AfterSkybox2");
        afterTransparent = RTHandles.Alloc(Shader.PropertyToID("_AfterTransparent"), "_AfterTransparent");
        afterPost = RTHandles.Alloc(Shader.PropertyToID("_AfterPostProcessTexture"), "_AfterPostProcessTexture");
        afterAll = RTHandles.Alloc(Shader.PropertyToID("_AfterAll"), "_AfterAll");
    }

    public override void Create()
    {
        UniversalRendererData data = null;
        if (UniversalRenderPipeline.asset.m_RendererDataList[0] != null)
            data = UniversalRenderPipeline.asset.m_RendererDataList[0] as UniversalRendererData;

        if (data == null)
            return;

        if (data.shaders == null)
            return;

        if (data.shaders.samplingPS == null)
            return;

        if (m_SamplingMaterial == null)
            m_SamplingMaterial = CoreUtils.CreateEngineMaterial(data.shaders.samplingPS);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Downsampling downSamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;

        var cameraColorTarget = k_CameraTarget;
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
        copyAfterEverything.Setup(afterPost, afterAll, downSamplingMethod);

        if (m_BlitRenderPassesToScreen == null)
            m_BlitRenderPassesToScreen = new BlitPass(RenderPassEvent.AfterRendering, cameraColorTarget);

        // Inserts out of order so we also test render passes sort correctly
        renderer.EnqueuePass(copyAfterEverything);
        renderer.EnqueuePass(m_BlitRenderPassesToScreen);
        renderer.EnqueuePass(copyAfterOpaquePass);
        renderer.EnqueuePass(copyAfterSkyboxPass);
        renderer.EnqueuePass(copyAfterSkyboxPass2);
        renderer.EnqueuePass(copyAfterTransparents);
        renderer.EnqueuePass(clearRenderPass);
        renderer.EnqueuePass(copyBeforeOpaquePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_SamplingMaterial);
        m_BlitRenderPassesToScreen?.Cleanup();
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
            cmd.SetGlobalTexture("_BlitTex", beforeAll);
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

            cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
            cmd.SetGlobalTexture("_BlitTex", afterOpaque);
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

            cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
            cmd.SetGlobalTexture("_BlitTex", afterSkybox);
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

            cmd.SetViewport(new Rect(0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
            cmd.SetGlobalTexture("_BlitTex", afterSkybox2);
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

            cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
            cmd.SetGlobalTexture("_BlitTex", afterTransparent);
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

            cmd.SetViewport(new Rect(renderingData.cameraData.camera.pixelRect.width / 3.0f * 2.0f, 0f, renderingData.cameraData.camera.pixelRect.width / 3.0f, renderingData.cameraData.camera.pixelRect.height / 2.0f));
            cmd.SetGlobalTexture("_BlitTex", afterAll);
            cmd.DrawMesh(mesh, Matrix4x4.identity, m_BlitMaterial);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            base.OnCameraCleanup(cmd);
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_BlitMaterial);
        }
    }
}
