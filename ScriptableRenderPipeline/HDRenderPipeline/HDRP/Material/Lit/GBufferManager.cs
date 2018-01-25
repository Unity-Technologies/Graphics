using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class GBufferManager : MRTBufferManager
    {
        int m_GBufferCount = 0;
        bool m_EnableShadowMask = false;
        RenderPipelineMaterial m_DeferredMaterial;

        public GBufferManager(RenderPipelineMaterial deferredMaterial, bool enableBakeShadowMask)
            : base(deferredMaterial.GetMaterialGBufferCount() + (enableBakeShadowMask ? 1 : 0))
        {
            Debug.Assert(m_BufferCount <= 8);

            m_DeferredMaterial = deferredMaterial;
            m_GBufferCount = deferredMaterial.GetMaterialGBufferCount();
            m_EnableShadowMask = enableBakeShadowMask;
        }

        public override void CreateBuffers()
        {
            RenderTextureFormat[] rtFormat;
            bool[] sRGBFlags;
            m_DeferredMaterial.GetMaterialGBufferDescription(out rtFormat, out sRGBFlags);

            for (int gbufferIndex = 0; gbufferIndex < m_GBufferCount; ++gbufferIndex)
            {
                m_RTs[gbufferIndex] = RTHandle.Alloc(Vector2.one, colorFormat: rtFormat[gbufferIndex], sRGB: sRGBFlags[gbufferIndex], filterMode: FilterMode.Point);
                m_RTIDs[gbufferIndex] = m_RTs[gbufferIndex].nameID;
                m_TextureShaderIDs[gbufferIndex] = HDShaderIDs._GBufferTexture[gbufferIndex];
            }

            if (m_EnableShadowMask)
            {
                m_RTs[m_GBufferCount] = RTHandle.Alloc(Vector2.one, colorFormat: Builtin.GetShadowMaskBufferFormat(), sRGB: Builtin.GetShadowMask_sRGBFlag(), filterMode: FilterMode.Point);
                m_RTIDs[m_GBufferCount] = new RenderTargetIdentifier(m_RTs[m_GBufferCount]);
                m_TextureShaderIDs[m_GBufferCount] = HDShaderIDs._ShadowMaskTexture;
            }
        }
    }
}
