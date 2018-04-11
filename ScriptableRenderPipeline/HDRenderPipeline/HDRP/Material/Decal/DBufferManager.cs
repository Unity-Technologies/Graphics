using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

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
                m_RTs[dbufferIndex] = RTHandle.Alloc(Vector2.one, colorFormat: rtFormat[dbufferIndex], sRGB: sRGBFlags[dbufferIndex], filterMode: FilterMode.Point, name: string.Format("DBuffer{0}", dbufferIndex));
                m_RTIDs[dbufferIndex] = m_RTs[dbufferIndex].nameID;
                m_TextureShaderIDs[dbufferIndex] = HDShaderIDs._DBufferTexture[dbufferIndex];
            }

            // We use 8x8 tiles in order to match the native GCN HTile as closely as possible.
            m_HTile = RTHandle.Alloc(size => new Vector2Int((size.x + 7) / 8, (size.y + 7) / 8), filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.R8, sRGB: false, enableRandomWrite: true, name: "DBufferHTile"); // Enable UAV
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

        public void SetHTileTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._DecalHTileTexture, m_HTile);
        }

        public void PushGlobalParams(CommandBuffer cmd, FrameSettings frameSettings)
        {
            if (frameSettings.enableDBuffer)
            {
                cmd.SetGlobalInt(HDShaderIDs._EnableDBuffer, vsibleDecalCount > 0 ? 1 : 0);
                cmd.SetGlobalVector(HDShaderIDs._DecalAtlasResolution, new Vector2(DecalSystem.kDecalAtlasSize, DecalSystem.kDecalAtlasSize));
                BindBufferAsTextures(cmd);
            }
            else
            {
                cmd.SetGlobalInt(HDShaderIDs._EnableDBuffer, 0);
                // We still bind black textures to make sure that something is bound (can be a problem on some platforms)
                for (int i = 0; i < m_BufferCount; ++i)
                {
                    cmd.SetGlobalTexture(m_TextureShaderIDs[i], RuntimeUtilities.blackTexture);
                }
            }
        }
    }
}
