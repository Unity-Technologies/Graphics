using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DBufferManager : MRTBufferManager
    {
        public bool EnableDBUffer { get; set; }

        RTHandleSystem.RTHandle m_HTile;

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
                m_RTs[dbufferIndex] = RTHandles.Alloc(Vector2.one, colorFormat: rtFormat[dbufferIndex], sRGB: sRGBFlags[dbufferIndex], filterMode: FilterMode.Point, name: string.Format("DBuffer{0}", dbufferIndex));
                m_RTIDs[dbufferIndex] = m_RTs[dbufferIndex].nameID;
                m_TextureShaderIDs[dbufferIndex] = HDShaderIDs._DBufferTexture[dbufferIndex];
            }

            // We use 8x8 tiles in order to match the native GCN HTile as closely as possible.
            m_HTile = RTHandles.Alloc(size => new Vector2Int((size.x + 7) / 8, (size.y + 7) / 8), filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.R8, sRGB: false, enableRandomWrite: true, name: "DBufferHTile"); // Enable UAV
        }

        override public void DestroyBuffers()
        {
            base.DestroyBuffers();
            RTHandles.Release(m_HTile);
        }

        public void ClearTargets(CommandBuffer cmd, HDCamera camera)
        {
            // for alpha compositing, color is cleared to 0, alpha to 1
            // https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html

            Color clearColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            Color clearColorNormal = new Color(0.5f, 0.5f, 0.5f, 1.0f); // for normals 0.5 is neutral
            HDUtils.SetRenderTarget(cmd, camera, m_RTs[0], ClearFlag.Color, clearColor);
            HDUtils.SetRenderTarget(cmd, camera, m_RTs[1], ClearFlag.Color, clearColorNormal);
            HDUtils.SetRenderTarget(cmd, camera, m_RTs[2], ClearFlag.Color, clearColor);
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

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (hdCamera.frameSettings.enableDBuffer)
            {
                cmd.SetGlobalInt(HDShaderIDs._EnableDBuffer, EnableDBUffer ? 1 : 0);
                cmd.SetGlobalVector(HDShaderIDs._DecalAtlasResolution, new Vector2(HDUtils.hdrpSettings.decalSettings.atlasWidth, HDUtils.hdrpSettings.decalSettings.atlasHeight));
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
