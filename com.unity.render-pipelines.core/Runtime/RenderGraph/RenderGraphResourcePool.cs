using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{

    abstract class RenderGraphResourcePool<Type> where Type : class
    {
        // Dictionary tracks resources by hash and stores resources with same hash in a List (list instead of a stack because we need to be able to remove stale allocations).
        Dictionary<int, List<(Type resource, int frameIndex)>> m_ResourcePool = new Dictionary<int, List<(Type resource, int frameIndex)>>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // Diagnostic only
        // This list allows us to determine if all resources were correctly released in the frame.
        List<(int, Type)> m_FrameAllocatedResources = new List<(int, Type)>();
#endif

        static int s_CurrentFrameIndex;

        // Release the GPU resource itself
        abstract protected void ReleaseInternalResource(Type res);
        abstract protected string GetResourceName(Type res);
        abstract protected string GetResourceTypeName();

        public void ReleaseResource(int hash, Type resource, int currentFrameIndex)
        {
            if (!m_ResourcePool.TryGetValue(hash, out var list))
            {
                list = new List<(Type resource, int frameIndex)>();
                m_ResourcePool.Add(hash, list);
            }

            list.Add((resource, currentFrameIndex));
        }

        public bool TryGetResource(int hashCode, out Type resource)
        {
            if (m_ResourcePool.TryGetValue(hashCode, out var list) && list.Count > 0)
            {
                resource = list[list.Count - 1].resource;
                list.RemoveAt(list.Count - 1); // O(1) since it's the last element.
                return true;
            }

            resource = null;
            return false;
        }

        public void PurgeUnusedResources(int currentFrameIndex)
        {
            // Update the frame index for the lambda. Static because we don't want to capture.
            s_CurrentFrameIndex = currentFrameIndex;

            foreach (var kvp in m_ResourcePool)
            {
                var list = kvp.Value;
                list.RemoveAll(obj =>
                {
                    if (obj.frameIndex < s_CurrentFrameIndex)
                    {
                        ReleaseInternalResource(obj.resource);
                        return true;
                    }
                    return false;
                });
            }
        }

        public void Cleanup()
        {
            foreach (var kvp in m_ResourcePool)
            {
                foreach (var res in kvp.Value)
                {
                    ReleaseInternalResource(res.resource);
                }
            }
        }

        public void RegisterFrameAllocation(int hash, Type value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (hash != -1)
                m_FrameAllocatedResources.Add((hash, value));
#endif
        }

        public void UnregisterFrameAllocation(int hash, Type value)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (hash != -1)
                m_FrameAllocatedResources.Remove((hash, value));
#endif
        }

        public void CheckFrameAllocation(bool onException, int frameIndex)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_FrameAllocatedResources.Count != 0 && !onException)
            {
                string logMessage = $"RenderGraph: Not all resources of type {GetResourceTypeName()} were released. This can be caused by a resources being allocated but never read by any pass.";

                foreach (var value in m_FrameAllocatedResources)
                {
                    logMessage = $"{logMessage}\n\t{GetResourceName(value.Item2)}";
                    ReleaseResource(value.Item1, value.Item2, frameIndex);
                }

                Debug.LogWarning(logMessage);
            }

            // If an error occurred during execution, it's expected that textures are not all released so we clear the tracking list.
            m_FrameAllocatedResources.Clear();
#endif
        }


        public void LogResources(RenderGraphLogger logger)
        {
            List<string> allocationList = new List<string>();
            foreach (var kvp in m_ResourcePool)
            {
                foreach (var res in kvp.Value)
                {
                    allocationList.Add(GetResourceName(res.resource));
                }
            }

            allocationList.Sort();
            int index = 0;
            foreach (var element in allocationList)
                logger.LogLine("[{0}] {1}", index++, element);
        }
    }

    class TexturePool : RenderGraphResourcePool<RTHandle>
    {
        protected override void ReleaseInternalResource(RTHandle res)
        {
            res.Release();
        }

        protected override string GetResourceName(RTHandle res)
        {
            return res.rt.name;
        }

        override protected string GetResourceTypeName()
        {
            return "Texture";
        }
    }

    class ComputeBufferPool : RenderGraphResourcePool<ComputeBuffer>
    {
        protected override void ReleaseInternalResource(ComputeBuffer res)
        {
            res.Release();
        }

        protected override string GetResourceName(ComputeBuffer res)
        {
            return "NameNotAvailable"; // res.name is a setter only :(
        }

        override protected string GetResourceTypeName()
        {
            return "ComputeBuffer";
        }

    }
}
