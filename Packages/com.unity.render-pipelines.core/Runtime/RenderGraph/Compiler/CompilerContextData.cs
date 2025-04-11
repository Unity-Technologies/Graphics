using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    // Wrapper struct to allow storing strings in a DynamicArray which requires a type with a parameterless constructor
    internal struct Name
    {
        public readonly string name;
        public readonly int utf8ByteCount;
        public Name(string name, bool computeUTF8ByteCount = false)
        {
            this.name = name;
            this.utf8ByteCount = ((name?.Length > 0) && computeUTF8ByteCount) ? System.Text.Encoding.UTF8.GetByteCount((ReadOnlySpan<char>)name) : 0;
        }
    }

    // Helper extensions for NativeList
    internal static class NativeListExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe ReadOnlySpan<T> MakeReadOnlySpan<T>(this ref NativeList<T> list, int first, int numElements) where T : unmanaged
        {
#if UNITY_EDITOR
            if (first + numElements > list.Length)
                throw new IndexOutOfRangeException();
#endif
            return new ReadOnlySpan<T>(&list.GetUnsafeReadOnlyPtr()[first], numElements);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndex<T>(this ref NativeList<T> list) where T : unmanaged
        {
            return list.Length - 1;
        }
    }

    // Note pass=node in the graph, both are sometimes mixed up here
    // Datastructure that contains passes and dependencies and allow you to iterate and reason on them more like a graph
    internal class CompilerContextData : IDisposable, RenderGraph.ICompiledGraph
    {
        public CompilerContextData()
        {
            fences = new Dictionary<int, GraphicsFence>();
            resources = new ResourcesData();
            passNames = new DynamicArray<Name>(0, false); // T in NativeList<T> cannot contain managed types, so the names are stored separately
        }

        void AllocateNativeDataStructuresIfNeeded(int estimatedNumPasses)
        {
            // Only first init or if Dispose() has been called through RenderGraph.Cleanup()
            if (!m_AreNativeListsAllocated)
            {
                // These are risky heuristics that only work because we purposely estimate a very high number of passes
                // We need to fix this with a proper size computation
                passData = new NativeList<PassData>(estimatedNumPasses, AllocatorManager.Persistent);
                inputData = new NativeList<PassInputData>(estimatedNumPasses * 2, AllocatorManager.Persistent);
                outputData = new NativeList<PassOutputData>(estimatedNumPasses * 2, AllocatorManager.Persistent);
                fragmentData = new NativeList<PassFragmentData>(estimatedNumPasses * 4, AllocatorManager.Persistent);
                randomAccessResourceData = new NativeList<PassRandomWriteData>(4, AllocatorManager.Persistent); // We assume not a lot of passes use random write
                nativePassData = new NativeList<NativePassData>(estimatedNumPasses, AllocatorManager.Persistent);// assume nothing gets merged
                nativeSubPassData = new NativeList<SubPassDescriptor>(estimatedNumPasses, AllocatorManager.Persistent);// there should "never" be more subpasses than graph passes
                createData = new NativeList<ResourceHandle>(estimatedNumPasses * 2, AllocatorManager.Persistent);    // assume every pass creates two resources
                destroyData = new NativeList<ResourceHandle>(estimatedNumPasses * 2, AllocatorManager.Persistent);   // assume every pass destroys two resources

                m_AreNativeListsAllocated = true;
            }
        }

        public void Initialize(RenderGraphResourceRegistry resourceRegistry, int estimatedNumPasses)
        {
            resources.Initialize(resourceRegistry);
            passNames.Reserve(estimatedNumPasses, false);
            AllocateNativeDataStructuresIfNeeded(estimatedNumPasses);
        }

        public void Clear()
        {
            passNames.Clear();
            resources.Clear();

            if (m_AreNativeListsAllocated)
            {
                passData.Clear();
                fences.Clear();
                inputData.Clear();
                outputData.Clear();
                fragmentData.Clear();
                randomAccessResourceData.Clear();
                nativePassData.Clear();
                nativeSubPassData.Clear();
                createData.Clear();
                destroyData.Clear();
            }
        }

        public ResourcesData resources;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ResourceUnversionedData UnversionedResourceData(ResourceHandle h)
        {
            return ref resources.unversionedData[h.iType].ElementAt(h.index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ResourceVersionedData VersionedResourceData(ResourceHandle h)
        {
            return ref resources[h];
        }

        // Iterate over all the readers of a particular resource
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<ResourceReaderData> Readers(ResourceHandle h)
        {
            int firstReader = resources.IndexReader(h, 0);
            int numReaders = resources[h].numReaders;
            return resources.readerData[h.iType].MakeReadOnlySpan(firstReader, numReaders);
        }

        // Get the i'th reader of a resource
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ResourceReaderData ResourceReader(ResourceHandle h, int i)
        {
            int numReaders = resources[h].numReaders;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (i >= numReaders)
            {
                throw new Exception("Invalid reader id");
            }
#endif
            return ref resources.readerData[h.iType].ElementAt(resources.IndexReader(h, 0) + i);
        }

        // Data per graph level renderpass
        public NativeList<PassData> passData;
        public Dictionary<int, GraphicsFence> fences;
        public DynamicArray<Name> passNames;

        // Tightly packed lists all passes, add to these lists then index in it using offset+count
        public NativeList<PassInputData> inputData;
        public NativeList<PassOutputData> outputData;
        public NativeList<PassFragmentData> fragmentData;
        public NativeList<ResourceHandle> createData;
        public NativeList<ResourceHandle> destroyData;
        public NativeList<PassRandomWriteData> randomAccessResourceData;

        // Data per native renderpas
        public NativeList<NativePassData> nativePassData;
        public NativeList<SubPassDescriptor> nativeSubPassData; //Tighty packed list of per nrp subpasses

        // resources can be added as fragment both as input and output so make sure not to add them twice (return true upon new addition)
        public bool AddToFragmentList(TextureAccess access, int listFirstIndex, int numItems)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (access.textureHandle.handle.type != RenderGraphResourceType.Texture) new Exception("Only textures can be used as a fragment attachment.");
#endif
            for (var i = listFirstIndex; i < listFirstIndex + numItems; ++i)
            {
                ref var fragment = ref fragmentData.ElementAt(i);
                if (fragment.resource.index == access.textureHandle.handle.index)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    if (fragment.resource.version != access.textureHandle.handle.version)
                    {
                        //this would mean you're trying to attach say both v1 and v2 of a resource to the same pass as an attachment
                        //this is not allowed
                        throw new Exception("Trying to UseFragment two versions of the same resource");
                    }
#endif
                    return false;
                }
            }

            // Validate that we're correctly building up the fragment lists we can only append to the last list
            // not int the middle of lists
            Debug.Assert(listFirstIndex + numItems == fragmentData.Length);

            fragmentData.Add(new PassFragmentData()
            {
                resource = access.textureHandle.handle,
                accessFlags = access.flags,
                mipLevel = access.mipLevel,
                depthSlice = access.depthSlice,
            });
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Name GetFullPassName(int passId) => passNames[passId];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetPassName(int passId) => passNames[passId].name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetResourceName(ResourceHandle h) => resources.resourceNames[h.iType][h.index].name;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetResourceVersionedName(ResourceHandle h) => GetResourceName(h) + " V" + h.version;

        // resources can be added as fragment both as input and output so make sure not to add them twice (return true upon new addition)
        public bool AddToRandomAccessResourceList(ResourceHandle h, int randomWriteSlotIndex, bool preserveCounterValue, int listFirstIndex, int numItems)
        {
            for (var i = listFirstIndex; i < listFirstIndex + numItems; ++i)
            {
                if (randomAccessResourceData[i].resource.index == h.index && randomAccessResourceData[i].resource.type == h.type)
                {
                    if (randomAccessResourceData[i].resource.version != h.version)
                    {
                        //this would mean you're trying to attach say both v1 and v2 of a resource to the same pass as an attachment
                        //this is not allowed
                        throw new Exception("Trying to UseTextureRandomWrite two versions of the same resource");
                    }
                    return false;
                }
            }

            // Validate that we're correctly building up the fragment lists we can only append to the last list
            // not int the middle of lists
            Debug.Assert(listFirstIndex + numItems == randomAccessResourceData.Length);

            randomAccessResourceData.Add(new PassRandomWriteData()
            {
                resource = h,
                index = randomWriteSlotIndex,
                preserveCounterValue = preserveCounterValue
            });
            return true;
        }

        // Mark all passes as unvisited this is useful for graph algorithms that do something with the tag
        public void TagAllPasses(int value)
        {
            for (int passId = 0; passId < passData.Length; passId++)
            {
                passData.ElementAt(passId).tag = value;
            }
        }
        public void CullAllPasses(bool isCulled)
        {
            for (int passId = 0; passId < passData.Length; passId++)
            {
                passData.ElementAt(passId).culled = isCulled;
            }
        }

        // Helper to loop over native passes
        public struct NativePassIterator
        {
            readonly CompilerContextData m_Ctx;
            int m_Index;

            public NativePassIterator(CompilerContextData ctx)
            {
                m_Ctx = ctx;
                m_Index = -1;
            }

            public ref readonly NativePassData Current => ref m_Ctx.nativePassData.ElementAt(m_Index);

            public bool MoveNext()
            {
                while (true)
                {
                    m_Index++;
                    bool inRange = m_Index < m_Ctx.nativePassData.Length;
                    if (!inRange || m_Ctx.nativePassData.ElementAt(m_Index).IsValid())
                        return inRange;
                }
            }

            public NativePassIterator GetEnumerator()
            {
                return this;
            }
        }

        // Iterate only the active native passes
        // the list may contain empty dummy entries after merging
        public NativePassIterator NativePasses => new NativePassIterator(this);


        // Use for testing only
        internal List<NativePassData> GetNativePasses()
        {
            var result = new List<NativePassData>();
            foreach (ref readonly var pass in NativePasses)
            {
                result.Add(pass);
            }
            return result;
        }

        // IDisposable implementation

        bool m_AreNativeListsAllocated = false;

        ~CompilerContextData() => Cleanup();

        public void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }

        void Cleanup()
        {
            resources.Dispose();

            if (m_AreNativeListsAllocated)
            {
                passData.Dispose();
                inputData.Dispose();
                outputData.Dispose();
                fragmentData.Dispose();
                createData.Dispose();
                destroyData.Dispose();
                randomAccessResourceData.Dispose();
                nativePassData.Dispose();
                nativeSubPassData.Dispose();

                m_AreNativeListsAllocated = false;
            }
        }
    }
}
