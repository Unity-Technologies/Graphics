namespace UnityEngine.Rendering.Universal
{
    public partial class UniversalRenderPipeline
    {
        struct ShaderIDs
        {
            // Adaptive Probe Volume
            public static readonly int _APVResIndex = Shader.PropertyToID("_APVResIndex");
            public static readonly int _APVResL0    = Shader.PropertyToID("_APVResL0");
            public static readonly int _APVResL1_R  = Shader.PropertyToID("_APVResL1_R");
            public static readonly int _APVResL1_G  = Shader.PropertyToID("_APVResL1_G");
            public static readonly int _APVResL1_B  = Shader.PropertyToID("_APVResL1_B");
        }

        private ComputeBuffer m_EmptyIndexBuffer = null;

        private void BindAPVRuntimeResources(CommandBuffer cmdBuffer)
        {
            bool needToBindNeutral = true;
            // Do this only if the framesetting is on, otherwise there is some hidden cost
            // TODO
            //if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
            {
                var refVolume = ProbeReferenceVolume.instance;
                ProbeReferenceVolume.RuntimeResources rr = refVolume.GetRuntimeResources();

                bool validResources = rr.index != null && rr.L0 != null && rr.L1_R != null && rr.L1_G != null && rr.L1_B != null;
                
                if (validResources)
                {
                    cmdBuffer.SetGlobalBuffer (ShaderIDs._APVResIndex, rr.index);
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL0, rr.L0);
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1_R, rr.L1_R);
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1_G, rr.L1_G);
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1_B, rr.L1_B);
                    needToBindNeutral = false;
                }
            }

            if (needToBindNeutral)
            {
                // Lazy init the empty buffer
                if (m_EmptyIndexBuffer == null)
                {
                    // Size doesn't really matter here, anything can be bound as long is a valid compute buffer.
                    m_EmptyIndexBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Structured);
                }

                cmdBuffer.SetGlobalBuffer (ShaderIDs._APVResIndex, m_EmptyIndexBuffer);
                cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL0, TextureXR.GetBlackTexture3D());
                cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1_R, TextureXR.GetBlackTexture3D());
                cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1_G, TextureXR.GetBlackTexture3D());
                cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1_B, TextureXR.GetBlackTexture3D());
            }
        }
    }
}
