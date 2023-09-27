using System;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    // Data per usage of a resource(version)
    internal struct ResourceReaderData
    {
        public int passId; // Pass using this
        public int inputSlot; // Nth input of the pass using this resource
    }

    // Part of the data that remains the same for all versions of the resource
    // We cache a lot of data here as the compiler accesses this in many places and going through
    // RenderGraphResourceRegistry was identified as slow in the profiler
    internal struct ResourceUnversionedData
    {
        public readonly string name; // For debugging
        public readonly bool isImported; // Imported graph resource
        public bool isShared; // Shared graph resource
        public int tag;
        public int lastUsePassID; // Index of last used pass. The resource (if not imported) is destroyed after this pass.
        public int lastWritePassID; // The last pass writing it. After this other passes may still read the resource
        public int firstUsePassID; //First pas using the resource this may be reading or writing. If not imported the resource is allocated just before this pass.
        public bool memoryLess;// Never create the texture it is allocated/freed within a renderpass

        public readonly int width;
        public readonly int height;
        public readonly int volumeDepth;
        public readonly int msaaSamples;

        public readonly int latestVersionNumber;

        public readonly bool clear; // graph.m_Resources.GetTextureResourceDesc(fragment.resource).clearBuffer;
        public readonly bool discard; // graph.m_Resources.GetTextureResourceDesc(fragment.resource).discardBuffer;
        public readonly bool bindMS;
        
        public ResourceUnversionedData(RenderGraphResourceType t, int resIndex, RenderGraphResourceRegistry resources)
        {
            // We cache these values here
            // as getting them over and over from other
            // graph data structures external to NRP RG is costly 
            var h = new ResourceHandle(resIndex, t, false);
            var rll = resources.GetResourceLowLevel(h);
            resources.GetRenderTargetInfo(h, out var info);
            ref var desc = ref (rll as TextureResource).desc;

            name = rll.GetName();
            isImported = rll.imported;
            isShared = resources.IsRenderGraphResourceShared(h);
            tag = 0;
            firstUsePassID = -1;
            lastUsePassID = -1;
            lastWritePassID = -1;
            memoryLess = false;

            width = info.width;
            height = info.height;
            volumeDepth = info.volumeDepth;
            msaaSamples = info.msaaSamples;

            latestVersionNumber = rll.version;

            clear = desc.clearBuffer;
            discard = desc.discardBuffer;
            bindMS = info.bindMS;
        }
    }

    // Data per resource(version)
    internal struct ResourceVersionedData
    {
        public bool written; // This version of the resource is written by a pass (external resources may never be written by the graph for example)
        public int writePass; // Index in the pass array of the pass writing this specific version. This is always one as the version is different if a resource is written several times.
        public int writeSlot; // Nth output of the pass writing this version
        public int numReaders; // Number of other passes reading this version
        public int tag;

        public int numFragmentUse;

        // Register the pass writing this resource version. A version can only be written by a single pass as every write should introduce a new distinct version.
        public void SetWritingPass(CompilerContextData ctx, ResourceHandle h,int passId, int slot)
        {
            if (written)
            {
                string passName = ctx.passData[passId].name;
                string resourceName = ctx.UnversionedResourceData(h).name;
                throw new Exception($"Only one pass can  write to the same resource. Pass '{passName}' is trying to write '{resourceName}' a second time.");
            }

            writePass = passId;
            writeSlot = slot;
            written = true;
        }

        // Add an extra reader for this resource version. Resource versions can be read many times
        // The same pass can even read a resource twice (if it is passed to two separate input slots)
        public void RegisterReadingPass(CompilerContextData ctx, ResourceHandle h, int passId, int index)
        {
            if (numReaders >= ResourcesData.MaxReaders)
            {
                string passName = ctx.passData[passId].name;
                string resourceName = ctx.UnversionedResourceData(h).name; 
                throw new Exception($"Maximum 10 passes can use a single graph output as input. Pass '{passName}' is trying to read '{resourceName}'.");
            }

            ctx.resources.readerData[h.iType][ResourcesData.IndexReader(h, numReaders)] = new ResourceReaderData
            {
                passId = passId,
                inputSlot = index
            };
            numReaders++;
        }

        // Remove all the reads for the given pass of this resource version
        public void RemoveReadingPass(CompilerContextData ctx, ResourceHandle h, int passId)
        {
            for (int r = 0; r < numReaders;)
            {
                if (ctx.resources.readerData[h.iType][ResourcesData.IndexReader(h, r)].passId == passId)
                {
                    // It should be removed, switch with the end of the list if we're not already at the end of it
                    if (r < numReaders - 1)
                    {
                        ctx.resources.readerData[h.iType][ResourcesData.IndexReader(h, r)] = ctx.resources.readerData[h.iType][ResourcesData.IndexReader(h, numReaders - 1)];
                    }

                    numReaders--;
                    continue; // Do not increment counter so we check the swapped element as well
                }

                r++;
            }
        }
    }

    // This class allows quick lookups from ResourceHandle -> ResourceUnversionedData/ResourceVersionData/ResourceReaderData
    // This is implementing a fully allocated array, we assume there aren't too many resources & versions. This lookup is fast and doesn't
    // require GC allocs to fill.
    internal class ResourcesData
    {
        public DynamicArray<ResourceUnversionedData>[] unversionedData; // Flattened fixed size array storing info per resource id shared between all versions.
        public DynamicArray<ResourceVersionedData>[] versionedData; // Flattened fixed size array storing up to MaxVersions versions per resource id.
        public DynamicArray<ResourceReaderData>[] readerData; // Flattened fixed size array storing up to MaxReaders per resource id per version.
        public const int MaxVersions = 20; // A quite arbitrary limit should be enough for most graphs. Increasing it shouldn't be a problem but will use more memory as these lists use a fixed size upfront allocation.
        public const int MaxReaders = 10; // A quite arbitrary limit should be enough for most graphs. Increasing it shouldn't be a problem but will use more memory as these lists use a fixed size upfront allocation.

        public ResourcesData(int estimatedNumResourcesPerType)
        {
            unversionedData = new DynamicArray<ResourceUnversionedData>[(int)RenderGraphResourceType.Count];
            versionedData = new DynamicArray<ResourceVersionedData>[(int)RenderGraphResourceType.Count];
            readerData = new DynamicArray<ResourceReaderData>[(int)RenderGraphResourceType.Count];

            for (int t = 0; t < (int)RenderGraphResourceType.Count; t++)
            {
                versionedData[t] = new DynamicArray<ResourceVersionedData>(MaxVersions * estimatedNumResourcesPerType, false);
                unversionedData[t] = new DynamicArray<ResourceUnversionedData>(estimatedNumResourcesPerType, false);
                readerData[t] = new DynamicArray<ResourceReaderData>(MaxVersions * estimatedNumResourcesPerType * MaxReaders, false);
            }
        }

        public void Clear()
        {
            for (int t = 0; t < (int)RenderGraphResourceType.Count; t++)
            {
                unversionedData[t].Clear();
                versionedData[t].Clear();
                readerData[t].Clear();
            }
        }

        public void Initialize(RenderGraphResourceRegistry resources)
        {
            for (int t = 0; t < (int)RenderGraphResourceType.Count; t++)
            {
                var numResources = resources.GetResourceCount((RenderGraphResourceType)t);
                
                // For each resource type, resize the buffer (only allocate if bigger)
                // We don't clear the buffer as we reinitialize it right after
                unversionedData[t].Resize(numResources, true);
                if(numResources > 0)
                {
                    unversionedData[t][0] = new ResourceUnversionedData(); // Null Resource
                }
                // Fill the buffer with any existing external info requested for NRP RG process
                for (int r = 1; r < numResources; r++)
                {
                    unversionedData[t][r] = new ResourceUnversionedData((RenderGraphResourceType)t, r, resources);
                }

                // Clear the other caching structures, they will be filled later
                versionedData[t].ResizeAndClear(MaxVersions * numResources);
                readerData[t].ResizeAndClear(MaxVersions * MaxReaders * numResources);  
            }
        }

        // Flatten array index
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index(ResourceHandle h)
        {
            if (h.version < 0 || h.version >= MaxVersions)
                throw new Exception("Invalid version: " + h.version);
            return h.index * MaxVersions + h.version;
        }

        // Flatten array index
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexReader(ResourceHandle h, int readerID)
        {
            if (h.version < 0 || h.version >= MaxVersions)
                throw new Exception("Invalid version");
            if (readerID < 0 || readerID >= MaxReaders)
                throw new Exception("Invalid reader");
            return (h.index * MaxVersions + h.version) * MaxReaders + readerID;
        }

        // Lookup data for a given handle
        public ref ResourceVersionedData this[ResourceHandle h]
        {
            get { return ref versionedData[h.iType][Index(h)]; }
        }
    }
}
