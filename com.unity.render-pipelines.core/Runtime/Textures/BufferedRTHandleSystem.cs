using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Implement a multiple buffering for RenderTextures.
    /// </summary>
    /// <exemple>
    /// <code>
    /// enum BufferType
    /// {
    ///     Color,
    ///     Depth
    /// }
    ///
    /// void Render()
    /// {
    ///     var camera = GetCamera();
    ///     var buffers = GetFrameHistoryBuffersFor(camera);
    ///
    ///     // Set reference size in case the rendering size changed this frame
    ///     buffers.SetReferenceSize(
    ///         GetCameraWidth(camera), GetCameraHeight(camera),
    ///         GetCameraUseMSAA(camera), GetCameraMSAASamples(camera)
    ///     );
    ///     buffers.Swap();
    ///
    ///     var currentColor = buffer.GetFrameRT((int)BufferType.Color, 0);
    ///     if (currentColor == null) // Buffer was not allocated
    ///     {
    ///         buffer.AllocBuffer(
    ///             (int)BufferType.Color,      // Color buffer id
    ///             ColorBufferAllocator,       // Custom functor to implement allocation
    ///             2                           // Use 2 RT for this buffer for double buffering
    ///         );
    ///         currentColor = buffer.GetFrameRT((int)BufferType.Color, 0);
    ///     }
    ///
    ///     var previousColor = buffers.GetFrameRT((int)BufferType.Color, 1);
    ///
    ///     // Use previousColor and write into currentColor
    /// }
    /// </code>
    /// </exemple>
    public class BufferedRTHandleSystem : IDisposable
    {
        Dictionary<int, RTHandle[]> m_RTHandles = new Dictionary<int, RTHandle[]>();

        RTHandleSystem m_RTHandleSystem = new RTHandleSystem();
        bool m_DisposedValue = false;

        /// <summary>
        /// Maximum allocated width of the Buffered RTHandle System
        /// </summary>
        public int maxWidth { get { return m_RTHandleSystem.GetMaxWidth(); } }
        /// <summary>
        /// Maximum allocated height of the Buffered RTHandle System
        /// </summary>
        public int maxHeight { get { return m_RTHandleSystem.GetMaxHeight(); } }
        /// <summary>
        /// Current properties of the Buffered RTHandle System
        /// </summary>
        public RTHandleProperties rtHandleProperties { get { return m_RTHandleSystem.rtHandleProperties; } }

        /// <summary>
        /// Return the frame RT or null.
        /// </summary>
        /// <param name="bufferId">Defines the buffer to use.</param>
        /// <param name="frameIndex"></param>
        /// <returns>The frame RT or null when the <paramref name="bufferId"/> was not previously allocated (<see cref="BufferedRTHandleSystem.AllocBuffer(int, Func{RTHandleSystem, int, RTHandle}, int)" />).</returns>
        public RTHandle GetFrameRT(int bufferId, int frameIndex)
        {
            if (!m_RTHandles.ContainsKey(bufferId))
                return null;

            Assert.IsTrue(frameIndex >= 0 && frameIndex < m_RTHandles[bufferId].Length);

            return m_RTHandles[bufferId][frameIndex];
        }

        /// <summary>
        /// Allocate RT handles for a buffer.
        /// </summary>
        /// <param name="bufferId">The buffer to allocate.</param>
        /// <param name="allocator">The functor to use for allocation.</param>
        /// <param name="bufferCount">The number of RT handles for this buffer.</param>
        public void AllocBuffer(
            int bufferId,
            Func<RTHandleSystem, int, RTHandle> allocator,
            int bufferCount
            )
        {
            var buffer = new RTHandle[bufferCount];
            m_RTHandles.Add(bufferId, buffer);

            // First is autoresized
            buffer[0] = allocator(m_RTHandleSystem, 0);

            // Other are resized on demand
            for (int i = 1, c = buffer.Length; i < c; ++i)
            {
                buffer[i] = allocator(m_RTHandleSystem, i);
                m_RTHandleSystem.SwitchResizeMode(buffer[i], RTHandleSystem.ResizeMode.OnDemand);
            }
        }

        /// <summary>
        /// Release a buffer
        /// </summary>
        /// <param name="bufferId">Id of the buffer that needs to be released.</param>
        public void ReleaseBuffer(int bufferId)
        {
            if (m_RTHandles.TryGetValue(bufferId, out var buffers))
            {
                foreach (var rt in buffers)
                    m_RTHandleSystem.Release(rt);
            }

            m_RTHandles.Remove(bufferId);
        }

        /// <summary>
        /// Swap buffers Set the reference size for this RT Handle System (<see cref="RTHandleSystem.SetReferenceSize(int, int, bool, MSAASamples)"/>)
        /// </summary>
        /// <param name="width">The width of the RTs of this buffer.</param>
        /// <param name="height">The height of the RTs of this buffer.</param>
        /// <param name="msaaSamples">Number of MSAA samples for this buffer.</param>
        public void SwapAndSetReferenceSize(int width, int height, MSAASamples msaaSamples)
        {
            Swap();
            m_RTHandleSystem.SetReferenceSize(width, height, msaaSamples);
        }

        /// <summary>
        /// Reset the reference size of the system and reallocate all textures.
        /// </summary>
        /// <param name="width">New width.</param>
        /// <param name="height">New height.</param>
        public void ResetReferenceSize(int width, int height)
        {
            m_RTHandleSystem.ResetReferenceSize(width, height);
        }

        /// <summary>
        /// Queries the number of RT handle buffers allocated for a buffer ID.
        /// </summary>
        /// <param name="bufferId">The buffer ID to query.</param>
        public int GetNumFramesAllocated(int bufferId)
        {
            if (!m_RTHandles.ContainsKey(bufferId))
                return 0;

            return m_RTHandles[bufferId].Length;
        }

        void Swap()
        {
            foreach (var item in m_RTHandles)
            {
                // Do not index out of bounds...
                if (item.Value.Length > 1)
                {
                    var nextFirst = item.Value[item.Value.Length - 1];
                    for (int i = 0, c = item.Value.Length - 1; i < c; ++i)
                        item.Value[i + 1] = item.Value[i];
                    item.Value[0] = nextFirst;

                    // First is autoresize, other are on demand
                    m_RTHandleSystem.SwitchResizeMode(item.Value[0], RTHandleSystem.ResizeMode.Auto);
                    m_RTHandleSystem.SwitchResizeMode(item.Value[1], RTHandleSystem.ResizeMode.OnDemand);
                }
                else
                {
                    m_RTHandleSystem.SwitchResizeMode(item.Value[0], RTHandleSystem.ResizeMode.Auto);
                }
            }
        }

        void Dispose(bool disposing)
        {
            if (!m_DisposedValue)
            {
                if (disposing)
                {
                    ReleaseAll();
                    m_RTHandleSystem.Dispose();
                    m_RTHandleSystem = null;
                }

                m_DisposedValue = true;
            }
        }

        /// <summary>
        /// Dispose implementation
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Deallocate and clear all buffers.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var item in m_RTHandles)
            {
                for (int i = 0, c = item.Value.Length; i < c; ++i)
                {
                    m_RTHandleSystem.Release(item.Value[i]);
                }
            }
            m_RTHandles.Clear();
        }
    }
}
