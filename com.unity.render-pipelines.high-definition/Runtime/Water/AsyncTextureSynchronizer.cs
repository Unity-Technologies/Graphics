using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    internal enum ReadbackCode
    {
        UpToDate,
        Enqueued,
        Busy
    }

    internal class AsyncTextureSynchronizer<T> where T : struct
    {
        // The pair of buffers that allows us to keep doing the async readback "permanently"
        NativeArray<T>[] m_InternalBuffers = new NativeArray<T>[2];
        int2 m_CurrentResolution = new int2(0, 0);

        // Tracker of the current "valid" buffer
        int m_CurrentBuffer = 1;

        // GPU buffer that the synchronizer will read from
        RenderTexture m_InternalRT;

        // Tracker of hash of the last request
        int m_TargetTextureHash = 0;

        // Is there any job on going right now?
        bool m_CurrentlyOnGoingJob = false;

        // What is the format that is currently used
        GraphicsFormat m_InternalGraphicsFormat;

        // Callback for the end of the async readback
        public struct AsyncTextureSynchronizerCallBack
        {
            public AsyncTextureSynchronizer<T> ats;
            public void OnReceive(AsyncGPUReadbackRequest request)
            {
                if (!request.hasError)
                    ats.SwapCurrentBuffer();
                ats.m_CurrentlyOnGoingJob = false;
            }
        }
        AsyncTextureSynchronizerCallBack callback = new AsyncTextureSynchronizerCallBack();

        public AsyncTextureSynchronizer(GraphicsFormat format)
        {
            callback.ats = this;
            m_InternalGraphicsFormat = format;
        }

        public NativeArray<T> CurrentBuffer()
        {
            return m_InternalBuffers[m_CurrentBuffer];
        }

        public int2 CurrentResolution()
        {
            return m_CurrentResolution;
        }

        void SwapCurrentBuffer()
        {
            m_CurrentBuffer = (m_CurrentBuffer + 1) % 2;
        }

        int NextBufferIndex()
        {
            return (m_CurrentBuffer + 1) % 2;
        }

        void ValidateNativeBuffer(ref NativeArray<T> buffer, int textureSize)
        {
            if (!buffer.IsCreated || buffer.Length != textureSize)
            {
                if (buffer.IsCreated)
                    buffer.Dispose();
                buffer = new NativeArray<T>(textureSize, Allocator.Persistent);
            }
        }

        void ValidateResources(int width, int height)
        {
            int textureSize = width * height;
            ValidateNativeBuffer(ref m_InternalBuffers[0], textureSize);
            ValidateNativeBuffer(ref m_InternalBuffers[1], textureSize);

            // Make sure the GPU buffer is the right size
            if (m_InternalRT == null || m_InternalRT.width != width || m_InternalRT.height != height)
            {
                if (m_InternalRT != null)
                    m_InternalRT.Release();
                m_InternalRT = new RenderTexture(width, height, 1, m_InternalGraphicsFormat);
            }
            m_CurrentResolution = int2(width, height);
        }

        public ReadbackCode EnqueueRequest(CommandBuffer cmd, Texture targetTexture, bool intermediateBlit)
        {
            // If the texture hash is already up to date, we have nothing to do
            int currentHash = CoreUtils.GetTextureHash(targetTexture);
            if (currentHash == m_TargetTextureHash)
                return ReadbackCode.UpToDate;

            // A job is already going on, we need to wait before we do anything
            if (m_CurrentlyOnGoingJob)
                return ReadbackCode.Busy;

            // Ok we are now inside a read back job
            m_CurrentlyOnGoingJob = true;
            m_TargetTextureHash = currentHash;

            // No job is going on, so we can free the resources if needed
            ValidateResources(targetTexture.width, targetTexture.height);

            // Decompress and pick a single channel
            Texture targetRT = null;
            if (intermediateBlit)
            {
                cmd.Blit(targetTexture, m_InternalRT);
                targetRT = m_InternalRT;
            }
            else
            {
                targetRT = targetTexture;
            }

            // Grab the next buffer
            NativeArray<T> nextBuffer = m_InternalBuffers[NextBufferIndex()];

#if UNITY_EDITOR
            // TODO: Remove this when the bug is fixed
            AtomicSafetyHandle ash = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(nextBuffer);
            AtomicSafetyHandle.CheckReadAndThrow(ash);
            AtomicSafetyHandle.CheckDeallocateAndThrow(ash);
#endif

            // Enqueue the job
            cmd.RequestAsyncReadbackIntoNativeArray(ref nextBuffer, targetRT, 0, m_InternalGraphicsFormat, callback.OnReceive);

            // Notify that we enqueued
            return ReadbackCode.Enqueued;
        }

        internal void ReleaseATSResources()
        {
            // If a job is still ongoing, we need to wait that it is done before we free the resources
            if (m_CurrentlyOnGoingJob)
                AsyncGPUReadback.WaitAllRequests();

            if (m_InternalBuffers[0].IsCreated)
                m_InternalBuffers[0].Dispose();

            if (m_InternalBuffers[1].IsCreated)
                m_InternalBuffers[1].Dispose();

            if (m_InternalRT != null)
                m_InternalRT.Release();

            m_CurrentResolution = 0;
        }
    }
}
