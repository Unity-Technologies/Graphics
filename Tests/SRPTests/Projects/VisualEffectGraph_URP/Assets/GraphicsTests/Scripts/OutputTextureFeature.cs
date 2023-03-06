using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
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
        private PassData m_PassData;
        private bool m_ForceYFlip;

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
            m_ForceYFlip = (inputRequirement == ScriptableRenderPassInput.Normal); //Workaround for 21.3
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

            // SetRenderTarget has logic to flip projection matrix when rendering to render texture. Flip the uv to account for that case.
            CameraData cameraData = renderingData.cameraData;
            bool isGameViewFinalTarget = (cameraData.cameraType == CameraType.Game && m_Renderer.cameraColorTarget == BuiltinRenderTextureType.CameraTarget);
            bool yflip = (cameraData.IsCameraProjectionMatrixFlipped()) && !isGameViewFinalTarget;
            if (m_ForceYFlip)
                yflip = !yflip;

            cmd.SetRenderTarget(m_Renderer.cameraColorTarget,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, // color
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth

            m_PassData.profilingSampler = m_ProfilingSampler;
            m_PassData.material = m_Material;

            ExecutePass(m_PassData, context, cmd, yflip);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        internal class PassData
        {
            internal ProfilingSampler profilingSampler;
            internal Material material;

            // used only by RG
            internal CameraData cameraData;
            internal bool isTargetBackbuffer;
            internal TextureHandle colorTarget;
            internal TextureHandle depthTarget;

        }

        public static void ExecutePass(PassData passData, ScriptableRenderContext context, CommandBuffer cmd, bool yFlip)
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

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
}
