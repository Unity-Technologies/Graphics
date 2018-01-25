using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DBufferManager : MRTBufferManager
    {
        public int vsibleDecalCount { get; set; }

        public DBufferManager()
            : base(Decal.GetMaterialDBufferCount())
        {
            Debug.Assert(m_BufferCount <= 4);
        }

        public override void CreateBuffers()
        {
            RenderTextureFormat[] rtFormat;
            bool[] sRGBFlags;
            Decal.GetMaterialDBufferDescription(out rtFormat, out sRGBFlags);

            for (int dbufferIndex = 0; dbufferIndex < m_BufferCount; ++dbufferIndex)
            {
                m_RTs[dbufferIndex] = RTHandle.Alloc(Vector2.one, colorFormat: rtFormat[dbufferIndex], sRGB: sRGBFlags[dbufferIndex], filterMode: FilterMode.Point);
                m_RTIDs[dbufferIndex] = m_RTs[dbufferIndex].nameID;
                m_TextureShaderIDs[dbufferIndex] = HDShaderIDs._DBufferTexture[dbufferIndex];
            }
        }

        public void PushGlobalParams(CommandBuffer cmd)
        {
            cmd.SetGlobalInt(HDShaderIDs._EnableDBuffer, vsibleDecalCount > 0 ? 1 : 0);
            BindBufferAsTextures(cmd);
        }
    }
}
