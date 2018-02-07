using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DBufferManager : MRTBufferManager
    {
        public int vsibleDecalCount { get; set; }

        RTHandle m_HTile;

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

            // We use 8x8 tiles in order to match the native GCN HTile as closely as possible.
            m_HTile = RTHandle.Alloc(size => new Vector2Int((size.x + 7) / 8, (size.y + 7) / 8), filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.R8, sRGB: false, enableRandomWrite: true); // Enable UAV
        }

        override public void DestroyBuffers()
        {
            base.DestroyBuffers();
            RTHandle.Release(m_HTile);
        }

        public void ClearNormalTargetAndHTile(CommandBuffer cmd, HDCamera camera, Color clearColor)
        {
            // index 1 is normals
            HDUtils.SetRenderTarget(cmd, camera, m_RTs[1], ClearFlag.Color, clearColor);
            HDUtils.SetRenderTarget(cmd, camera, m_HTile, ClearFlag.Color, CoreUtils.clearColorAllBlack);
        }

        public void SetHTile(int bindSlot, CommandBuffer cmd)
        {
            cmd.SetRandomWriteTarget(bindSlot, m_HTile);
        }

        public void UnSetHTile(CommandBuffer cmd)
        {
            cmd.ClearRandomWriteTargets();
        }

        public void PushGlobalParams(CommandBuffer cmd)
        {
            cmd.SetGlobalInt(HDShaderIDs._EnableDBuffer, vsibleDecalCount > 0 ? 1 : 0);
            BindBufferAsTextures(cmd);
        }
    }
}
