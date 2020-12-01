using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEditor.Graphing
{
    static class DictionaryPool<TKey, TValue>
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<Dictionary<TKey, TValue>> k_Pool = new ObjectPool<Dictionary<TKey, TValue>>(() => new Dictionary<TKey, TValue>(), null, l => l.Clear());

        public static Dictionary<TKey, TValue> Get()
        {
            return k_Pool.Get();
        }

        public static void Release(Dictionary<TKey, TValue> toRelease)
        {
            k_Pool.Release(toRelease);
        }
    }
}
