#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    // Main thread only. Coalescing pending changes by key is safe because an EntityId is never reused.
    static class SurfaceCacheEntityChangeBridge
    {
        static readonly Dictionary<EntityId, SurfaceCacheEntityInstanceRecord> s_PendingChanged = new();
        static readonly Dictionary<EntityId, Matrix4x4> s_PendingTransforms = new();
        static readonly HashSet<EntityId> s_PendingDestroyed = new();
        static bool s_ConsumerRegistered;
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
            ClearPending();
        }

        public static void UnregisterConsumer()
        {
            Debug.Assert(s_ConsumerRegistered);
            s_ConsumerRegistered = false;
            ClearPending();
        }

        public static void PushChanged(in SurfaceCacheEntityInstanceRecord record)
        {
            if (!IsConsumerPresent)
                return;

            s_PendingTransforms.Remove(record.Key);
            s_PendingChanged[record.Key] = record;
        }

        public static void PushTransformChanged(in SurfaceCacheEntityTransformRecord record)
        {
            if (!IsConsumerPresent)
                return;

            if (s_PendingChanged.TryGetValue(record.Key, out var pending))
            {
                pending.LocalToWorld = record.LocalToWorld;
                s_PendingChanged[record.Key] = pending;
            }
            else
            {
                s_PendingTransforms[record.Key] = record.LocalToWorld;
            }
        }

        public static void PushDestroyed(in EntityId key)
        {
            if (!IsConsumerPresent)
                return;

            s_PendingChanged.Remove(key);
            s_PendingTransforms.Remove(key);
            s_PendingDestroyed.Add(key);
        }

        public static void Drain(
            List<SurfaceCacheEntityInstanceRecord> changed,
            List<SurfaceCacheEntityTransformRecord> transformChanged,
            List<EntityId> destroyed)
        {
            foreach (var record in s_PendingChanged.Values)
            {
                changed.Add(record);
            }

            foreach (var pair in s_PendingTransforms)
            {
                transformChanged.Add(new SurfaceCacheEntityTransformRecord { Key = pair.Key, LocalToWorld = pair.Value });
            }

            foreach (var key in s_PendingDestroyed)
            {
                destroyed.Add(key);
            }

            ClearPending();
        }

        static void ClearPending()
        {
            s_PendingChanged.Clear();
            s_PendingTransforms.Clear();
            s_PendingDestroyed.Clear();
        }
    }

    sealed class EntityChangeSource : IDisposable
    {
        static readonly Unity.Profiling.ProfilerMarker k_CollectMarker = new("SurfaceCache.EntityChangeCollection");

        readonly List<SurfaceCacheEntityInstanceRecord> _changed = new();
        readonly List<SurfaceCacheEntityTransformRecord> _transformChanged = new();
        readonly List<EntityId> _destroyed = new();

        public EntityChangeSource()
        {
            SurfaceCacheEntityChangeBridge.RegisterConsumer();
        }

        public void CollectChanges(SurfaceCacheWorldChangeSet changeSet)
        {
            using var _ = k_CollectMarker.Auto();

            _changed.Clear();
            _transformChanged.Clear();
            _destroyed.Clear();
            SurfaceCacheEntityChangeBridge.Drain(_changed, _transformChanged, _destroyed);

            changeSet.EntityInstanceChangedList = _changed;
            changeSet.EntityInstanceTransformChangedList = _transformChanged;
            changeSet.EntityInstanceDestroyedList = _destroyed;
        }

        public void Dispose()
        {
            SurfaceCacheEntityChangeBridge.UnregisterConsumer();
        }
    }
}

#endif
