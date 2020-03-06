using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GradientFogRendererFeature : ScriptableRendererFeature
{
    class GradientFogPass : ScriptableRenderPass
    {
        private Material fogMaterial;
        public GradientFogVolume volume;

        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle m_TempTex;

        public GradientFogPass()
        {
            m_TempTex.Init("_TempTex");
            fogMaterial = new Material(Shader.Find("Shader Graphs/GradientFogGraph"));
        }

        public void Setup(RenderTargetIdentifier cameraColorTarget)
        {
            source = cameraColorTarget;
            //GradientFogVolume volume = VolumeManager.instance.stack.GetComponent<GradientFogVolume>();
            fogMaterial.SetFloat("_StartDist", volume.nearDistance.value);
            fogMaterial.SetFloat("_EndDist", volume.farDistance.value);
            fogMaterial.SetColor("_NearCol", volume.nearColor.value);
            fogMaterial.SetColor("_MidCol", volume.midColor.value);
            fogMaterial.SetColor("_FarCol", volume.farColor.value);
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Gradient Fog");

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            
            opaqueDesc.depthBufferBits = 0;
            
            cmd.GetTemporaryRT(m_TempTex.id, opaqueDesc);
            
            
            Blit(cmd, source, m_TempTex.Identifier(), fogMaterial);
            Blit(cmd, m_TempTex.Identifier(), source);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_TempTex.id);
        }
    }

    GradientFogPass m_GradientFogPass;
    

    public override void Create()
    {
        m_GradientFogPass = new GradientFogPass();
        m_GradientFogPass.volume = VolumeManager.instance.stack.GetComponent<GradientFogVolume>();;

        // Configures where the render pass should be injected.
        m_GradientFogPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var src = renderer.cameraColorTarget;
        m_GradientFogPass.Setup(src);
        renderer.EnqueuePass(m_GradientFogPass);
    }
}


