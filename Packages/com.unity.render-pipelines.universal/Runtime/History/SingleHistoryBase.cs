using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    // TODO: Draft. A convenience base class to wrap single texture into a type.
    internal abstract class SingleHistoryBase : CameraHistoryItem
    {
        private int m_Id;
        private RenderTextureDescriptor m_Descriptor;
        private Hash128 m_DescKey;

        /// <summary>
        /// Called internally on instance creation.
        /// Sets up RTHandle ids.
        /// </summary>
        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            base.OnCreate(owner, typeId);
            m_Id = MakeId(0);
        }

        /// <summary>
        /// Get the history texture(s).
        /// </summary>
        public RTHandle GetTexture(int frameIndex = 0)
        {
            if ((uint)frameIndex >= GetHistoryFrameCount())
                return null;
            return storage.GetFrameRT(m_Id, frameIndex);
        }

        public RTHandle GetCurrentTexture()
        {
            return GetCurrentFrameRT(m_Id);
        }

        public RTHandle GetPreviousTexture()
        {
            return GetTexture(1);
        }

        internal bool IsAllocated()
        {
            return GetTexture() != null;
        }

        // True if the desc changed, graphicsFormat etc.
        internal bool IsDirty(ref RenderTextureDescriptor desc)
        {
            return m_DescKey != Hash128.Compute(ref desc);
        }

        private void Alloc(ref RenderTextureDescriptor desc)
        {
            AllocHistoryFrameRT(m_Id, GetHistoryFrameCount(), ref desc, GetHistoryName());

            m_Descriptor = desc;
            m_DescKey = Hash128.Compute(ref desc);
        }

        /// <summary>
        /// Release the history texture(s).
        /// </summary>
        public override void Reset()
        {
            ReleaseHistoryFrameRT(m_Id);
        }

        // Return true if the RTHandles were reallocated.
        internal bool Update(ref RenderTextureDescriptor cameraDesc)
        {
            if (cameraDesc.width > 0 && cameraDesc.height > 0 && cameraDesc.graphicsFormat != GraphicsFormat.None)
            {
                var historyDesc = GetHistoryDescriptor(ref cameraDesc);

                if (IsDirty(ref historyDesc))
                    Reset();

                if (!IsAllocated())
                {
                    Alloc(ref historyDesc);
                    return true;
                }
            }

            return false;
        }

        protected abstract int GetHistoryFrameCount();
        protected abstract string GetHistoryName();
        protected abstract RenderTextureDescriptor GetHistoryDescriptor(ref RenderTextureDescriptor cameraDesc);
    }
}
