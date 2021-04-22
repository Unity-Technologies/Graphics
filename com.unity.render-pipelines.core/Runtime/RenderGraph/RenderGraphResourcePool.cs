using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    abstract class IRenderGraphResourcePool
    {
        public abstract void PurgeUnusedResources(int currentFrameIndex);
        public abstract void Cleanup();
        public abstract void CheckFrameAllocation(bool onException, int frameIndex);
        public abstract void LogResources(RenderGraphLogger logger);
    }

    abstract class RenderGraphResourcePool<Type> : IRenderGraphResourcePool where Type : class
    {
        // Dictionary tracks resources by hash and stores resources with same hash in a List (list instead of a stack because we need to be able to remove stale allocations, potentially in the middle of the stack).
        protected  Dictionary<int, List<(Type resource, int frameIndex)>> m_ResourcePool = new Dictionary<int, List<(Type resource, int frameIndex)>>();

        // This list allows us to determine if all resources were correctly released in the frame.
        // This is useful to warn in case of user error or avoid leaks when a render graph execution errors occurs for example.
        List<(int, Type)> m_FrameAllocatedResources = new List<(int, Type)>();

        protected static int s_CurrentFrameIndex;
        const int kStaleResourceLifetime = 10;

        // Release the GPU resource itself
        protected abstract void ReleaseInternalResource(Type res);
        protected abstract string GetResourceName(Type res);
        protected abstract long GetResourceSize(Type res);
        protected abstract string GetResourceTypeName();

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

        public override void Cleanup()
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
            if (hash != -1)
                m_FrameAllocatedResources.Add((hash, value));
        }

        public void UnregisterFrameAllocation(int hash, Type value)
        {
            if (hash != -1)
                m_FrameAllocatedResources.Remove((hash, value));
        }

        public override void CheckFrameAllocation(bool onException, int frameIndex)
        {
            // In case of exception we need to release all resources to the pool to avoid leaking.
            // If it's not an exception then it's a user error so we need to log the problem.
            if (m_FrameAllocatedResources.Count != 0)
            {
                string logMessage = "";
                if (!onException)
                    logMessage = $"RenderGraph: Not all resources of type {GetResourceTypeName()} were released. This can be caused by a resources being allocated but never read by any pass.";

                foreach (var value in m_FrameAllocatedResources)
                {
                    if (!onException)
                        logMessage = $"{logMessage}\n\t{GetResourceName(value.Item2)}";
                    ReleaseResource(value.Item1, value.Item2, frameIndex);
                }

                Debug.LogWarning(logMessage);
            }

            // If an error occurred during execution, it's expected that textures are not all released so we clear the tracking list.
            m_FrameAllocatedResources.Clear();
        }

        struct ResourceLogInfo
        {
            public string name;
            public long size;
        }

        public override void LogResources(RenderGraphLogger logger)
        {
            List<ResourceLogInfo> allocationList = new List<ResourceLogInfo>();
            foreach (var kvp in m_ResourcePool)
            {
                foreach (var res in kvp.Value)
                {
                    allocationList.Add(new ResourceLogInfo { name = GetResourceName(res.resource), size = GetResourceSize(res.resource) });
                }
            }

            logger.LogLine($"== {GetResourceTypeName()} Resources ==");

            allocationList.Sort((a, b) => a.size < b.size ? 1 : -1);
            int index = 0;
            float total = 0;
            foreach (var element in allocationList)
            {
                float size = element.size / (1024.0f * 1024.0f);
                total += size;
                logger.LogLine($"[{index++:D2}]\t[{size:0.00} MB]\t{element.name}");
            }

            logger.LogLine($"\nTotal Size [{total:0.00}]");
        }

        static protected bool ShouldReleaseResource(int lastUsedFrameIndex, int currentFrameIndex)
        {
            // We need to have a delay of a few frames before releasing resources for good.
            // Indeed, when having multiple off-screen cameras, they are rendered in a separate SRP render call and thus with a different frame index than main camera
            // This causes texture to be deallocated/reallocated every frame if the two cameras don't need the same buffers.
            return (lastUsedFrameIndex + kStaleResourceLifetime) < currentFrameIndex;
        }
    }
}
