#if UNITY_EDITOR
using System.Linq;
#endif
using System.Collections.Generic;

namespace UnityEngine.VFX
{
    public static class VFXReplicationExtension
    {
        struct ReplicationData
        {
            //Contains only eventID with count > 1
            public Dictionary<int, int[]> replicatedEventID;
#if UNITY_EDITOR
            //Use this list of event to invalidate the cache if needed.
            //Only in editor : asset can't change in runtime.
            public List<string> savedEvents;
#endif
        }

        static Dictionary<VisualEffectAsset, ReplicationData> s_ReplicationCache = new Dictionary<VisualEffectAsset, ReplicationData>();
        static List<string> s_EventsCache = new List<string>();

        struct ReplicationCandidate
        {
            public string baseEventName;
            public List<uint> eventIndices;
        }

        private static ReplicationData GetOrUpdateReplicationCache(VisualEffectAsset asset)
        {
            ReplicationData replication;
            if (s_ReplicationCache.TryGetValue(asset, out replication))
            {
#if UNITY_EDITOR
                s_EventsCache.Clear();
                asset.GetEvents(s_EventsCache);
                if (s_EventsCache.SequenceEqual(replication.savedEvents))
                    return replication;

                //EventList changed, the cache is invalid, rebuild it
                s_ReplicationCache.Remove(asset);
#else
                return replication;
#endif
            }

            var newCacheEntry = new ReplicationData();
            s_EventsCache.Clear();
            asset.GetEvents(s_EventsCache);

#if UNITY_EDITOR
            newCacheEntry.savedEvents = s_EventsCache.ToList();
#endif

            var replicationCandidates = new Dictionary<int, ReplicationCandidate>();
            foreach (var eventName in s_EventsCache)
            {
                uint index = 0u;
                var baseEventName = eventName;
                int separatorIndex = eventName.LastIndexOf('_');
                if (separatorIndex != -1 && separatorIndex != 0)
                {
                    var indexStr = eventName.Substring(separatorIndex + 1);
                    if (uint.TryParse(indexStr, out index))
                        baseEventName = eventName.Substring(0, separatorIndex);
                }

                var eventNameID = Shader.PropertyToID(baseEventName);
                ReplicationCandidate replicationCandidate;
                if (!replicationCandidates.TryGetValue(eventNameID, out replicationCandidate))
                {
                    replicationCandidate.eventIndices = new List<uint>();
                    replicationCandidate.baseEventName = baseEventName;
                    replicationCandidates.Add(eventNameID, replicationCandidate);
                }
                replicationCandidate.eventIndices.Add(index);
            }

            foreach (var candidate in replicationCandidates)
            {
                var listOfIndices = candidate.Value.eventIndices;
                if (listOfIndices.Count > 1)
                {
                    listOfIndices.Sort();
                    int i = 0;
                    for (; i < listOfIndices.Count; ++i)
                        if (i != listOfIndices[i])
                            break;

                    if (i == listOfIndices.Count)
                    {
                        //This list of event is valid, there isn't any missing entry.
                        if (newCacheEntry.replicatedEventID == null)
                            newCacheEntry.replicatedEventID = new Dictionary<int, int[]>();

                        var eventNameIds = new int[listOfIndices.Count];
                        for (int j = 0; j < listOfIndices.Count; j++)
                        {
                            var eventName = GetEventNameFromReplicationIndex(candidate.Value.baseEventName, (uint)j);
                            eventNameIds[j] = Shader.PropertyToID(eventName);
                        }
                        newCacheEntry.replicatedEventID.Add(candidate.Key, eventNameIds);
                    }
                }
            }

            s_ReplicationCache.Add(asset, newCacheEntry);
            return newCacheEntry;
        }

        public static string GetEventNameFromReplicationIndex(string baseEventName, uint index)
        {
            if (index == 0u)
                return baseEventName;

            return string.Format("{0}_{1}", baseEventName, index);
        }

        public static uint GetReplicationCount(this VisualEffect visualEffect, string eventNameID)
        {
            return GetReplicationCount(visualEffect, Shader.PropertyToID(eventNameID));
        }

        public static uint GetReplicationCount(this VisualEffect visualEffect, int eventNameID)
        {
            var asset = visualEffect.visualEffectAsset;
            if (asset != null)
                return GetReplicationCount(asset, eventNameID);

            return 1u;
        }

        public static uint GetReplicationCount(this VisualEffectAsset asset, string eventNameID)
        {
            return GetReplicationCount(asset, Shader.PropertyToID(eventNameID));
        }

        public static uint GetReplicationCount(this VisualEffectAsset asset, int eventNameID)
        {
            var cache = GetOrUpdateReplicationCache(asset);
            int[] replicationIDs;
            if (cache.replicatedEventID != null && cache.replicatedEventID.TryGetValue(eventNameID, out replicationIDs))
            {
                return (uint)replicationIDs.Length;
            }
            return 1u;
        }

        public static void SendReplicatedEvent(this VisualEffect visualEffect, string eventName, VFXEventAttribute eventAttribute, uint replicationIndex)
        {
            SendReplicatedEvent(visualEffect, Shader.PropertyToID(eventName), eventAttribute, replicationIndex);
        }

        public static void SendReplicatedEvent(this VisualEffect visualEffect, int eventNameID, VFXEventAttribute eventAttribute, uint replicationIndex)
        {
            var asset = visualEffect.visualEffectAsset;
            if (asset != null)
            {
                var cache = GetOrUpdateReplicationCache(asset);
                int[] replicationIDs;
                if (cache.replicatedEventID != null && cache.replicatedEventID.TryGetValue(eventNameID, out replicationIDs))
                {
                    replicationIndex = replicationIndex % (uint)replicationIDs.Length;
                    visualEffect.SendEvent(replicationIDs[replicationIndex], eventAttribute);
                    return;
                }
            }

            visualEffect.SendEvent(eventNameID, eventAttribute);
        }
    }

}
