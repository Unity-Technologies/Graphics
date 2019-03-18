using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    static class IndexSetPool
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<IndexSet> s_IndexSetPool = new ObjectPool<IndexSet>(null, l => l.Clear());

        public static IndexSet Get()
        {
            return s_IndexSetPool.Get();
        }

        public static PooledObject<IndexSet> GetDisposable()
        {
            return s_IndexSetPool.GetDisposable();
        }

        public static void Release(IndexSet toRelease)
        {
            s_IndexSetPool.Release(toRelease);
        }
    }
}
