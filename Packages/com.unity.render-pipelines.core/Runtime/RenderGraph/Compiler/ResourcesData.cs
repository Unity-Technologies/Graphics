using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler
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

        public int latestVersionNumber; // mostly readonly, can be decremented only if all passes using the last version are culled

        public readonly bool clear; // graph.m_Resources.GetTextureResourceDesc(fragment.resource).clearBuffer;
        public readonly bool discard; // graph.m_Resources.GetTextureResourceDesc(fragment.resource).discardBuffer;
        public readonly bool bindMS;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetName(CompilerContextData ctx, ResourceHandle h) => ctx.GetResourceName(h);

        public ResourceUnversionedData(IRenderGraphResource rll, ref RenderTargetInfo info, ref TextureDesc desc, bool isResourceShared)
        {
            isImported = rll.imported;
            isShared = isResourceShared;
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

        public ResourceUnversionedData(IRenderGraphResource rll, ref BufferDesc _, bool isResourceShared)
        {
            // We don't do anything with the BufferDesc for now. The compiler doesn't really need the details of the buffer like it does with textures
            // since for textures it needs the details to merge passes etc. Which is not relevant for buffers.
            isImported = rll.imported;
            isShared = isResourceShared;
            tag = 0;
            firstUsePassID = -1;
            lastUsePassID = -1;
            lastWritePassID = -1;
            memoryLess = false;

            width = -1;
            height = -1;
            volumeDepth = -1;
            msaaSamples = -1;

            latestVersionNumber = rll.version;

            clear = false;
            discard = false;
            bindMS = false;
        }

        public ResourceUnversionedData(IRenderGraphResource rll, ref RayTracingAccelerationStructureDesc _, bool isResourceShared)
        {
            // We don't do anything with the RayTracingAccelerationStructureDesc for now. The compiler doesn't really need the details of the acceleration structures like it does with textures
            // since for textures it needs the details to merge passes etc. Which is not relevant for acceleration structures.
            isImported = rll.imported;
            isShared = isResourceShared;
            tag = 0;
            firstUsePassID = -1;
            lastUsePassID = -1;
            lastWritePassID = -1;
            memoryLess = false;

            width = -1;
            height = -1;
            volumeDepth = -1;
            msaaSamples = -1;

            latestVersionNumber = rll.version;

            clear = false;
            discard = false;
            bindMS = false;
        }

        public void InitializeNullResource()
        {
            firstUsePassID = -1;
            lastUsePassID = -1;
            lastWritePassID = -1;
        }
    }

    // Data per resource(version)
    internal struct ResourceVersionedData
    {
        public bool written; // This version of the resource is written by a pass (external resources may never be written by the graph for example)
        public int writePassId; // Index in the pass array of the pass writing this specific version. If any, there is always a single index as the version differs when a resource is written several times.
        public int numReaders; // Number of other passes reading this version

        // Register the pass writing this resource version. A version can only be written by a single pass as every write should introduce a new distinct version.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetWritingPass(CompilerContextData ctx, ResourceHandle h, int passId)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (written)
            {
                string passName = ctx.GetPassName(passId);
                string resourceName = ctx.GetResourceName(h);
                throw new Exception($"Only one pass can write to the same resource. Pass {passName} is trying to write {resourceName} a second time.");
            }
#endif
            writePassId = passId;
            written = true;
        }

        // Add an extra reader for this resource version. Resource versions can be read many times
        // The same pass can even read a resource twice (if it is passed to two separate input slots)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterReadingPass(CompilerContextData ctx, ResourceHandle h, int passId, int index)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (numReaders >= ctx.resources.MaxReaders[h.iType])
            {
                string passName = ctx.GetPassName(passId);
                string resourceName = ctx.GetResourceName(h);
                throw new Exception($"Maximum '{ctx.resources.MaxReaders}' passes can use a single graph output as input. Pass {passName} is trying to read {resourceName}.");
            }
#endif
            ctx.resources.readerData[h.iType][ctx.resources.IndexReader(h, numReaders)] = new ResourceReaderData
            {
                passId = passId,
                inputSlot = index
            };
            numReaders++;
        }

        // Remove all the reads for the given pass of this resource version
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveReadingPass(CompilerContextData ctx, ResourceHandle h, int passId)
        {
            for (int r = 0; r < numReaders;)
            {
                ref var reader = ref ctx.resources.readerData[h.iType].ElementAt(ctx.resources.IndexReader(h, r));
                if (reader.passId == passId)
                {
                    // It should be removed, switch with the end of the list if we're not already at the end of it
                    if (r < numReaders - 1)
                    {
                        reader = ctx.resources.readerData[h.iType][ctx.resources.IndexReader(h, numReaders - 1)];
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
        public NativeList<ResourceUnversionedData>[] unversionedData; // Flattened fixed size array storing info per resource id shared between all versions.
        public NativeList<ResourceVersionedData>[] versionedData; // Flattened fixed size array storing up to MaxVersions versions per resource id.
        public NativeList<ResourceReaderData>[] readerData; // Flattened fixed size array storing up to MaxReaders per resource id per version.

        public int[] MaxVersions;
        public int[] MaxReaders;

        public DynamicArray<Name>[] resourceNames;

        public ResourcesData()
        {
            unversionedData = new NativeList<ResourceUnversionedData>[(int)RenderGraphResourceType.Count];
            versionedData = new NativeList<ResourceVersionedData>[(int)RenderGraphResourceType.Count];
            readerData = new NativeList<ResourceReaderData>[(int)RenderGraphResourceType.Count];
            resourceNames = new DynamicArray<Name>[(int)RenderGraphResourceType.Count];
            MaxVersions = new int[(int)RenderGraphResourceType.Count];
            MaxReaders = new int[(int)RenderGraphResourceType.Count];

            for (int t = 0; t < (int)RenderGraphResourceType.Count; t++)
                resourceNames[t] = new DynamicArray<Name>(0); // T in NativeList<T> cannot contain managed types, so the names are stored separately
        }

        public void Clear()
        {
            for (int t = 0; t < (int)RenderGraphResourceType.Count; t++)
            {
                if (unversionedData[t].IsCreated)
                    unversionedData[t].Clear();

                if (versionedData[t].IsCreated)
                    versionedData[t].Clear();

                if (readerData[t].IsCreated)
                    readerData[t].Clear();

                resourceNames[t].Clear();
            }
        }

        void AllocateAndResizeNativeListIfNeeded<T>(ref NativeList<T> nativeList, int size, NativeArrayOptions options) where T : unmanaged
        {
            // Allocate the first time or if Dispose() has been called through RenderGraph.Cleanup()
            // Length remains 0, list is still empty
            if (!nativeList.IsCreated)
                nativeList = new NativeList<T>(size, AllocatorManager.Persistent);

            // Resize the list (it will allocate if necessary)
            // List is not empty anymore
            nativeList.Resize(size, options);
        }

        public void Initialize(RenderGraphResourceRegistry resources)
        {
            for (int t = 0; t < (int)RenderGraphResourceType.Count; t++)
            {
                RenderGraphResourceType resourceType = (RenderGraphResourceType) t;
                var numResources = resources.GetResourceCount(resourceType);

                uint maxReaders = 0;
                uint maxWriters = 0;

                // We don't clear the list as we reinitialize it right after
                AllocateAndResizeNativeListIfNeeded(ref unversionedData[t], numResources, NativeArrayOptions.UninitializedMemory);

                resourceNames[t].Resize(numResources, true);

                if (numResources > 0) // Null Resource
                {
                    var nullResource = new ResourceUnversionedData();
                    nullResource.InitializeNullResource();
                    unversionedData[t][0] = nullResource;
                    resourceNames[t][0] = new Name("");
                }

                // Fill the buffer with any existing external info requested for NRP RG process
                for (int r = 1; r < numResources; r++)
                {
                    // We cache these values here
                    // as getting them over and over from other
                    // graph data structures external to NRP RG is costly
                    var h = new ResourceHandle(r, resourceType, false);
                    var rll = resources.GetResourceLowLevel(h);
                    resourceNames[t][r] = new Name(rll.GetName());

                    switch (t)
                    {
                        case (int)RenderGraphResourceType.Texture:
                            {
                                resources.GetRenderTargetInfo(h, out var info);
                                ref var desc = ref (rll as TextureResource).desc;
                                bool isResourceShared = resources.IsRenderGraphResourceShared(h);

                                unversionedData[t][r] = new ResourceUnversionedData(rll, ref info, ref desc, isResourceShared);
                                break;
                            }
                        case (int)RenderGraphResourceType.Buffer:
                            {
                                ref var desc = ref (rll as BufferResource).desc;
                                bool isResourceShared = resources.IsRenderGraphResourceShared(h);

                                unversionedData[t][r] = new ResourceUnversionedData(rll, ref desc, isResourceShared);
                                break;
                            }
                        case (int)RenderGraphResourceType.AccelerationStructure:
                            {
                                ref var desc = ref (rll as RayTracingAccelerationStructureResource).desc;
                                bool isResourceShared = resources.IsRenderGraphResourceShared(h);

                                unversionedData[t][r] = new ResourceUnversionedData(rll, ref desc, isResourceShared);
                                break;
                            }
                        default:
                            throw new Exception("Unsupported resource type: " + t);
                    }

                    maxReaders = Math.Max(maxReaders, rll.readCount);
                    maxWriters = Math.Max(maxWriters, rll.writeCount);
                }

                // The first resource is a null resource, so we need to add 1 to the count.
                MaxReaders[t] = (int)maxReaders + 1;
                MaxVersions[t]  = (int)maxWriters + 1;

                // Clear the other caching structures, they will be filled later
                AllocateAndResizeNativeListIfNeeded(ref versionedData[t], MaxVersions[t] * numResources, NativeArrayOptions.ClearMemory);
                AllocateAndResizeNativeListIfNeeded(ref readerData[t], MaxVersions[t] * MaxReaders[t] * numResources, NativeArrayOptions.ClearMemory);
            }
        }

        // Flatten array index
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(ResourceHandle h)
        {
#if UNITY_EDITOR // Hot path
            if (h.version < 0 || h.version >= MaxVersions[h.iType])
                throw new Exception("Invalid version: " + h.version);
#endif
            return h.index * MaxVersions[h.iType] + h.version;
        }

        // Flatten array index
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexReader(ResourceHandle h, int readerID)
        {
#if UNITY_EDITOR // Hot path
            if (h.version < 0 || h.version >= MaxVersions[h.iType])
                throw new Exception("Invalid version");
            if (readerID < 0 || readerID >= MaxReaders[h.iType])
                throw new Exception("Invalid reader");
#endif
            return (h.index * MaxVersions[h.iType] + h.version) * MaxReaders[h.iType] + readerID;
        }

        // Lookup data for a given handle
        public ref ResourceVersionedData this[ResourceHandle h]
        {
            get { return ref versionedData[h.iType].ElementAt(Index(h)); }
        }

        public void Dispose()
        {
            for (int t = 0; t < (int)RenderGraphResourceType.Count; t++)
            {
                if (versionedData[t].IsCreated)
                    versionedData[t].Dispose();

                if (unversionedData[t].IsCreated)
                    unversionedData[t].Dispose();

                if (readerData[t].IsCreated)
                    readerData[t].Dispose();
            }
        }
    }
}
