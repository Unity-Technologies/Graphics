using UnityEngine;
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
        renderer.EnqueuePass(m_OutputTexturePassPass);
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

        public OutputTexturePass(string profilerTag)
        {
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
        }

        public void Setup(ScriptableRenderer renderer, Material material, ScriptableRenderPassInput inputRequirement, RenderPassEvent renderPassEvent, int renderPassEventAdjustment)
        {
            m_Material = material;
            m_Renderer = renderer;
            ConfigureInput(inputRequirement);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.SetRenderTarget(m_Renderer.cameraColorTarget,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, // color
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                cmd.ClearRenderTarget(true, true, Color.white, 1.0f);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material, 0, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
}
