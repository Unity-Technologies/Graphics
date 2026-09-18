using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Double-buffered post-upscale alpha history consumed by the alpha upscale pass (core AlphaUpscaler).
    /// </summary>
    internal sealed class AlphaUpscaleHistory : CameraHistoryItem
    {
        // One id per eye for XR multi-pass rendering.
        int[] m_Ids = new int[2];
        static readonly string[] k_Names = { "AlphaUpscaleHistory0", "AlphaUpscaleHistory1" };

        Hash128 m_DescKey;

        // The frame the textures were (re)allocated on. XR multi-pass calls Update once per eye
        // within the same frame; tracking the frame keeps the history invalid for both eye passes
        // of the allocation frame, not just the one that performed the allocation.
        int m_AllocFrameIndex = -1;

        // False for the whole frame of a (re)allocation: the previous-frame content is undefined.
        internal bool validHistory { get; private set; }

        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            base.OnCreate(owner, typeId);
            m_Ids[0] = MakeId(0);
            m_Ids[1] = MakeId(1);
        }

        internal RTHandle GetCurrentTexture(int eyeIndex = 0) => GetCurrentFrameRT(m_Ids[eyeIndex]);
        internal RTHandle GetPreviousTexture(int eyeIndex = 0) => GetPreviousFrameRT(m_Ids[eyeIndex]);

        public override void Reset()
        {
            for (int i = 0; i < m_Ids.Length; i++)
                ReleaseHistoryFrameRT(m_Ids[i]);
            validHistory = false;
        }

        internal void Update(UniversalCameraData cameraData, bool xrMultipassEnabled)
        {
            // The history lives at the post-upscale resolution. Base the descriptor on the camera
            // target so the XR texture dimension carries over.
            var desc = cameraData.cameraTargetDescriptor;
            desc.width = cameraData.pixelWidth;
            desc.height = cameraData.pixelHeight;
            desc.graphicsFormat = GraphicsFormat.R16_SFloat;
            desc.depthStencilFormat = GraphicsFormat.None;
            desc.mipCount = 0;
            desc.msaaSamples = 1;
            desc.enableRandomWrite = true;
            // The history lives at the fixed post-upscale resolution.
            desc.useDynamicScale = false;

            var descKey = Hash128.Compute(ref desc);
            if (descKey != m_DescKey && GetCurrentTexture() != null)
                Reset();

            if (GetCurrentTexture() == null)
            {
                AllocHistoryFrameRT(m_Ids[0], 2, ref desc, k_Names[0]);
                if (xrMultipassEnabled)
                    AllocHistoryFrameRT(m_Ids[1], 2, ref desc, k_Names[1]);
                m_DescKey = descKey;
                m_AllocFrameIndex = Time.frameCount;
            }

            validHistory = Time.frameCount != m_AllocFrameIndex;
        }
    }
}
