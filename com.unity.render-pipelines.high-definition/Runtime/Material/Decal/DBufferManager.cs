using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class DBufferManager : MRTBufferManager
    {
        RTHandle m_HTile;

        public DBufferManager(bool use4RTs)
            : base(use4RTs ? 4 : 3)
        {
            Debug.Assert(m_BufferCount <= 4);
        }

        public RTHandle[] GetRTHandles() { return m_RTs; }
        public RTHandle GetHTileBuffer() { return m_HTile; }

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

            // We use 8x8 tiles in order to match the native GCN HTile as closely as possible.
            m_HTile = RTHandles.Alloc(size => new Vector2Int((size.x + 7) / 8, (size.y + 7) / 8), TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R32_UInt, enableRandomWrite: true, useDynamicScale: true, name: "DBufferHTile"); // Enable UAV
        }

        override public void DestroyBuffers()
        {
            base.DestroyBuffers();
            RTHandles.Release(m_HTile);
        }

        public void BindBlackTextures(CommandBuffer cmd)
        {
            for (int i = 0; i < m_BufferCount; ++i)
            {
                cmd.SetGlobalTexture(m_TextureShaderIDs[i], TextureXR.GetBlackTexture());
            }
        }
    }
}
