#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // Main thread only. Coalescing pending changes by key is safe because an EntityId is never reused.
    // Transforms are appended rather than coalesced, so producers can write them from a job.
    static class SurfaceCacheEntityChangeBridge
    {
        static NativeList<SurfaceCacheEntityInstanceRecord> s_PendingChanged;
        static NativeHashMap<EntityId, int> s_PendingChangedIndex;
        static NativeList<SurfaceCacheSubMeshMaterial> s_PendingMaterials;
        static NativeList<EntityId> s_PendingDestroyed;
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
            s_PendingChanged = new NativeList<SurfaceCacheEntityInstanceRecord>(Allocator.Persistent);
            s_PendingChangedIndex = new NativeHashMap<EntityId, int>(64, Allocator.Persistent);
            s_PendingMaterials = new NativeList<SurfaceCacheSubMeshMaterial>(Allocator.Persistent);
            s_PendingDestroyed = new NativeList<EntityId>(Allocator.Persistent);
            s_PendingTransforms = new NativeList<SurfaceCacheEntityTransformRecord>(Allocator.Persistent);
            ClearPending();
            return true;
        }

        public static void UnregisterConsumer()
        {
            Debug.Assert(s_ConsumerRegistered);

            s_ConsumerRegistered = false;
            s_DrainInProgress = false;

            if (s_PendingTransforms.IsCreated)
            {
                s_PendingChanged.Dispose();
                s_PendingChangedIndex.Dispose();
                s_PendingMaterials.Dispose();
                s_PendingDestroyed.Dispose();
                s_PendingTransforms.Dispose();
            }
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

        public static void PushChanged(in SurfaceCacheEntityInstanceRecord record, ReadOnlySpan<SurfaceCacheSubMeshMaterial> materials)
        {
            if (!IsConsumerPresent)
                return;

            DropRetainedTransforms(record.Key);

            var stored = record;
            stored.MaterialsCount = materials.Length;

            // Avoids re-appending when a key's assignment count does not grow.
            if (s_PendingChangedIndex.TryGetValue(record.Key, out int at))
            {
                var previous = s_PendingChanged[at];
                if (previous.MaterialsCount >= materials.Length)
                {
                    for (int i = 0; i < materials.Length; i++)
                        s_PendingMaterials[previous.MaterialsStart + i] = materials[i];

                    stored.MaterialsStart = previous.MaterialsStart;
                }
                else
                {
                    stored.MaterialsStart = s_PendingMaterials.Length;
                    foreach (var material in materials)
                        s_PendingMaterials.Add(material);
                }

                s_PendingChanged[at] = stored;
                return;
            }

            stored.MaterialsStart = s_PendingMaterials.Length;
            foreach (var material in materials)
                s_PendingMaterials.Add(material);

            s_PendingChangedIndex.Add(record.Key, s_PendingChanged.Length);
            s_PendingChanged.Add(stored);
        }

        public static void PushDestroyed(in EntityId key)
        {
            if (!IsConsumerPresent)
                return;

            if (s_PendingChangedIndex.TryGetValue(key, out int at))
            {
                s_PendingChangedIndex.Remove(key);
                s_PendingChanged.RemoveAtSwapBack(at);

                if (at < s_PendingChanged.Length)
                    s_PendingChangedIndex[s_PendingChanged[at].Key] = at;
            }

            s_PendingDestroyed.Add(key);
        }

        // Views into the bridge's storage: valid until EndDrain, invalidated by any push in between.
        public static void BeginDrain(
            out NativeArray<SurfaceCacheEntityInstanceRecord> changed,
            out NativeArray<SurfaceCacheSubMeshMaterial> changedMaterials,
            out NativeArray<EntityId> destroyed,
            out NativeArray<SurfaceCacheEntityTransformRecord> transformChanged)
        {
            Debug.Assert(!s_DrainInProgress, "BeginDrain without a matching EndDrain.");
            s_DrainInProgress = true;

            if (s_PendingChanged.Length != 0 || s_PendingDestroyed.Length != 0)
                FoldSupersededTransforms();

            changed = s_PendingChanged.AsArray();
            changedMaterials = s_PendingMaterials.AsArray();
            destroyed = s_PendingDestroyed.AsArray();
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
            var destroyed = new NativeHashSet<EntityId>(math.max(1, s_PendingDestroyed.Length), Allocator.Temp);
            for (int i = 0; i < s_PendingDestroyed.Length; i++)
                destroyed.Add(s_PendingDestroyed[i]);

            int kept = 0;
            for (int i = 0; i < s_PendingTransforms.Length; i++)
            {
                var key = s_PendingTransforms[i].Key;
                if (destroyed.Contains(key))
                    continue;

                if (s_PendingChangedIndex.TryGetValue(key, out int at))
                {
                    var record = s_PendingChanged[at];
                    record.LocalToWorld = s_PendingTransforms[i].LocalToWorld;
                    s_PendingChanged[at] = record;
                    continue;
                }

                s_PendingTransforms[kept++] = s_PendingTransforms[i];
            }

            s_PendingTransforms.Length = kept;
            destroyed.Dispose();
        }

        static void ClearPending()
        {
            s_PendingChanged.Clear();
            s_PendingChangedIndex.Clear();
            s_PendingMaterials.Clear();
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

            SurfaceCacheEntityChangeBridge.BeginDrain(out var changed, out var materials, out var destroyed, out var transformChanged);
            m_DrainBegun = true;

            changeSet.EntityInstanceChangedList = changed;
            changeSet.EntityInstanceMaterialList = materials;
            changeSet.EntityInstanceDestroyedList = destroyed;
            changeSet.EntityInstanceTransformChangedList = transformChanged;
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
