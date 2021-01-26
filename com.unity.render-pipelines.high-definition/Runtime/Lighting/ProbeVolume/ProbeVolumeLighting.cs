namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        private ComputeBuffer m_EmptyIndexBuffer = null;

        private void BindAPVRuntimeResources(CommandBuffer cmdBuffer, HDCamera hdCamera)
        {
            bool needToBindNeutral = true;
            // Do this only if the framesetting is on, otherwise there is some hidden cost
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
            {
                var refVolume = ProbeReferenceVolume.instance;
                ProbeReferenceVolume.RuntimeResources rr = refVolume.GetRuntimeResources();

                bool validResources = rr.index != null && rr.L0 != null && rr.L1_R != null && rr.L1_G != null && rr.L1_B != null;


                if (validResources)
                {
                    cmdBuffer.SetGlobalBuffer(HDShaderIDs._APVResIndex, rr.index);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL0, rr.L0);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_R, rr.L1_R);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_G, rr.L1_G);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_B, rr.L1_B);
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

                cmdBuffer.SetGlobalBuffer(HDShaderIDs._APVResIndex, m_EmptyIndexBuffer);
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL0, TextureXR.GetBlackTexture3D());
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_R, TextureXR.GetBlackTexture3D());
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_G, TextureXR.GetBlackTexture3D());
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_B, TextureXR.GetBlackTexture3D());
            }
        }

        private void UpdateShaderVariablesProbeVolumes(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            bool loadedData = ProbeReferenceVolume.instance.DataHasBeenLoaded();
            cb._EnableProbeVolumes = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) && loadedData) ? 1u : 0u;
        }
    }
}
