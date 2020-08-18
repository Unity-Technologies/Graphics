namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        APVRuntimeResources m_APVResources = new APVRuntimeResources();

        public void AssignAPVRuntimeResources(APVRuntimeResources apvRes) { m_APVResources = apvRes; }
        public void ClearAPVRuntimeResources() { m_APVResources.Clear(); }
        private void BindAPVRuntimeResources(CommandBuffer cmdBuffer)
        {
            if( m_APVResources.IsValid())
            {
                cmdBuffer.SetGlobalBuffer (HDShaderIDs._APVResIndex, m_APVResources.index);
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL0   , m_APVResources.L0);
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_R , m_APVResources.L1_R);
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_G , m_APVResources.L1_G);
                cmdBuffer.SetGlobalTexture(HDShaderIDs._APVResL1_B , m_APVResources.L1_B);
            }
        }
    }
}
