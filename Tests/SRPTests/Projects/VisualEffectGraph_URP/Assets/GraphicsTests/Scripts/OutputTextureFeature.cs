using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutputTextureFeature : ScriptableRendererFeature
{
    public Shader shader;
    public ScriptableRenderPassInput inputRequirement;
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
    public int renderPassEventAdjustment = 0;

    private Material m_Material;
    private OutputTexturePass m_OutputTexturePassPass;


    /// <inheritdoc/>
    public override void Create()
    {
        m_OutputTexturePassPass = new OutputTexturePass(name);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (shader == null)
        {
            return;
        }

        if (m_Material == null)
        {
            m_Material = new Material(shader);
        }
        m_OutputTexturePassPass.renderPassEvent = renderPassEvent + renderPassEventAdjustment;
        m_OutputTexturePassPass.Setup(renderer, m_Material, inputRequirement, renderPassEvent, renderPassEventAdjustment);
        #pragma warning disable CS0618 // Type or member is obsolete
        renderer.EnqueuePass(m_OutputTexturePassPass);
        #pragma warning restore CS0618 // Type or member is obsolete
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }

    class OutputTexturePass : ScriptableRenderPass
    {
        private Material m_Material;
        private ScriptableRenderer m_Renderer;
        private ProfilingSampler m_ProfilingSampler;
        private PassData m_PassData;

        public OutputTexturePass(string profilerTag)
        {
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_PassData = new PassData();
        }

        public void Setup(ScriptableRenderer renderer, Material material, ScriptableRenderPassInput inputRequirement, RenderPassEvent renderPassEvent, int renderPassEventAdjustment)
        {
            m_Material = material;
            m_Renderer = renderer;
            ConfigureInput(inputRequirement);
        }

#if URP_COMPATIBILITY_MODE
        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            // SetRenderTarget has logic to flip projection matrix when rendering to render texture. Flip the uv to account for that case.
            bool yflip = renderingData.cameraData.IsCameraProjectionMatrixFlipped();

            CoreUtils.SetRenderTarget(cmd, m_Renderer.cameraColorTargetHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);

            m_PassData.profilingSampler = m_ProfilingSampler;
            m_PassData.material = m_Material;

            ExecutePass(m_PassData,CommandBufferHelpers.GetRasterCommandBuffer(cmd), yflip);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif

        internal class PassData
        {
            internal ProfilingSampler profilingSampler;
            internal Material material;

            // used only by RG
            internal UniversalCameraData cameraData;
            internal bool isTargetBackbuffer;
            internal TextureHandle colorTarget;
            internal TextureHandle depthTarget;

        }

        public static void ExecutePass(PassData passData, RasterCommandBuffer cmd, bool yFlip)
        {
            using (new ProfilingScope(cmd, passData.profilingSampler))
            {
                // SetRenderTarget has logic to flip projection matrix when rendering to render texture. Flip the uv to account for that case.
                float flipSign = yFlip ? -1.0f : 1.0f;
                Vector4 scaleBiasRt = (flipSign < 0.0f)
                    ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                    : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                cmd.SetGlobalVector(Shader.PropertyToID("_ScaleBiasRt"), scaleBiasRt);

                cmd.ClearRenderTarget(true, true, Color.white, 1.0f);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, passData.material, 0, 0);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Output Texture Pass", out var passData, m_ProfilingSampler))
            {
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowGlobalStateModification(true);

                passData.profilingSampler = m_ProfilingSampler;
                passData.material = m_Material;
                passData.cameraData = cameraData;

                passData.isTargetBackbuffer = resourceData.isActiveTargetBackBuffer;
                passData.colorTarget = resourceData.activeColorTexture;
                passData.depthTarget = resourceData.activeDepthTexture;

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    UniversalCameraData cameraData = data.cameraData;
                    bool yFlip = cameraData.IsRenderTargetProjectionMatrixFlipped(data.colorTarget, data.depthTarget);
                    ExecutePass(data, rgContext.cmd, yFlip);
                });
            }
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
}
