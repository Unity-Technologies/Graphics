using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX
{
    public static class VFXReplicationExtension
    {
        struct ReplicationCache
        {
            //Contains only eventID with count > 1
            public Dictionary<int, int[]> replicatedEventID;
        }

        private static ReplicationCache GetOrUpdateReplicationCache(VisualEffectAsset asset)
        {
            //TODO : Really cache it (with a specific editor behavior)
            var cache = new ReplicationCache();

            var events = new List<string>();
            asset.GetEvents(events);

            //TODO : Can be simplified with a struct
            var replicationCandidate = new Dictionary<int, List<uint>>();
            var propertyIDToName = new Dictionary<int, string>();
            foreach (var eventName in events)
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
                if (!propertyIDToName.ContainsKey(eventNameID))
                    propertyIDToName.Add(eventNameID, baseEventName);

                List<uint> replicationIndices;
                if (!replicationCandidate.TryGetValue(eventNameID, out replicationIndices))
                {
                    replicationIndices = new List<uint>();
                    replicationCandidate.Add(eventNameID, replicationIndices);
                }
                replicationIndices.Add(index);
            }

            foreach (var candidate in replicationCandidate)
            {
                var listOfIndices = candidate.Value;
                if (listOfIndices.Count > 1)
                {
                    listOfIndices.Sort(); //Generally already sorted (TODOPAUL : Check the sort on already sorted hasn't any impact)
                    int i = 0;
                    for (; i < listOfIndices.Count; ++i)
                        if (i != listOfIndices[i])
                            break;

                    if (i == listOfIndices.Count)
                    {
                        //This list of indices is valid
                        if (cache.replicatedEventID == null)
                            cache.replicatedEventID = new Dictionary<int, int[]>();

                        var eventNameID = new int[listOfIndices.Count];
                        var baseEvent = propertyIDToName[candidate.Key];
                        for (int j = 0; j < listOfIndices.Count; j++)
                        {
                            eventNameID[j] = j == 0 ? candidate.Key : Shader.PropertyToID(string.Format("{0}_{1}", baseEvent, j));
                        }

                        cache.replicatedEventID.Add(candidate.Key, eventNameID);
                    }
                }
            }

            return cache;
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

            //Default fallback
            visualEffect.SendEvent(eventNameID, eventAttribute);
        }
    }

}
