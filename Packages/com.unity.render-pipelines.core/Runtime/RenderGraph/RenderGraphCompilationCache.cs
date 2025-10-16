using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;

class RenderGraphCompilationCache
{
    struct HashEntry<T>
    {
        public int hash;
        public int lastFrameUsed;
        public T compiledGraph;
    }

    DynamicArray<HashEntry<RenderGraph.CompiledGraph>> m_HashEntries = new();
    DynamicArray<HashEntry<CompilerContextData>> m_NativeHashEntries = new();

    Stack<RenderGraph.CompiledGraph> m_CompiledGraphPool = new();
    Stack<CompilerContextData> m_NativeCompiledGraphPool = new();

    static int HashEntryComparer<T>(HashEntry<T> a, HashEntry<T> b)
    {
        if (a.lastFrameUsed < b.lastFrameUsed)
            return -1;
        else if (a.lastFrameUsed > b.lastFrameUsed)
            return 1;
        else
            return 0;
    }

    static DynamicArray<HashEntry<RenderGraph.CompiledGraph>>.SortComparer s_EntryComparer = HashEntryComparer<RenderGraph.CompiledGraph>;
    static DynamicArray<HashEntry<CompilerContextData>>.SortComparer s_NativeEntryComparer = HashEntryComparer<CompilerContextData>;

    const int k_CachedGraphCount = 20;

    public RenderGraphCompilationCache()
    {
        for (int i = 0; i < k_CachedGraphCount; ++i)
        {
            m_CompiledGraphPool.Push(new RenderGraph.CompiledGraph());
            m_NativeCompiledGraphPool.Push(new CompilerContextData());
        }
    }

    // Avoid GC in lambda.
    static int s_Hash;

    bool GetCompilationCache<T>(int hash, int frameIndex, out T outGraph, DynamicArray<HashEntry<T>> hashEntries, Stack<T> pool, DynamicArray<HashEntry<T>>.SortComparer comparer)
        where T : RenderGraph.ICompiledGraph
    {
        s_Hash = hash;
        int index = hashEntries.FindIndex(value => value.hash == s_Hash);
        if (index != -1)
        {
            ref var entry = ref hashEntries[index];
            outGraph = entry.compiledGraph;
            entry.lastFrameUsed = frameIndex;
            return true;
        }
        else
        {
            if (pool.Count != 0)
            {
                var newEntry = new HashEntry<T>()
                {
                    hash = hash,
                    lastFrameUsed = frameIndex,
                    compiledGraph = pool.Pop()
                };
                hashEntries.Add(newEntry);
                outGraph = newEntry.compiledGraph;
                return false;
            }
            else
            {
                // Reuse the oldest one.
                hashEntries.QuickSort(comparer);
                ref var oldestEntry = ref hashEntries[0];
                oldestEntry.hash = hash;
                oldestEntry.lastFrameUsed = frameIndex;
                oldestEntry.compiledGraph.Clear();

                outGraph = oldestEntry.compiledGraph;
                return false;
            }
        }
    }

    public bool GetCompilationCache(int hash, int frameIndex, out RenderGraph.CompiledGraph outGraph)
    {
        return GetCompilationCache(hash, frameIndex, out outGraph, m_HashEntries, m_CompiledGraphPool, s_EntryComparer);
    }

    public bool GetCompilationCache(int hash, int frameIndex, out CompilerContextData outGraph)
    {
        return GetCompilationCache(hash, frameIndex, out outGraph, m_NativeHashEntries, m_NativeCompiledGraphPool, s_NativeEntryComparer);
    }

    public void Cleanup()
    {
        // We clear the contents of the pools but not the pool themselves, because they are only
        // filled at the beginning of the renderer pipeline and never after. This means when we call
        // Cleanup() after an error, if we were clearing the pools, the render graph could not gracefully start
        // back up because the cache would have a size of 0 (so no room to cache anything).

        // Cleanup compiled graphs currently in the cache
        for (int i = 0; i < m_HashEntries.size; ++i)
        {
            var compiledGraph = m_HashEntries[i].compiledGraph;
            compiledGraph.Clear();
        }
        m_HashEntries.Clear();

        // Cleanup compiled graphs that might be left in the pool
        var compiledGraphs = m_CompiledGraphPool.ToArray();
        for (int i = 0; i < compiledGraphs.Length; ++i)
        {
            compiledGraphs[i].Clear();
        }

        // Dispose of CompilerContextData currently in the cache
        for (int i = 0; i < m_NativeHashEntries.size; ++i)
        {
            var compiledGraph = m_NativeHashEntries[i].compiledGraph;
            compiledGraph.Dispose();
        }
        m_NativeHashEntries.Clear();

        // Dispose of CompilerContextData that might be left in the pool
        var nativeCompiledGraphs = m_NativeCompiledGraphPool.ToArray();
        for (int i = 0; i < nativeCompiledGraphs.Length; ++i)
        {
            nativeCompiledGraphs[i].Dispose();
        }
    }
}
