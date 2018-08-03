using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class CopyColorPass : ScriptableRenderPass
    {
        float[] m_OpaqueScalerValues = {1.0f, 0.5f, 0.25f, 0.25f};
        int m_SampleOffsetShaderHandle;

        private RenderTargetHandle source { get; set; }
        private RenderTargetHandle destination { get; set; }
        
        public CopyColorPass()
        {
            m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
        }

        public void Setup(RenderTargetHandle source, RenderTargetHandle destination)
        {
            this.source = source;
            this.destination = destination;
        }

        public override void Execute(LightweightForwardRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            
            CommandBuffer cmd = CommandBufferPool.Get("Copy Color");
            Downsampling downsampling = renderingData.cameraData.opaqueTextureDownsampling;
            float opaqueScaler = m_OpaqueScalerValues[(int)downsampling];

            RenderTextureDescriptor opaqueDesc = LightweightForwardRenderer.CreateRTDesc(ref renderingData.cameraData, opaqueScaler);
            RenderTargetIdentifier colorRT = source.Identifier();
            RenderTargetIdentifier opaqueColorRT = destination.Identifier();

            cmd.GetTemporaryRT(destination.id, opaqueDesc, renderingData.cameraData.opaqueTextureDownsampling == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear);
            switch (downsampling)
            {
                case Downsampling.None:
                    cmd.Blit(colorRT, opaqueColorRT);
                    break;
                case Downsampling._2xBilinear:
                    cmd.Blit(colorRT, opaqueColorRT);
                    break;
                case Downsampling._4xBox:
                    Material samplingMaterial = renderer.GetMaterial(MaterialHandles.Sampling);
                    samplingMaterial.SetFloat(m_SampleOffsetShaderHandle, 2);
                    cmd.Blit(colorRT, opaqueColorRT, samplingMaterial, 0);
                    break;
                case Downsampling._4xBilinear:
                    cmd.Blit(colorRT, opaqueColorRT);
                    break;
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (destination != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(destination.id);
                destination = RenderTargetHandle.CameraTarget;
            }
        }
        
    }
}
