using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class DBufferManager : MRTBufferManager
    {


        public DBufferManager()
            : base(Decal.GetMaterialDBufferCount())
        {
            Debug.Assert(m_BufferCount <= 4);
        }

        public RTHandle[] GetRTHandles() { return m_RTs; }

        public override void CreateBuffers()
        {
            GraphicsFormat[] rtFormat;
            Decal.GetMaterialDBufferDescription(out rtFormat);

            for (int dbufferIndex = 0; dbufferIndex < m_BufferCount; ++dbufferIndex)
            {
                m_RTs[dbufferIndex] = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: rtFormat[dbufferIndex], dimension: TextureXR.dimension, useDynamicScale: true, name: string.Format("DBuffer{0}", dbufferIndex));
                m_RTIDs[dbufferIndex] = m_RTs[dbufferIndex].nameID;
                m_TextureShaderIDs[dbufferIndex] = HDShaderIDs._DBufferTexture[dbufferIndex];
            }
        }

        override public void DestroyBuffers()
        {
            base.DestroyBuffers();
        }

        public void BindBlackTextures(CommandBuffer cmd)
        {
            for (int i = 0; i < m_BufferCount; ++i)
            {
                cmd.SetGlobalTexture(m_TextureShaderIDs[i], TextureXR.GetBlackTexture());
            }

            cmd.SetGlobalTexture(HDShaderIDs._DecalPrepassTexture, TextureXR.GetBlackTexture());
        }
    }
}
