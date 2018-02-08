using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class GBufferManager : MRTBufferManager
    {
        int m_GBufferCount = 0;
        bool m_EnableShadowMask = false;
        RenderPipelineMaterial m_DeferredMaterial;
        protected RenderTargetIdentifier[] m_RTIDsNoShadowMask;

        public GBufferManager(RenderPipelineMaterial deferredMaterial, bool enableBakeShadowMask)
            : base(deferredMaterial.GetMaterialGBufferCount() + (enableBakeShadowMask ? 1 : 0))
        {
            Debug.Assert(m_BufferCount <= 8);

            m_DeferredMaterial = deferredMaterial;
            m_GBufferCount = deferredMaterial.GetMaterialGBufferCount();
            m_EnableShadowMask = enableBakeShadowMask;

            m_RTIDsNoShadowMask = new RenderTargetIdentifier[m_GBufferCount];
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
                m_RTIDsNoShadowMask[gbufferIndex] = HDShaderIDs._GBufferTexture[gbufferIndex];
            }

            if (m_EnableShadowMask)
            {
                m_RTs[m_GBufferCount] = RTHandle.Alloc(Vector2.one, colorFormat: Builtin.GetShadowMaskBufferFormat(), sRGB: Builtin.GetShadowMaskSRGBFlag(), filterMode: FilterMode.Point);
                m_RTIDs[m_GBufferCount] = new RenderTargetIdentifier(m_RTs[m_GBufferCount]);
                m_TextureShaderIDs[m_GBufferCount] = HDShaderIDs._ShadowMaskTexture;
            }
        }

        public RenderTargetIdentifier[] GetBuffersRTI(bool enableShadowMask)
        {
            if(!enableShadowMask)
            {
                // nameID can change from one frame to another depending on the msaa flag so so we need to update this array to be sure it's up to date.
                // Moreover, if we don't have shadow masks we only need to bind the first GBuffers
                // This is important because in the shader the shadowmask buffer gets optimized out so anything bound after (like the DBuffer HTile) has a different bind point.
                for (int i = 0; i < m_GBufferCount; ++i)
                {
                    m_RTIDsNoShadowMask[i] = m_RTs[i].nameID;
                }
                return m_RTIDsNoShadowMask;

            }
            else
            {
                return GetBuffersRTI();
            }
        }

        public int GetBufferCount(bool enableShadowMask)
        {
            return enableShadowMask ? m_BufferCount : m_GBufferCount;
        }
    }
}
