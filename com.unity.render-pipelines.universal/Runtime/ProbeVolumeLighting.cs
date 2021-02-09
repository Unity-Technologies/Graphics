namespace UnityEngine.Rendering.Universal
{
    public partial class UniversalRenderPipeline
    {
        private bool referenceVolumeInitialized = false;
        private ProbeVolumeSHBands probeVolumeSHBands = ProbeVolumeSHBands.SphericalHarmonicsL1;

        struct ShaderIDs
        {
            // Adaptive Probe Volume
            public static readonly int _APVResIndex = Shader.PropertyToID("_APVResIndex");
            public static readonly int _APVResL0_L1Rx = Shader.PropertyToID("_APVResL0_L1Rx");
            public static readonly int _APVResL1G_L1Ry = Shader.PropertyToID("_APVResL1G_L1Ry");
            public static readonly int _APVResL1B_L1Rz = Shader.PropertyToID("_APVResL1B_L1Rz");

            public static readonly int _APVResL2_0 = Shader.PropertyToID("_APVResL2_0");
            public static readonly int _APVResL2_1 = Shader.PropertyToID("_APVResL2_1");
            public static readonly int _APVResL2_2 = Shader.PropertyToID("_APVResL2_2");
            public static readonly int _APVResL2_3 = Shader.PropertyToID("_APVResL2_3");

        }

        private ComputeBuffer m_EmptyIndexBuffer = null;

        private void InitProbeVolumes(UniversalRenderPipelineAsset asset)
        {
            if (!asset.probeVolume)
            { 
                referenceVolumeInitialized = false;
                return;
            }

            ProbeReferenceVolume.instance.InitProbeReferenceVolume(ProbeReferenceVolume.s_ProbeIndexPoolAllocationSize, asset.probeVolumeMemoryBudget, ProbeReferenceVolumeProfile.s_DefaultIndexDimensions);
            referenceVolumeInitialized = true;
            probeVolumeSHBands = asset.probeVolumeSHBands;
        }

        public void BindProbeVolumeRuntimeResources(CommandBuffer cmdBuffer)
        {
            bool needToBindNeutral = true;

            CoreUtils.SetKeyword(cmdBuffer, ShaderKeywordStrings.PROBE_VOLUMES_L1, probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL1);
            CoreUtils.SetKeyword(cmdBuffer, ShaderKeywordStrings.PROBE_VOLUMES_L2, probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2);
            CoreUtils.SetKeyword(cmdBuffer, ShaderKeywordStrings.PROBE_VOLUMES_OFF, !referenceVolumeInitialized);

            if (referenceVolumeInitialized)
            {
                ProbeReferenceVolume.RuntimeResources rr = ProbeReferenceVolume.instance.GetRuntimeResources();

                bool validResources = rr.index != null && rr.L0_L1rx != null && rr.L1_G_ry != null && rr.L1_B_rz != null;

                if (validResources)
                {
                    cmdBuffer.SetGlobalBuffer(ShaderIDs._APVResIndex, rr.index);

                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL0_L1Rx, rr.L0_L1rx);
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1G_L1Ry, rr.L1_G_ry);
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1B_L1Rz, rr.L1_B_rz);

                    if (probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    {
                        cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL2_0, rr.L2_0);
                        cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL2_1, rr.L2_1);
                        cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL2_2, rr.L2_2);
                        cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL2_3, rr.L2_3);
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

                cmdBuffer.SetGlobalBuffer(ShaderIDs._APVResIndex, m_EmptyIndexBuffer);

                cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL0_L1Rx, TextureXR.GetBlackTexture3D());

                cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1G_L1Ry, TextureXR.GetBlackTexture3D());
                cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL1B_L1Rz, TextureXR.GetBlackTexture3D());

                if (probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL2_0, TextureXR.GetBlackTexture3D());
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL2_1, TextureXR.GetBlackTexture3D());
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL2_2, TextureXR.GetBlackTexture3D());
                    cmdBuffer.SetGlobalTexture(ShaderIDs._APVResL2_3, TextureXR.GetBlackTexture3D());
                }
            }
        }

        private void PerformPendingProbeVolumeOperations()
        {
            if (referenceVolumeInitialized)
                ProbeReferenceVolume.instance.PerformPendingOperations();
        }
    }
}
