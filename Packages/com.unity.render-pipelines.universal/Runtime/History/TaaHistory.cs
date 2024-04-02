using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Temporal Anti-aliasing (TAA) persistent data for Universal Rendering Pipeline.
    /// Holds the TAA color history accumulation texture.
    /// </summary>
    public sealed class TaaHistory : CameraHistoryItem
    {
        private int[] m_TaaAccumulationTextureIds = new int[2];
        private int[] m_TaaAccumulationVersions = new int[2];

        private static readonly string[] m_TaaAccumulationNames = new[]
        {
            "TaaAccumulationTex0",
            "TaaAccumulationTex1"
        };

        private RenderTextureDescriptor m_Descriptor;
        private Hash128 m_DescKey;

        /// <summary>
        /// Called internally on instance creation.
        /// Sets up RTHandle ids.
        /// </summary>
        /// <param name="owner">BufferedRTHandleSystem of the owning camera.</param>
        /// <param name="typeId">Unique id given to TaaHistory by the owning camera.</param>
        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            base.OnCreate(owner, typeId);
            m_TaaAccumulationTextureIds[0] = MakeId(0);
            m_TaaAccumulationTextureIds[1] = MakeId(1);
        }

        /// <summary>
        /// Release TAA accumulation textures.
        /// </summary>
        public override void Reset()
        {
            for (int i = 0; i < m_TaaAccumulationTextureIds.Length; i++)
            {
                ReleaseHistoryFrameRT(m_TaaAccumulationTextureIds[i]);
                m_TaaAccumulationVersions[i] = -1;
            }

            m_Descriptor.width = 0;
            m_Descriptor.height = 0;
            m_Descriptor.graphicsFormat = GraphicsFormat.None;
            m_DescKey = Hash128.Compute(0);
        }

        /// <summary>
        /// Get TAA accumulation texture.
        /// </summary>
        /// <param name="eyeIndex">Eye index for XR multi-pass.</param>
        /// <returns>Current frame RTHandle for TAA accumulation texture.</returns>
        public RTHandle GetAccumulationTexture(int eyeIndex = 0)
        {
            return GetCurrentFrameRT(m_TaaAccumulationTextureIds[eyeIndex]);
        }

        /// <summary>
        /// Get TAA accumulation texture version.
        /// Tracks which frame the accumulation was last updated.
        /// </summary>
        /// <param name="eyeIndex">Eye index for XR multi-pass.</param>
        /// <returns>Accumulation texture version.</returns>
        public int GetAccumulationVersion(int eyeIndex = 0)
        {
            return m_TaaAccumulationVersions[eyeIndex];
        }

        internal void SetAccumulationVersion(int eyeIndex, int version)
        {
            m_TaaAccumulationVersions[eyeIndex] = version;
        }

        // Check if the TAA accumulation texture is valid.
        private bool IsValid()
        {
            return GetAccumulationTexture(0) != null;
        }

        // True if the desc changed, graphicsFormat etc.
        private bool IsDirty(ref RenderTextureDescriptor desc)
        {
            return m_DescKey != Hash128.Compute(ref desc);
        }

        private void Alloc(ref RenderTextureDescriptor desc, bool xrMultipassEnabled)
        {
            AllocHistoryFrameRT(m_TaaAccumulationTextureIds[0], 1, ref desc, m_TaaAccumulationNames[0]);

            if (xrMultipassEnabled)
                AllocHistoryFrameRT(m_TaaAccumulationTextureIds[1], 1, ref desc, m_TaaAccumulationNames[1]);

            m_Descriptor = desc;
            m_DescKey = Hash128.Compute(ref desc);
        }

        // Return true if the RTHandles were reallocated.
        internal bool Update(ref RenderTextureDescriptor cameraDesc, bool xrMultipassEnabled = false)
        {
            if (cameraDesc.width > 0 && cameraDesc.height > 0 && cameraDesc.graphicsFormat != GraphicsFormat.None)
            {
                var taaDesc = TemporalAA.TemporalAADescFromCameraDesc(ref cameraDesc);

                if (IsDirty(ref taaDesc))
                    Reset();

                if (!IsValid())
                {
                    Alloc(ref taaDesc, xrMultipassEnabled);
                    return true;
                }
            }

            return false;
        }
    }
}
