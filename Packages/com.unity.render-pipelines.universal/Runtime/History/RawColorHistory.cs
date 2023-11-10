using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Raw render color history. The color snapshot is taken before post processing. Raw rendered geometry. No UI overlay.
    /// A matching depth is RawDepthHistory.
    /// Color space is linear RGB.
    /// No mips.
    /// MSAA is not supported and is resolved for the history.
    /// XR is supported.
    /// </summary>
    public sealed class RawColorHistory : CameraHistoryItem
    {
        private int[] m_Ids = new int[2];
        private static readonly string[] m_Names = new[]
        {
            "RawColorHistory0",
            "RawColorHistory1"
        };
        private RenderTextureDescriptor m_Descriptor;
        private Hash128 m_DescKey;

        /// <inheritdoc />
        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            base.OnCreate(owner, typeId);
            m_Ids[0] = MakeId(0);
            m_Ids[1] = MakeId(1);
        }

        /// <summary>
        /// Get the current history texture.
        /// Current history might not be valid yet. It is valid only after executing the producing render pass.
        /// </summary>
        /// <param name="eyeIndex">Eye index, typically XRPass.multipassId.</param>
        /// <returns>The texture.</returns>
        public RTHandle GetCurrentTexture(int eyeIndex = 0)
        {
            if ((uint)eyeIndex >= m_Ids.Length)
                return null;

            return GetCurrentFrameRT(m_Ids[eyeIndex]);
        }

        /// <summary>
        /// Get the previous history texture.
        /// Previous history might not be valid yet. It is valid only after executing the producing render pass.
        /// </summary>
        /// <param name="eyeIndex">Eye index, typically XRPass.multipassId.</param>
        /// <returns>The texture.</returns>
        public RTHandle GetPreviousTexture(int eyeIndex = 0)
        {
            if ((uint)eyeIndex >= m_Ids.Length)
                return null;

            return GetPreviousFrameRT(m_Ids[eyeIndex]);
        }

        private bool IsAllocated()
        {
            return GetCurrentTexture() != null;
        }

        // True if the desc changed, graphicsFormat etc.
        private bool IsDirty(ref RenderTextureDescriptor desc)
        {
            return m_DescKey != Hash128.Compute(ref desc);
        }

        private void Alloc(ref RenderTextureDescriptor desc, bool xrMultipassEnabled)
        {
            // In generic case, the current texture might not have been written yet. We need double buffering.
            AllocHistoryFrameRT(m_Ids[0], 2, ref desc, m_Names[0]);

            if(xrMultipassEnabled)
                AllocHistoryFrameRT(m_Ids[1], 2, ref desc, m_Names[1]);

            m_Descriptor = desc;
            m_DescKey = Hash128.Compute(ref desc);
        }

        /// <summary>
        /// Release the history texture(s).
        /// </summary>
        public override void Reset()
        {
            for(int i = 0; i < m_Ids.Length; i++)
                ReleaseHistoryFrameRT(m_Ids[i]);
        }

        internal RenderTextureDescriptor GetHistoryDescriptor(ref RenderTextureDescriptor cameraDesc)
        {
            var colorDesc = cameraDesc;
            colorDesc.depthBufferBits = (int)DepthBits.None;
            colorDesc.mipCount = 0;
            colorDesc.msaaSamples = 1;

            return colorDesc;
        }

        // Return true if the RTHandles were reallocated.
        internal bool Update(ref RenderTextureDescriptor cameraDesc, bool xrMultipassEnabled = false)
        {
            if (cameraDesc.width > 0 && cameraDesc.height > 0 && cameraDesc.graphicsFormat != GraphicsFormat.None)
            {
                var historyDesc = GetHistoryDescriptor(ref cameraDesc);

                if (IsDirty(ref historyDesc))
                    Reset();

                if (!IsAllocated())
                {
                    Alloc(ref historyDesc, xrMultipassEnabled);
                    return true;
                }
            }

            return false;
        }
    }
}
