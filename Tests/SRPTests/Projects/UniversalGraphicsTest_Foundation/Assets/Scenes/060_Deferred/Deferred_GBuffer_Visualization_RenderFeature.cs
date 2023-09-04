using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

public class Deferred_GBuffer_Visualization_RenderFeature : ScriptableRendererFeature
{
    public Shader visualizeShader;
    public bool visualizeAlphaChannel = false;
    private Material m_Material;

    class CustomRenderPass : ScriptableRenderPass
    {
        internal Material m_Material;
        internal bool m_VisualizeAlphaChannel = false;

        public CustomRenderPass(Shader visualizeShader, bool visualizeAlphaChannel)
        {
            m_VisualizeAlphaChannel = visualizeAlphaChannel;
        }

        public void Setup(Material material)
        {
            m_Material = material;
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

            ExecutePass(cmd, ref renderingData);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        void ExecutePass(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (m_Material == null)
                return;

            UniversalRenderer renderer = renderingData.cameraData.renderer as UniversalRenderer;

            ConfigureTarget(renderer?.cameraColorTargetHandle);
            ConfigureClear(ClearFlag.Color, Color.yellow);

            if (m_Material)
            {
                CoreUtils.SetKeyword(m_Material,"VIS_ALPHA", m_VisualizeAlphaChannel);
                cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Triangles, 3, 1);
            }
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(visualizeShader, visualizeAlphaChannel);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!GetMaterials())
        {
            Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
            return;
        }

        m_ScriptablePass.Setup(m_Material);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }

    private bool GetMaterials()
    {
        if (m_Material == null && visualizeShader != null)
            m_Material = CoreUtils.CreateEngineMaterial(visualizeShader);
        return m_Material != null;
    }
}
