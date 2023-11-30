using System;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // NOTE: This might become SRPCameraHistory for unified history access in the future.

    /// <summary>
    /// URP camera history texture manager.
    /// </summary>
    public class UniversalCameraHistory : ICameraHistoryReadAccess, ICameraHistoryWriteAccess, IPerFrameHistoryAccessTracker, IDisposable
    {
        /// <summary>
        /// Number of frames to consider history valid.
        /// </summary>
        const int k_ValidVersionCount = 2;  // current frame + previous frame

        private static uint s_TypeCount = 0;
        private static class TypeId<T>
        {
            public static uint value = s_TypeCount++;
        }

        private struct Item
        {
            public ContextItem storage;

            // The last tick the type was requested.
            public int requestVersion;

            // The last tick the type was written to.
            public int writeVersion;

            public void Reset()
            {
                storage?.Reset();
                requestVersion = -k_ValidVersionCount;  // NOTE: must be invalid on frame 0
                writeVersion   = -k_ValidVersionCount;
            }
        }
        private Item[] m_Items = new Item[32];
        private int m_Version = 0;

        // A central storage for camera history textures.
        private BufferedRTHandleSystem m_HistoryTextures = new BufferedRTHandleSystem();

    #region SrpApi

        /// <summary>
        /// Request access to a history item.
        /// </summary>
        /// <typeparam name="Type">Type of the history item.</typeparam>
        public void RequestAccess<Type>() where Type : ContextItem
        {
            uint index = TypeId<Type>.value;

            // Resize
            if(index >= m_Items.Length)
            {
                var items = new Item[math.max(math.ceilpow2(s_TypeCount), m_Items.Length * 2)];
                for (var i = 0; i < m_Items.Length; i++)
                {
                    items[i] = m_Items[i];
                }
                m_Items = items;
            }

            m_Items[index].requestVersion = m_Version;
        }

        /// <summary>
        /// Obtain read access to a history item.
        /// Valid only if the item was requested and written this or the previous frame.
        /// </summary>
        /// <typeparam name="Type">Type of the history item.</typeparam>
        /// <returns>Instance of the history item if valid. Null otherwise.</returns>
        public Type GetHistoryForRead<Type>() where Type : ContextItem
        {
            uint index = TypeId<Type>.value;

            if (index >= m_Items.Length)
                return null;

            // If the Type wasn't written in previous or this frame, return null.
            // The Type design is expected to handle, current/previous/Nth frame history access via BufferedRTHandleSystem.
            if (!IsValid((int)index))
                return null;

            return (Type)m_Items[index].storage;
        }

        /// <summary>
        /// Check if a type was requested this frame.
        /// </summary>
        /// <typeparam name="Type">Type of the history item.</typeparam>
        /// <returns>True if an active request exists. False otherwise.</returns>
        public bool IsAccessRequested<Type>() where Type : ContextItem
        {
            uint index = TypeId<Type>.value;

            if (index >= m_Items.Length)
                return false;

            return IsValidRequest((int)index);
        }

        /// <summary>
        /// Obtain write access to a history item.
        /// Valid only if the item was requested this or the previous frame.
        /// Write access implies that the contents of the history item must be written.
        /// </summary>
        /// <typeparam name="Type">Type of the history item.</typeparam>
        /// <returns>Instance of the history item if valid. Null otherwise.</returns>
        public Type GetHistoryForWrite<Type>() where Type : ContextItem, new()
        {
            uint index = TypeId<Type>.value;

            if (index >= m_Items.Length)
                return null;

            if (!IsValidRequest((int)index)) // If the request is too old, return null.
                return null;

            // Create Type instance on the first use
            if (m_Items[index].storage == null)
            {
                ref var i = ref m_Items[index];
                i.storage = new Type();

                // If the convenience base class for BufferedRTHandleSystem is used, set the owner.
                if (i.storage is CameraHistoryItem hi)
                    hi.OnCreate(m_HistoryTextures, index);
            }

            // Assume the write for GetForWrite is done correctly by the caller.
            m_Items[index].writeVersion = m_Version;

            ContextItem item = m_Items[index].storage;
            return (Type)item;
        }

        /// <summary>
        /// Check if a type was written this frame.
        /// </summary>
        /// <typeparam name="Type">Type of the history item.</typeparam>
        /// <returns>True if write access was obtained this frame. False otherwise.</returns>
        public bool IsWritten<Type>() where Type : ContextItem
        {
            uint index = TypeId<Type>.value;

            if (index >= m_Items.Length)
                return false;

            return m_Items[index].writeVersion == m_Version;
        }

        /// <summary>
        /// Register external type request callbacks to this event.
        /// </summary>
        public event ICameraHistoryReadAccess.HistoryRequestDelegate OnGatherHistoryRequests;

    #endregion
    #region UrpApi

        internal UniversalCameraHistory()
        {
            // Init items with invalid versions.
            for(int i = 0; i < m_Items.Length; i++)
                m_Items[i].Reset();
        }

        /// <summary>
        /// Release all camera history textures on the GPU.
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < m_Items.Length; i++)
                m_Items[i].Reset();

            m_HistoryTextures.ReleaseAll();
        }

        // Query which types are needed for the registered system.
        internal void GatherHistoryRequests()
        {
            OnGatherHistoryRequests?.Invoke(this);
        }

        // A type is valid if it was requested this or the last frame.
        // Requesting access in the leaf classes, means we might be 1 frame late.
        private bool IsValidRequest(int i)
        {
            return ((m_Version - m_Items[i].requestVersion) < k_ValidVersionCount);
        }

        // A type is valid if it was requested and written to this or the last frame.
        // Requesting access in the leaf classes, means we might be 1 frame late.
        // NOTE: BufferedRTHandleSystem technically supports history of N length.
        //       We might need to keep the history for N frames.
        //       For now we expect that active history has the previous frame written.
        private bool IsValid(int i)
        {
            return ((m_Version - m_Items[i].writeVersion) < k_ValidVersionCount);
        }

        // Garbage collect old unused type instances and Reset them. The user is expected to free any GPU resources.
        internal void ReleaseUnusedHistory()
        {
            for (int i = 0; i < m_Items.Length; i++)
            {
                if (!IsValidRequest(i) && !IsValid(i))
                    m_Items[i].Reset();
            }

            // After collecting stale Types, start a new generation.
            m_Version++;
        }

        // Set the camera reference size for all history textures.
        internal void SwapAndSetReferenceSize(int cameraWidth, int cameraHeight)
        {
            m_HistoryTextures.SwapAndSetReferenceSize(cameraWidth, cameraHeight);
        }

    #endregion
    }
}
