#if VOLUMETRIC_FOG

using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    // Per-camera double-buffered 3D v-buffer history used by VolumetricLightingPass for temporal
    // reprojection. The two RTHandles are swapped each frame: the previous-frame texture is
    // sampled at each voxel's center reprojected through the previous view-projection, and the
    // blended result is written to the current-frame texture as the next frame's history.
    // Size matches the v-buffer (vbufferW * vbufferH * sliceCount) — separate from the camera's
    // color buffer, so the BufferedRTHandleSystem auto-resize path is not used. Format is
    // RGBA16F (matches the lighting pass output: linearized RGB + optical depth).
    internal sealed class VolumetricFogHistory : CameraHistoryItem
    {
        const string k_TextureName = "VBuffer Fog History";

        int m_Id;
        int m_Width;
        int m_Height;
        int m_SliceCount;
        // Number of consecutive frames during which the history has been written with matching
        // params. The first frame after allocation is bootstrap (history is the current frame
        // copied to itself); reprojection blending only kicks in once we have a real prior frame.
        int m_ValidFrames;

        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            base.OnCreate(owner, typeId);
            m_Id = MakeId(0);
        }

        public override void Reset()
        {
            ReleaseHistoryFrameRT(m_Id);
            m_Width = 0;
            m_Height = 0;
            m_SliceCount = 0;
            m_ValidFrames = 0;
        }

        public RTHandle GetCurrentTexture() => GetCurrentFrameRT(m_Id);
        public RTHandle GetPreviousTexture() => GetPreviousFrameRT(m_Id);

        // 0 on the first frame after (re)allocation, incremented every Update. Reprojection
        // blending should only be enabled when this is at least 2 (i.e. we have a previous-frame
        // texture that was actually written by the lighting pass).
        public int validFrames => m_ValidFrames;

        // Returns true if the history textures are sized to (width, height, sliceCount) and
        // ready to use. Reallocates if the size has changed (which invalidates the history).
        // Call once per frame from the lighting pass after computing the v-buffer geometry.
        internal bool Update(int width, int height, int sliceCount)
        {
            if (width <= 0 || height <= 0 || sliceCount <= 0)
                return false;

            bool sizeChanged = m_Width != width || m_Height != height || m_SliceCount != sliceCount;
            if (sizeChanged)
                Reset();

            if (GetCurrentTexture() == null)
            {
                var desc = new RenderTextureDescriptor(width, height, GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.None)
                {
                    dimension = TextureDimension.Tex3D,
                    volumeDepth = sliceCount,
                    enableRandomWrite = true,
                    msaaSamples = 1,
                };

                AllocHistoryFrameRT(m_Id, 2, ref desc, FilterMode.Bilinear, k_TextureName);

                m_Width = width;
                m_Height = height;
                m_SliceCount = sliceCount;
                m_ValidFrames = 0;
            }

            m_ValidFrames++;
            return true;
        }
    }
}

#endif
