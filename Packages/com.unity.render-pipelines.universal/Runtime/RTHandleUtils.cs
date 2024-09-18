using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    // a customized version of RenderGraphResourcePool from SRP core
    internal class RTHandleResourcePool
    {
        // Dictionary tracks resources by hash and stores resources with same hash in a List (list instead of a stack because we need to be able to remove stale allocations, potentially in the middle of the stack).
        // The list needs to be sorted otherwise you could get inconsistent resource usage from one frame to another.
        protected Dictionary<int, SortedList<int, (RTHandle resource, int frameIndex)>> m_ResourcePool = new Dictionary<int, SortedList<int, (RTHandle resource, int frameIndex)>>();
        protected List<int> m_RemoveList = new List<int>(32); // Used to remove stale resources as there is no RemoveAll on SortedLists

        protected static int s_CurrentStaleResourceCount = 0;
        // Keep stale resources alive for 3 frames
        protected static int s_StaleResourceLifetime = 3;
        // Store max 32 rtHandles
        // 1080p * 32bpp * 32 = 265.4mb
        protected static int s_StaleResourceMaxCapacity = 32;

        /// <summary>
        /// Controls the resource pool's max stale resource capacity. 
        /// Increasing the capacity may have a negative impact on the memory usage.
        /// Increasing the capacity may reduce the runtime RTHandle realloc cost in multi view/multi camera setup.
        /// Setting capacity will purge the current pool. It is recommended to setup the capacity upfront and not changing it during the runtime.
        /// Setting capacity won't do anything if new capacity is the same to the current capacity.
        /// </summary>
        internal int staleResourceCapacity
        {
            get { return s_StaleResourceMaxCapacity; }
            set {
                if (s_StaleResourceMaxCapacity != value)
                {
                    s_StaleResourceMaxCapacity = value;
                    Cleanup();
                }
            }
        }

        // Add no longer used resouce to pool
        // Return true if resource is added to pool successfully, return false otherwise.
        internal bool AddResourceToPool(in TextureDesc texDesc, RTHandle resource, int currentFrameIndex)
        {
            if (s_CurrentStaleResourceCount >= s_StaleResourceMaxCapacity)
                return false;

            int hashCode = GetHashCodeWithNameHash(texDesc);

            if (!m_ResourcePool.TryGetValue(hashCode, out var list))
            {
                // Init list with max capacity to avoid runtime GC.Alloc when calling list.Add(resize list)
                list = new SortedList<int, (RTHandle resource, int frameIndex)>(s_StaleResourceMaxCapacity);
                m_ResourcePool.Add(hashCode, list);
            }

            list.Add(resource.GetInstanceID(), (resource, currentFrameIndex));
            s_CurrentStaleResourceCount++;

            return true;
        }

        // Get resource from the pool using TextureDesc as key
        // Return true if resource successfully retried resource from the pool, return false otherwise.
        internal bool TryGetResource(in TextureDesc texDesc, out RTHandle resource, bool usepool = true)
        {
            int hashCode = GetHashCodeWithNameHash(texDesc);
            if (usepool && m_ResourcePool.TryGetValue(hashCode, out SortedList<int, (RTHandle resource, int frameIndex)> list) && list.Count > 0)
            {
                resource = list.Values[list.Count - 1].resource;
                list.RemoveAt(list.Count - 1); // O(1) since it's the last element.
                s_CurrentStaleResourceCount--;
                return true;
            }

            resource = null;
            return false;
        }

        // Release all resources in pool. 
        internal void Cleanup()
        {
            foreach (var kvp in m_ResourcePool)
            {
                foreach (var res in kvp.Value)
                {
                    res.Value.resource.Release();
                }
            }
            m_ResourcePool.Clear();

            s_CurrentStaleResourceCount = 0;
        }

        static protected bool ShouldReleaseResource(int lastUsedFrameIndex, int currentFrameIndex)
        {
            // We need to have a delay of a few frames before releasing resources for good.
            // Indeed, when having multiple off-screen cameras, they are rendered in a separate SRP render call and thus with a different frame index than main camera
            // This causes texture to be deallocated/reallocated every frame if the two cameras don't need the same buffers.
            return (lastUsedFrameIndex + s_StaleResourceLifetime) < currentFrameIndex;
        }

        // Release resources that are not used in last couple frames.
        internal void PurgeUnusedResources(int currentFrameIndex)
        {
            // Update the frame index for the lambda. Static because we don't want to capture.
            m_RemoveList.Clear();

            foreach (var kvp in m_ResourcePool)
            {
                // WARNING: No foreach here. Sorted list GetEnumerator generates garbage...
                var list = kvp.Value;
                var keys = list.Keys;
                var values = list.Values;
                for (int i = 0; i < list.Count; ++i)
                {
                    var value = values[i];
                    if (ShouldReleaseResource(value.frameIndex, currentFrameIndex))
                    {
                        value.resource.Release();
                        m_RemoveList.Add(keys[i]);
                        s_CurrentStaleResourceCount--;
                    }
                }

                foreach (var key in m_RemoveList)
                    list.Remove(key);
            }
        }

        internal void LogDebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("RTHandleResourcePool for frame {0}, Total stale resources {1}", Time.frameCount, s_CurrentStaleResourceCount);
            sb.AppendLine();

            foreach (var kvp in m_ResourcePool)
            {
                var list = kvp.Value;
                var keys = list.Keys;
                var values = list.Values;
                for (int i = 0; i < list.Count; ++i)
                {
                    var value = values[i];
                    sb.AppendFormat("Resrouce in pool: Name {0} Last active frame index {1} Size {2} x {3} x {4}",
                        value.resource.name,
                        value.frameIndex,
                        value.resource.rt.descriptor.width,
                        value.resource.rt.descriptor.height,
                        value.resource.rt.descriptor.volumeDepth
                        );
                    sb.AppendLine();
                }
            }

            Debug.Log(sb);
        }

        // NOTE: Only allow reusing resource with the same name.
        // This is because some URP code uses texture name as key to bind input texture (GBUFFER_2). Different name will result in URP bind texture to different shader input slot.
        // Ideally if URP code uses shaderPropertyID(instead of name string), we can relax the restriction here.
        internal int GetHashCodeWithNameHash(in TextureDesc texDesc)
        {
            int hashCode = texDesc.GetHashCode();
            hashCode = hashCode * 23 + texDesc.name.GetHashCode();

            return hashCode;
        }

        internal static TextureDesc CreateTextureDesc(RenderTextureDescriptor desc,
            TextureSizeMode textureSizeMode = TextureSizeMode.Explicit, int anisoLevel = 1, float mipMapBias = 0,
            FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Clamp, string name = "")
        {            
            Assertions.Assert.IsFalse(desc.graphicsFormat != GraphicsFormat.None && desc.depthStencilFormat != GraphicsFormat.None,
                "The RenderTextureDescriptor used to create a TextureDesc contains both graphicsFormat and depthStencilFormat which is not allowed.");
            
            var format = (desc.depthStencilFormat != GraphicsFormat.None) ? desc.depthStencilFormat : desc.graphicsFormat;

            TextureDesc rgDesc = new TextureDesc(desc.width, desc.height);
            rgDesc.sizeMode = textureSizeMode;
            rgDesc.slices = desc.volumeDepth;
            rgDesc.format = format;
            rgDesc.filterMode = filterMode;
            rgDesc.wrapMode = wrapMode;
            rgDesc.dimension = desc.dimension;
            rgDesc.enableRandomWrite = desc.enableRandomWrite;
            rgDesc.useMipMap = desc.useMipMap;
            rgDesc.autoGenerateMips = desc.autoGenerateMips;
            rgDesc.isShadowMap = desc.shadowSamplingMode != ShadowSamplingMode.None;
            rgDesc.anisoLevel = anisoLevel;
            rgDesc.mipMapBias = mipMapBias;
            rgDesc.msaaSamples = (MSAASamples)desc.msaaSamples;
            rgDesc.bindTextureMS = desc.bindMS;
            rgDesc.useDynamicScale = desc.useDynamicScale;
            rgDesc.memoryless = RenderTextureMemoryless.None;
            rgDesc.vrUsage = VRTextureUsage.None;
            rgDesc.name = name;

            return rgDesc;
        }
    }
}
