using UnityEngine;
using static UnityEditor.LightBaking.InputExtraction;

namespace UnityEditor.LightBaking.Tests
{
    internal static class SourceMapUtil
    {
        public static uint? LookupInstanceIndex(SourceMap map, string gameObjectName)
        {
            GameObject go = GameObject.Find(gameObjectName);
            if (go == null)
                return new uint?();

            EntityId entityId = go.GetEntityId();
            int instanceIndex = map.GetInstanceIndex(entityId);

            if (instanceIndex == -1)
                return new uint?();

            return (uint)instanceIndex;
        }
    }
}
