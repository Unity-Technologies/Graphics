namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static private ComputeBuffer m_EmptyIndexBuffer = null;

        public struct APVRuntimeResources
        {
            public ComputeBuffer index;
            public Texture3D     L0;
            public Texture3D     L1_R;
            public Texture3D     L1_G;
            public Texture3D     L1_B;

            public bool IsValid() { return index != null && L0 != null && L1_R != null && L1_G != null && L1_B != null; }
            public void Clear() { index = null; L0 = L1_R = L1_G = L1_B = null; }

            public void Cleanup()
            {
                CoreUtils.SafeRelease(m_EmptyIndexBuffer); // We free upon cleanup as it will be lazy-init again upon necessity.
                CoreUtils.SafeRelease(index);
                CoreUtils.Destroy(L0);
                CoreUtils.Destroy(L1_R);
                CoreUtils.Destroy(L1_G);
                CoreUtils.Destroy(L1_B);

                m_EmptyIndexBuffer = null;
                index = null;
                L0 = null;
                L1_R = null;
                L1_G = null;
                L1_B = null;
            }
        }

        APVRuntimeResources m_APVResources = new APVRuntimeResources();

        // TODO: Do we even need this, why not pulling directly from the instance? 
        public void AssignAPVRuntimeResources(APVRuntimeResources apvRes) { m_APVResources = apvRes; }
        public void ClearAPVRuntimeResources() { m_APVResources.Clear(); }
        private void BindAPVRuntimeResources(CommandBuffer cmdBuffer, HDCamera hdCamera)
        {
            bool needToBindNeutral = true;
            // TODO: Do this only if the framesetting is on, otherwise there is some hidden cost
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume))
            {
                var refVolume = ProbeReferenceVolume.instance;
                // TODO: I think we can bypass this; here we still go through it for sake of it, but I really think is not needed and will remove soon.
                if (ProbeReferenceVolume.instance.DataHasBeenLoaded() && !m_APVResources.IsValid())
                {
                    ProbeReferenceVolume.RuntimeResources rr = refVolume.GetRuntimeResources();
                    m_APVResources.index = rr.index;
                    m_APVResources.L0 = rr.L0;
                    m_APVResources.L1_R = rr.L1_R;
                    m_APVResources.L1_G = rr.L1_G;
                    m_APVResources.L1_B = rr.L1_B;
                }

                if (m_APVResources.IsValid())
                {
                    cmdBuffer.SetGlobalBuffer(HDShaderIDs._APVResIndex, m_APVResources.index);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL0, m_APVResources.L0);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_R, m_APVResources.L1_R);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_G, m_APVResources.L1_G);
                    cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_B, m_APVResources.L1_B);
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
            if (ShaderConfig.s_EnableProbeVolumes == 0)
                return;

            cb._EnableProbeVolumes = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume) && m_APVResources.IsValid()) ? 1u : 0u;
        }
    }
}
