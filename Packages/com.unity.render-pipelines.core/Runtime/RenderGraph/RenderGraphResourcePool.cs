using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering.RenderGraphModule
{
    abstract class IRenderGraphResourcePool
    {
        public abstract void PurgeUnusedResources(int currentFrameIndex);
        public abstract void Cleanup();
        public abstract void CheckFrameAllocation(bool onException, int frameIndex);
        public abstract void LogResources(RenderGraphLogger logger);

        // When disabled, resources released in the current execution cannot be reused until the next execution.
        // This is useful for Frame Debugger to ensure each pass uses unique resources within an execution.
        public bool IntraFrameMemoryAliasing { get; set; } = true;
    }

    /// <summary>
    /// Encapsulates a pooled resource along with its frame and execution tracking metadata.
    /// This allows the pool to track when resources were released for proper reuse and purging decisions.
    /// </summary>
    readonly struct PooledResourceEntry<Type> where Type : class
    {
        public readonly Type resource;
        public readonly int frameIndex;
        public readonly int executionCount;

        public PooledResourceEntry(Type resource, int frameIndex, int executionCount)
        {
            this.resource = resource;
            this.frameIndex = frameIndex;
            this.executionCount = executionCount;
        }
    }

    abstract class RenderGraphResourcePool<Type> : IRenderGraphResourcePool where Type : class
    {
        // Dictionary tracks resources by hash and stores resources with same hash in a List (list instead of a stack because we need to be able to remove stale allocations, potentially in the middle of the stack).
        // The list needs to be sorted otherwise you could get inconsistent resource usage from one frame to another.
        protected Dictionary<int, SortedList<int, PooledResourceEntry<Type>>> m_ResourcePool = new Dictionary<int, SortedList<int, PooledResourceEntry<Type>>>();

        // This list allows us to determine if all resources were correctly released in the frame when validity checks are enabled.
        // This is useful to warn in case of user error or avoid leaks when a render graph execution error occurs for example.
        List<(int, Type)> m_FrameAllocatedResources = new List<(int, Type)>();
        const int kStaleResourceLifetime = 10;

        // Release the GPU resource itself
        protected abstract void ReleaseInternalResource(Type res);
        protected abstract string GetResourceName(in Type res);
        protected abstract long GetResourceSize(in Type res);
        protected abstract string GetResourceTypeName();
        protected abstract int GetSortIndex(Type res);

        public void ReleaseResource(int hash, Type resource, int currentFrameIndex, int currentExecutionCount)
        {
            if (!m_ResourcePool.TryGetValue(hash, out var list))
            {
                list = new SortedList<int, PooledResourceEntry<Type>>();
                m_ResourcePool.Add(hash, list);
            }

            list.Add(GetSortIndex(resource), new PooledResourceEntry<Type>(resource, currentFrameIndex, currentExecutionCount));
        }

        public bool TryGetResource(int hashCode, out Type resource, int currentFrameIndex, int currentExecutionCount)
        {
            if (m_ResourcePool.TryGetValue(hashCode, out SortedList<int, PooledResourceEntry<Type>> list) && list.Count > 0)
            {
                // When "intra frame memory aliasing" mode is disabled, skip resources that were released in the current execution.
                // They will only be available for reuse in the next execution.
                if (!IntraFrameMemoryAliasing)
                {
                    // Iterate from the end to find a resource not from the current execution.
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var entry = list.Values[i];

                        // Allow reuse if the resource was released in a previous frame (normal case),
                        // or in a previous execution within the same frame (Frame Debugger cascade-repaints).
                        // This handles both standard frame-to-frame reuse and rapid repaints in Frame Debugger.
                        if (entry.frameIndex < currentFrameIndex || entry.executionCount < currentExecutionCount)
                        {
                            resource = entry.resource;
                            list.RemoveAt(i);
                            return true;
                        }
                    }
                    // All available resources are from the current execution, so we need to create a new one.
                }
                else
                {
                    // Normal mode: take the most recent resource.
                    var index = list.Count - 1;
                    resource = list.Values[index].resource;
                    list.RemoveAt(index); // O(1) since it's the last element.
                    return true;
                }
            }

            resource = null;
            return false;
        }

        public override void Cleanup()
        {
            // Removing the actual graphics resources
            foreach (var kvp in m_ResourcePool)
            {
                foreach (var res in kvp.Value)
                {
                    ReleaseInternalResource(res.Value.resource);
                }
            }

            // Clearing the lists and the pool itself
            m_ResourcePool.Clear();

            // Clearing it, if not done already
            m_FrameAllocatedResources.Clear();
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void RegisterFrameAllocation(int hash, Type value)
        {
            if (RenderGraph.enableValidityChecks && hash != -1)
                m_FrameAllocatedResources.Add((hash, value));
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public void UnregisterFrameAllocation(int hash, Type value)
        {
            if (RenderGraph.enableValidityChecks && hash != -1)
                m_FrameAllocatedResources.Remove((hash, value));
        }

        public override void CheckFrameAllocation(bool onException, int frameIndex)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR // conditional not working with override
            if (RenderGraph.enableValidityChecks)
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
                        // When releasing on exception/error, use -1 as executionCount marker.
                        // This ensures the resource goes back to the pool but won't interfere with normal execution tracking.
                        ReleaseResource(value.Item1, value.Item2, frameIndex, -1);
                    }

                    if (!onException) // If onException is true, logMessage is ""
                        Debug.LogWarning(logMessage);
                }

                // If an error occurred during execution, it's expected that textures are not all released so we clear the tracking list.
                m_FrameAllocatedResources.Clear();
            }
#endif
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
                    allocationList.Add(new ResourceLogInfo { name = GetResourceName(res.Value.resource), size = GetResourceSize(res.Value.resource) });
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

        public float GetMemorySizeInMB()
        {
            float totalSize = 0;

            foreach (var kvp in m_ResourcePool)
            {
                foreach (var res in kvp.Value)
                {
                    totalSize += GetResourceSize(res.Value.resource) / (1024.0f * 1024.0f);
                }
            }

            return totalSize;
        }

        public int GetNumResourcesAvailable()
        {
            int totalResources = 0;

            foreach (var kvp in m_ResourcePool)
            {
                totalResources += kvp.Value.Count;
            }

            return totalResources;
        }

        static List<int> s_ToRemoveList = new List<int>(32);

        public override void PurgeUnusedResources(int currentFrameIndex)
        {
            foreach (var kvp in m_ResourcePool)
            {
                s_ToRemoveList.Clear();

                var list = kvp.Value;
                var keys = list.Keys;
                var values = list.Values;
                for (int i = 0; i < list.Count; ++i)
                {
                    var value = values[i];
                    var key = keys[i];

                    // When a resource hasn't been used for a few frames, we release it for good to reduce memory footprint.
                    // We wait a few frames because when having multiple off-screen cameras,
                    // they are rendered in a separate SRP render call and thus with a different frame index than main camera
                    // This causes texture to be deallocated/reallocated every frame if the two cameras don't need the same buffers.
                    if (value.frameIndex + kStaleResourceLifetime < currentFrameIndex)
                    {
                        ReleaseInternalResource(value.resource);
                        // Adding stale resource to the remove list
                        s_ToRemoveList.Add(key);
                    }
                }

                // Removing the stale resource from the pool
                for (int j = 0; j < s_ToRemoveList.Count; ++j)
                    list.Remove(s_ToRemoveList[j]);
            }
        }
    }
}
