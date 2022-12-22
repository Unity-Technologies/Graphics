using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class LocalVolumetricFogManager
    {
        // Allocate graphics buffers by chunk to avoid reallocating them too often
        private static readonly int k_IndirectBufferChunkSize = 50;

        static LocalVolumetricFogManager m_Manager;
        public static LocalVolumetricFogManager manager
        {
            get
            {
                if (m_Manager == null)
                    m_Manager = new LocalVolumetricFogManager();
                return m_Manager;
            }
        }

        List<LocalVolumetricFog> m_Volumes = null;

        /// <summary>Stores all the indirect arguments for the indirect draws of the fog</summary>
        internal GraphicsBuffer globalIndirectBuffer;
        /// <summary>Indirection buffer that transforms the an index in the global indirect buffer to an index for the volumetric material data buffer</summary>
        internal GraphicsBuffer globalIndirectionBuffer;

        LocalVolumetricFogManager()
        {
            m_Volumes = new List<LocalVolumetricFog>();
        }

        public void RegisterVolume(LocalVolumetricFog volume)
        {
            m_Volumes.Add(volume);
            ResizeBuffersIfNeeded();
        }

        public void DeRegisterVolume(LocalVolumetricFog volume)
        {
            if (m_Volumes.Contains(volume))
            {
                m_Volumes.Remove(volume);
                ResizeBuffersIfNeeded();
            }
        }

        int GetNeededBufferCount()
            => Mathf.Max(k_IndirectBufferChunkSize, Mathf.CeilToInt(m_Volumes.Count / (float)k_IndirectBufferChunkSize) * k_IndirectBufferChunkSize);

        internal unsafe void InitializeGraphicsBuffers()
        {
            int count = GetNeededBufferCount();
            if (count > 0)
            {
                globalIndirectBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, count, sizeof(GraphicsBuffer.IndirectDrawArgs));
                globalIndirectionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, count, sizeof(uint));
            }
        }

        /// <summary>
        /// Resize buffers only if the total number of volumes is above the buffer size or if the number
        /// of volumes is below the buffer size minus the chunk size
        /// </summary>
        unsafe void ResizeBuffersIfNeeded()
        {
            if (globalIndirectBuffer == null || !globalIndirectBuffer.IsValid())
                return;

            int count = GetNeededBufferCount();

            if (count > globalIndirectBuffer.count)
                Resize(count);
            if (count < globalIndirectBuffer.count - k_IndirectBufferChunkSize)
                Resize(count + k_IndirectBufferChunkSize);

            void Resize(int bufferCount)
            {
                Debug.Log("Resize buffers " + bufferCount);
                globalIndirectBuffer.Release();
                globalIndirectionBuffer.Release();
                globalIndirectBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, bufferCount, sizeof(GraphicsBuffer.IndirectDrawArgs));
                globalIndirectionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferCount, sizeof(uint));
            }
        }

        internal void CleanupGraphicsBuffers()
        {
            globalIndirectBuffer?.Release();
            globalIndirectionBuffer?.Release();
        }

        public bool ContainsVolume(LocalVolumetricFog volume) => m_Volumes.Contains(volume);

        public List<LocalVolumetricFog> PrepareLocalVolumetricFogData(CommandBuffer cmd, HDCamera currentCam)
        {
            //Update volumes
            float time = currentCam.time;
            int globalIndex = 0;
            foreach (LocalVolumetricFog volume in m_Volumes)
                volume.PrepareParameters(time, globalIndex++);

            return m_Volumes;
        }

        public bool IsInitialized() => globalIndirectBuffer != null && globalIndirectBuffer.IsValid();
    }
}
