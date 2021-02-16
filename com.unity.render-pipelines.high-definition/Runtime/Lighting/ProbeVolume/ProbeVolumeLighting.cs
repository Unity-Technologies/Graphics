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

                bool validResources = rr.index != null && rr.L0_L1rx != null && rr.L1_G_ry != null && rr.L1_B_rz != null;

                if (validResources)
                {
                    cmdBuffer.SetGlobalBuffer(HDShaderIDs._APVResIndex, rr.index);

                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL0_L1Rx, rr.L0_L1rx);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1G_L1Ry, rr.L1_G_ry);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1B_L1Rz, rr.L1_B_rz);

                    if (m_Asset.currentPlatformRenderPipelineSettings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    {
                        cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL2_0, rr.L2_0);
                        cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL2_1, rr.L2_1);
                        cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL2_2, rr.L2_2);
                        cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL2_3, rr.L2_3);
                    }

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

                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL0_L1Rx, TextureXR.GetBlackTexture3D());

                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1G_L1Ry, TextureXR.GetBlackTexture3D());
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1B_L1Rz, TextureXR.GetBlackTexture3D());

                if (m_Asset.currentPlatformRenderPipelineSettings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL2_0, TextureXR.GetBlackTexture3D());
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL2_1, TextureXR.GetBlackTexture3D());
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL2_2, TextureXR.GetBlackTexture3D());
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL2_3, TextureXR.GetBlackTexture3D());
                }
            }
        }

        private void UpdateShaderVariablesProbeVolumes(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            bool loadedData = ProbeReferenceVolume.instance.DataHasBeenLoaded();
            cb._EnableProbeVolumes = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) && loadedData) ? 1u : 0u;
        }
    }
}
