namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        public struct APVRuntimeResources
        {
            public ComputeBuffer index;
            public Texture3D     L0;
            public Texture3D     L1_R;
            public Texture3D     L1_G;
            public Texture3D     L1_B;

            public bool IsValid() { return index != null && L0 != null && L1_R != null && L1_G != null && L1_B != null; }
            public void Clear() { index = null; L0 = L1_R = L1_G = L1_B = null; }
        }

        APVRuntimeResources m_APVResources = new APVRuntimeResources();

        public void AssignAPVRuntimeResources(APVRuntimeResources apvRes) { m_APVResources = apvRes; }
        public void ClearAPVRuntimeResources() { m_APVResources.Clear(); }
        private void BindAPVRuntimeResources(CommandBuffer cmdBuffer)
        {
            if( m_APVResources.IsValid())
            {
                cmdBuffer.SetGlobalBuffer( HDShaderIDs._APVResIndex, m_APVResources.index);
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL0   , m_APVResources.L0);
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_R , m_APVResources.L1_R);
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_G , m_APVResources.L1_G);
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_B , m_APVResources.L1_B);
            }
        }
    }
}
