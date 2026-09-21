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
        // Transforms below this index were retained by an aborted drain and predate any later push.
        static int s_RetainedTransformCount;

        public static bool IsConsumerPresent => s_ConsumerRegistered;

        // Producers push their full state again when this changes, so a recreated consumer starts complete.
        public static uint ConsumerVersion => s_ConsumerVersion;

        public static bool TryRegisterConsumer()
        {
            if (s_ConsumerRegistered)
                return false;

            s_ConsumerRegistered = true;
            s_ConsumerVersion++;
            s_PendingTransforms = new NativeList<SurfaceCacheEntityTransformRecord>(Allocator.Persistent);
            ClearPending();
            return true;
        }

        public static void UnregisterConsumer()
        {
            Debug.Assert(s_ConsumerRegistered);

            s_ConsumerRegistered = false;
            s_DrainInProgress = false;
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

            DropRetainedTransforms(record.Key);
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

            if (s_PendingChanged.Count != 0 || s_PendingDestroyed.Count != 0)
                FoldSupersededTransforms();

            foreach (var record in s_PendingChanged.Values)
                changed.Add(record);

            foreach (var key in s_PendingDestroyed)
                destroyed.Add(key);

            transformChanged = s_PendingTransforms.AsArray();
        }

        public static void EndDrain()
        {
            Debug.Assert(s_DrainInProgress, "EndDrain without a matching BeginDrain.");
            s_DrainInProgress = false;
            ClearPending();
        }

        public static void AbortDrain()
        {
            Debug.Assert(s_DrainInProgress, "AbortDrain without a matching BeginDrain.");
            s_DrainInProgress = false;
            s_RetainedTransformCount = s_PendingTransforms.Length;
        }

        // A record push supersedes the retained transforms for its key: they are older, and the fold must not apply them over it.
        static void DropRetainedTransforms(in EntityId key)
        {
            if (s_RetainedTransformCount == 0)
                return;

            int kept = 0;
            int retained = s_RetainedTransformCount;
            for (int i = 0; i < s_PendingTransforms.Length; i++)
            {
                if (i < s_RetainedTransformCount && s_PendingTransforms[i].Key == key)
                {
                    retained--;
                    continue;
                }

                s_PendingTransforms[kept++] = s_PendingTransforms[i];
            }

            s_PendingTransforms.Length = kept;
            s_RetainedTransformCount = retained;
        }

        // A record retained by an aborted drain may predate a transform push, so fold the transform in rather than drop it.
        static void FoldSupersededTransforms()
        {
            int kept = 0;
            for (int i = 0; i < s_PendingTransforms.Length; i++)
            {
                var key = s_PendingTransforms[i].Key;
                if (s_PendingDestroyed.Contains(key))
                    continue;

                if (s_PendingChanged.TryGetValue(key, out var record))
                {
                    record.LocalToWorld = s_PendingTransforms[i].LocalToWorld;
                    s_PendingChanged[key] = record;
                    continue;
                }

                s_PendingTransforms[kept++] = s_PendingTransforms[i];
            }

            s_PendingTransforms.Length = kept;
        }

        static void ClearPending()
        {
            s_PendingChanged.Clear();
            s_PendingDestroyed.Clear();
            s_RetainedTransformCount = 0;

            if (s_PendingTransforms.IsCreated)
                s_PendingTransforms.Clear();
        }
    }

    sealed class EntityChangeSource : IDisposable
    {
        internal const string k_ContentionError =
            "Multiple Surface Cache GI renderer features are loaded, but only one of them can receive entity (ECS) " +
            "changes. Entities will be missing from this feature's global illumination until the other features are " +
            "removed or their renderer is recreated. GameObject-based renderers are not affected.";

        static readonly Unity.Profiling.ProfilerMarker k_CollectMarker = new("SurfaceCache.EntityChangeCollection");

        readonly List<SurfaceCacheEntityInstanceRecord> m_Changed = new();
        readonly List<EntityId> m_Destroyed = new();
        bool m_BridgeAcquired;
        bool m_DrainBegun;
        bool m_ContentionLogged;

        bool TryAcquireBridge()
        {
            if (!m_BridgeAcquired)
                m_BridgeAcquired = SurfaceCacheEntityChangeBridge.TryRegisterConsumer();

            return m_BridgeAcquired;
        }

        public void CollectChanges(SurfaceCacheWorldChangeSet changeSet)
        {
            using var _ = k_CollectMarker.Auto();

            if (!TryAcquireBridge())
            {
                if (!m_ContentionLogged)
                {
                    m_ContentionLogged = true;
                    Debug.LogError(k_ContentionError);
                }

                return;
            }

            m_Changed.Clear();
            m_Destroyed.Clear();
            SurfaceCacheEntityChangeBridge.BeginDrain(m_Changed, m_Destroyed, out var transformChanged);
            m_DrainBegun = true;

            changeSet.EntityInstanceChangedList = m_Changed;
            changeSet.EntityInstanceTransformChangedList = transformChanged;
            changeSet.EntityInstanceDestroyedList = m_Destroyed;
        }

        public void EndCollectChanges()
        {
            if (!m_DrainBegun)
                return;

            m_DrainBegun = false;
            SurfaceCacheEntityChangeBridge.EndDrain();
        }

        public void AbortCollectChanges()
        {
            if (!m_DrainBegun)
                return;

            m_DrainBegun = false;
            SurfaceCacheEntityChangeBridge.AbortDrain();
        }

        public void Dispose()
        {
            if (!m_BridgeAcquired)
                return;

            AbortCollectChanges();
            m_BridgeAcquired = false;
            SurfaceCacheEntityChangeBridge.UnregisterConsumer();
        }
    }
}

#endif
