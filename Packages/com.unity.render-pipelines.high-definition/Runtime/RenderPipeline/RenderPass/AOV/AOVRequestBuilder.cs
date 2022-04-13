using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Utility to build frame passes.</summary>
    public class AOVRequestBuilder : IDisposable
    {
        // Owned
        private List<AOVRequestData> m_AOVRequestDataData;

        /// <summary>Add a AOV request.</summary>
        /// <param name="settings">Settings to use for this frame pass.</param>
        /// <param name="bufferAllocator">An allocator for each buffer.</param>
        /// <param name="includedLightList">If non null, only these lights will be rendered, if none, all lights will be rendered.</param>
        /// <param name="aovBuffers">A list of buffers to use.</param>
        /// <param name="callback">A callback that can use the requested buffers once the rendering has completed.</param>
        /// <returns></returns>
        public AOVRequestBuilder Add(
            AOVRequest settings,
            AOVRequestBufferAllocator bufferAllocator,
            List<GameObject> includedLightList,
            AOVBuffers[] aovBuffers,
            FramePassCallback callback
        )
        {
            (m_AOVRequestDataData ?? (m_AOVRequestDataData = ListPool<AOVRequestData>.Get())).Add(
                new AOVRequestData(settings, bufferAllocator, includedLightList, aovBuffers, callback));
            return this;
        }

        /// <summary>Add a AOV request.</summary>
        /// <param name="settings">Settings to use for this frame pass.</param>
        /// <param name="bufferAllocator">An allocator for each buffer.</param>
        /// <param name="includedLightList">If non null, only these lights will be rendered, if none, all lights will be rendered.</param>
        /// <param name="aovBuffers">A list of buffers to use.</param>
        /// <param name="customPassAovBuffers">A list of custom passes to captured.</param>
        /// <param name="customPassbufferAllocator">An allocator for each custom pass buffer.</param>
        /// <param name="callback">A callback that can use the requested buffers once the rendering has completed.</param>
        /// <returns></returns>
        public AOVRequestBuilder Add(
            AOVRequest settings,
            AOVRequestBufferAllocator bufferAllocator,
            List<GameObject> includedLightList,
            AOVBuffers[] aovBuffers,
            CustomPassAOVBuffers[] customPassAovBuffers,
            AOVRequestCustomPassBufferAllocator customPassbufferAllocator,
            FramePassCallbackEx callback
        )
        {
            (m_AOVRequestDataData ?? (m_AOVRequestDataData = ListPool<AOVRequestData>.Get())).Add(
                new AOVRequestData(settings, bufferAllocator, includedLightList, aovBuffers, customPassAovBuffers, customPassbufferAllocator, callback));
            return this;
        }

        /// <summary>Build the frame passes. Allocated resources will be transferred to the returned value.</summary>
        /// <returns>The built collection.</returns>
        public AOVRequestDataCollection Build()
        {
            var result = new AOVRequestDataCollection(m_AOVRequestDataData);
            m_AOVRequestDataData = null;
            return result;
        }

        /// <summary>
        /// Dispose the builder.
        ///
        /// This is required when you don't call <see cref="Build"/>.
        /// </summary>
        public void Dispose()
        {
            if (m_AOVRequestDataData == null) return;
            ListPool<AOVRequestData>.Release(m_AOVRequestDataData);
            m_AOVRequestDataData = null;
        }
    }
}
