#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    // Main thread only. Coalescing pending changes by key is safe because an EntityId is never reused.
    // Transform changes are appended rather than coalesced, so that producers can write them from a job.
    static class SurfaceCacheEntityChangeBridge
    {
        static readonly Dictionary<EntityId, SurfaceCacheEntityInstanceRecord> s_PendingChanged = new();
        static readonly HashSet<EntityId> s_PendingDestroyed = new();
        static NativeList<SurfaceCacheEntityTransformRecord> s_PendingTransforms;

        static bool s_ConsumerRegistered;
        static bool s_DrainInProgress;
        static uint s_ConsumerVersion;

        public static bool IsConsumerPresent => s_ConsumerRegistered;

        // Producers push their full state again when this changes, so a recreated consumer starts complete.
        public static uint ConsumerVersion => s_ConsumerVersion;

        public static void RegisterConsumer()
        {
            if (s_ConsumerRegistered)
                throw new InvalidOperationException("SurfaceCacheEntityChangeBridge supports only a single consumer.");

            s_ConsumerRegistered = true;
            s_ConsumerVersion++;
            s_PendingTransforms = new NativeList<SurfaceCacheEntityTransformRecord>(Allocator.Persistent);
            ClearPending();
        }

        public static void UnregisterConsumer()
        {
            Debug.Assert(s_ConsumerRegistered);
            Debug.Assert(!s_DrainInProgress, "UnregisterConsumer during a drain.");

            s_ConsumerRegistered = false;
            ClearPending();

            if (s_PendingTransforms.IsCreated)
                s_PendingTransforms.Dispose();
        }

        // Appended to by producer jobs. The caller must complete its jobs before returning. The assert
        // below catches acquiring across a drain, which is an ordering mistake rather than a race: this
        // bridge is main thread only, so there is nothing to lock against.
        public static NativeList<SurfaceCacheEntityTransformRecord> AcquireTransformChangedList()
        {
            Debug.Assert(s_ConsumerRegistered, "AcquireTransformChangedList without a consumer.");
            Debug.Assert(!s_DrainInProgress, "AcquireTransformChangedList during a drain.");
            return s_PendingTransforms;
        }

        public static void PushChanged(in SurfaceCacheEntityInstanceRecord record)
        {
            if (!IsConsumerPresent)
                return;

            s_PendingChanged[record.Key] = record;
        }

        public static void PushDestroyed(in EntityId key)
        {
            if (!IsConsumerPresent)
                return;

            s_PendingChanged.Remove(key);
            s_PendingDestroyed.Add(key);
        }

        // transformChanged aliases the bridge's own storage rather than copying it, so it is valid only
        // until EndDrain, and any push in between would invalidate it.
        public static void BeginDrain(
            List<SurfaceCacheEntityInstanceRecord> changed,
            List<EntityId> destroyed,
            out NativeArray<SurfaceCacheEntityTransformRecord> transformChanged)
        {
            Debug.Assert(!s_DrainInProgress, "BeginDrain without a matching EndDrain.");
            s_DrainInProgress = true;

            foreach (var record in s_PendingChanged.Values)
                changed.Add(record);

            foreach (var key in s_PendingDestroyed)
                destroyed.Add(key);

            if (s_PendingChanged.Count != 0 || s_PendingDestroyed.Count != 0)
                DropSupersededTransforms();

            transformChanged = s_PendingTransforms.AsArray();
        }

        public static void EndDrain()
        {
            Debug.Assert(s_DrainInProgress, "EndDrain without a matching BeginDrain.");
            s_DrainInProgress = false;
            ClearPending();
        }

        static void DropSupersededTransforms()
        {
            int kept = 0;
            for (int i = 0; i < s_PendingTransforms.Length; i++)
            {
                var key = s_PendingTransforms[i].Key;
                if (s_PendingChanged.ContainsKey(key) || s_PendingDestroyed.Contains(key))
                    continue;

                s_PendingTransforms[kept++] = s_PendingTransforms[i];
            }

            s_PendingTransforms.Length = kept;
        }

        static void ClearPending()
        {
            s_PendingChanged.Clear();
            s_PendingDestroyed.Clear();

            if (s_PendingTransforms.IsCreated)
                s_PendingTransforms.Clear();
        }
    }

    sealed class EntityChangeSource : IDisposable
    {
        static readonly Unity.Profiling.ProfilerMarker k_CollectMarker = new("SurfaceCache.EntityChangeCollection");

        readonly List<SurfaceCacheEntityInstanceRecord> _changed = new();
        readonly List<EntityId> _destroyed = new();

        public EntityChangeSource()
        {
            SurfaceCacheEntityChangeBridge.RegisterConsumer();
        }

        public void CollectChanges(SurfaceCacheWorldChangeSet changeSet)
        {
            using var _ = k_CollectMarker.Auto();

            _changed.Clear();
            _destroyed.Clear();
            SurfaceCacheEntityChangeBridge.BeginDrain(_changed, _destroyed, out var transformChanged);

            changeSet.EntityInstanceChangedList = _changed;
            changeSet.EntityInstanceTransformChangedList = transformChanged;
            changeSet.EntityInstanceDestroyedList = _destroyed;
        }

        public void EndCollectChanges()
        {
            SurfaceCacheEntityChangeBridge.EndDrain();
        }

        public void Dispose()
        {
            SurfaceCacheEntityChangeBridge.UnregisterConsumer();
        }
    }
}

#endif
