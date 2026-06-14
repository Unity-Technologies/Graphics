#if MODERN_SSAO
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Screen Space Ambient Occlusion (SSAO) history for temporal filtering.
    /// Uses double buffering so the previous frame can be sampled while the current frame is written.
    /// The history stores packed depth (RG), AO (B), and a normalized accumulated frame count (A) in an RGBA8 texture.
    /// </summary>
    public sealed class SSAOHistory : CameraHistoryItem
    {
        private int[] m_AccumulationTextureIds = new int[2];
        private int[] m_AccumulationVersions = new int[2];

        private static readonly string[] m_AccumulationNames = new[]
        {
            "SSAOAccumulationTex0",
            "SSAOAccumulationTex1"
        };

        private RenderTextureDescriptor m_Descriptor;
        private Hash128 m_DescKey;

        /// <summary>
        /// Called internally on instance creation.
        /// Sets up RTHandle ids.
        /// </summary>
        /// <param name="owner">BufferedRTHandleSystem of the owning camera.</param>
        /// <param name="typeId">Unique id given to SSAOHistory by the owning camera.</param>
        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            base.OnCreate(owner, typeId);
            m_AccumulationTextureIds[0] = MakeId(0);
            m_AccumulationTextureIds[1] = MakeId(1);
        }

        /// <summary>
        /// Release SSAO accumulation textures.
        /// </summary>
        public override void Reset()
        {
            for (int i = 0; i < m_AccumulationTextureIds.Length; i++)
            {
                ReleaseHistoryFrameRT(m_AccumulationTextureIds[i]);
                m_AccumulationVersions[i] = -1;
            }

            m_Descriptor.width = 0;
            m_Descriptor.height = 0;
            m_Descriptor.graphicsFormat = GraphicsFormat.None;
            m_DescKey = Hash128.Compute(0);
        }

        /// <summary>
        /// Get the SSAO history write target for the current frame.
        /// </summary>
        /// <param name="eyeIndex">Eye index for XR multi-pass.</param>
        /// <returns>Current frame RTHandle for SSAO history writes.</returns>
        public RTHandle GetCurrentTexture(int eyeIndex = 0)
        {
            if ((uint)eyeIndex >= m_AccumulationTextureIds.Length)
                return null;

            return GetCurrentFrameRT(m_AccumulationTextureIds[eyeIndex]);
        }

        /// <summary>
        /// Get the SSAO history read target from the previous frame.
        /// </summary>
        /// <param name="eyeIndex">Eye index for XR multi-pass.</param>
        /// <returns>Previous frame RTHandle for SSAO history reads.</returns>
        public RTHandle GetPreviousTexture(int eyeIndex = 0)
        {
            if ((uint)eyeIndex >= m_AccumulationTextureIds.Length)
                return null;

            return GetPreviousFrameRT(m_AccumulationTextureIds[eyeIndex]);
        }

        /// <summary>
        /// Get SSAO accumulation texture.
        /// </summary>
        /// <param name="eyeIndex">Eye index for XR multi-pass.</param>
        /// <returns>Current frame RTHandle for SSAO accumulation texture.</returns>
        public RTHandle GetAccumulationTexture(int eyeIndex = 0)
        {
            return GetCurrentTexture(eyeIndex);
        }

        /// <summary>
        /// Get SSAO accumulation texture version.
        /// Tracks which frame the accumulation was last updated.
        /// </summary>
        /// <param name="eyeIndex">Eye index for XR multi-pass.</param>
        /// <returns>Accumulation texture version.</returns>
        public int GetAccumulationVersion(int eyeIndex = 0)
        {
            return m_AccumulationVersions[eyeIndex];
        }

        /// <summary>
        /// Set SSAO accumulation texture version.
        /// </summary>
        /// <param name="eyeIndex">Eye index for XR multi-pass.</param>
        /// <param name="version">Version to set (typically Time.frameCount).</param>
        internal void SetAccumulationVersion(int eyeIndex, int version)
        {
            m_AccumulationVersions[eyeIndex] = version;
        }

        // Check if the SSAO accumulation texture is valid.
        private bool IsValid()
        {
            return GetCurrentTexture(0) != null;
        }

        // True if the desc changed, graphicsFormat etc.
        private bool IsDirty(ref RenderTextureDescriptor desc)
        {
            return m_DescKey != Hash128.Compute(ref desc);
        }

        private void Alloc(ref RenderTextureDescriptor desc, bool xrMultipassEnabled)
        {
            AllocHistoryFrameRT(m_AccumulationTextureIds[0], 2, ref desc, m_AccumulationNames[0]);

            if (xrMultipassEnabled)
                AllocHistoryFrameRT(m_AccumulationTextureIds[1], 2, ref desc, m_AccumulationNames[1]);

            m_Descriptor = desc;
            m_DescKey = Hash128.Compute(ref desc);
        }

        /// <summary>
        /// Create SSAO history descriptor from camera descriptor.
        /// </summary>
        /// <param name="cameraDesc">Camera render texture descriptor.</param>
        /// <param name="downsample">Whether SSAO is running at half resolution.</param>
        /// <param name="enableRandomWrite">Whether the resource requires enableRandomWrite.</param>
        /// <returns>SSAO history render texture descriptor.</returns>
        internal static RenderTextureDescriptor GetHistoryDescriptor(ref RenderTextureDescriptor cameraDesc, bool downsample, bool enableRandomWrite)
        {
            int downsampleDivider = downsample ? 2 : 1;

            RenderTextureDescriptor ssaoDesc = cameraDesc;
            ssaoDesc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            ssaoDesc.depthStencilFormat = GraphicsFormat.None;
            ssaoDesc.msaaSamples = 1;
            ssaoDesc.width /= downsampleDivider;
            ssaoDesc.height /= downsampleDivider;
            ssaoDesc.enableRandomWrite = enableRandomWrite;

            return ssaoDesc;
        }

        /// <summary>
        /// Update the SSAO history texture allocation.
        /// </summary>
        /// <param name="cameraData">Camera data providing the render texture descriptor.</param>
        /// <param name="downsample">Whether SSAO is running at half resolution.</param>
        /// <param name="enableRandomWrite">Whether the resource requires enableRandomWrite.</param>
        /// <param name="xrMultipassEnabled">Whether XR multi-pass is enabled.</param>
        /// <returns>True if the RTHandles were reallocated.</returns>
        internal bool Update(UniversalCameraData cameraData, bool downsample, bool enableRandomWrite, bool xrMultipassEnabled = false)
        {
            ref RenderTextureDescriptor cameraDesc = ref cameraData.cameraTargetDescriptor;
            if (cameraDesc.width > 0 && cameraDesc.height > 0 && cameraDesc.graphicsFormat != GraphicsFormat.None)
            {
                var ssaoDesc = GetHistoryDescriptor(ref cameraDesc, downsample, enableRandomWrite);

                Camera camera = cameraData.camera;
                bool isPreview = camera.cameraType == CameraType.Preview;
                bool isRenderRequest = camera.isProcessingRenderRequest;
                if (!isPreview && !isRenderRequest && IsDirty(ref ssaoDesc))
                    Reset();

                if (!IsValid())
                {
                    Alloc(ref ssaoDesc, xrMultipassEnabled);
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
