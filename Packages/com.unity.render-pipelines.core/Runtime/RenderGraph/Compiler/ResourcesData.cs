using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    // Data per usage of a resource(version)
    internal readonly struct ResourceReaderData
    {
        public readonly int passId; // Pass using this
        public readonly int inputSlot; // Nth input of the pass using this resource

        public ResourceReaderData(int _passId, int _inputSlot)
        {
            passId = _passId;
            inputSlot = _inputSlot;
        }
    }

    // Part of the data that remains the same for all versions of the resource
    // We cache a lot of data here as the compiler accesses this in many places and going through
    // RenderGraphResourceRegistry was identified as slow in the profiler
    internal struct ResourceUnversionedData
    {
        public int versionedDataOffset;     // Where this resource's versioned data starts in the packed array
        public int versionedDataCount;      // Number of versions this resource actually has
        public int readerDataOffset;        // Where this resource's reader data starts in the packed array
        public int maxReadersPerVersion;    // Max readers across all versions of this resource

        public int lastUsePassID; // Index of last used pass. The resource (if not imported) is destroyed after this pass.
        public int lastWritePassID; // The last pass writing it. After this other passes may still read the resource
        public int firstUsePassID; // First pass using the resource this may be reading or writing. If not imported the resource is allocated just before this pass.
        public int latestVersionNumber; // Mostly readonly, can be decremented only if all passes using the last version are culled

        public readonly bool isImported; // Imported graph resource
        public bool memoryLess; // Never create the texture on GPU if it is allocated/freed within a renderpass
        public int tag;

        public readonly int width;
        public readonly int height;
        public readonly int volumeDepth;
        public readonly int msaaSamples;
        public readonly GraphicsFormat graphicsFormat;

        public readonly bool clear; // graph.m_Resources.GetTextureResourceDesc(fragment.resource).clearBuffer;
        public readonly bool discard; // graph.m_Resources.GetTextureResourceDesc(fragment.resource).discardBuffer;
        public readonly bool bindMS;
        public readonly bool isBackBuffer;

        public TextureUVOriginSelection textureUVOrigin;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetName(CompilerContextData ctx, in ResourceHandle h) => ctx.GetResourceName(h);

        public ResourceUnversionedData(TextureResource rll, ref RenderTargetInfo info, ref TextureDesc desc, bool isResBackBuffer)
        {
            isImported = rll.imported;
            tag = 0;
            firstUsePassID = -1;
            lastUsePassID = -1;
            lastWritePassID = -1;
            memoryLess = false;

            width = info.width;
            height = info.height;
            volumeDepth = info.volumeDepth;
            msaaSamples = info.msaaSamples;

            latestVersionNumber = (int)rll.writeCount;

            clear = desc.clearBuffer;
            discard = desc.discardBuffer;
            bindMS = info.bindMS;
            isBackBuffer = isResBackBuffer;
            textureUVOrigin = rll.textureUVOrigin;
            graphicsFormat = desc.format;

            versionedDataOffset = 0;
            versionedDataCount = 0;
            readerDataOffset = 0;
            maxReadersPerVersion = 0;
        }

        public ResourceUnversionedData(IRenderGraphResource rll, ref BufferDesc _, bool isResBackBuffer)
        {
            // We don't do anything with the BufferDesc for now. The compiler doesn't really need the details of the buffer like it does with textures
            // since for textures it needs the details to merge passes etc. Which is not relevant for buffers.
            isImported = rll.imported;
            tag = 0;
            firstUsePassID = -1;
            lastUsePassID = -1;
            lastWritePassID = -1;
            memoryLess = false;

            width = -1;
            height = -1;
            volumeDepth = -1;
            msaaSamples = -1;

            latestVersionNumber = (int)rll.writeCount;

            clear = false;
            discard = false;
            bindMS = false;
            isBackBuffer = isResBackBuffer;
            textureUVOrigin = TextureUVOriginSelection.Unknown;
            graphicsFormat = GraphicsFormat.None;

            versionedDataOffset = 0;
            versionedDataCount = 0;
            readerDataOffset = 0;
            maxReadersPerVersion = 0;
        }

        public ResourceUnversionedData(IRenderGraphResource rll, ref RayTracingAccelerationStructureDesc _, bool isResBackBuffer)
        {
            // We don't do anything with the RayTracingAccelerationStructureDesc for now. The compiler doesn't really need the details of the acceleration structures like it does with textures
            // since for textures it needs the details to merge passes etc. Which is not relevant for acceleration structures.
            isImported = rll.imported;
            tag = 0;
            firstUsePassID = -1;
            lastUsePassID = -1;
            lastWritePassID = -1;
            memoryLess = false;

            width = -1;
            height = -1;
            volumeDepth = -1;
            msaaSamples = -1;

            latestVersionNumber = (int)rll.writeCount;

            clear = false;
            discard = false;
            bindMS = false;
            isBackBuffer = isResBackBuffer;
            textureUVOrigin = TextureUVOriginSelection.Unknown;
            graphicsFormat = GraphicsFormat.None;

            versionedDataOffset = 0;
            versionedDataCount = 0;
            readerDataOffset = 0;
            maxReadersPerVersion = 0;
        }

        public void InitializeNullResource()
        {
            firstUsePassID = -1;
            lastUsePassID = -1;
            lastWritePassID = -1;
            textureUVOrigin = TextureUVOriginSelection.Unknown;
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
        public void SetWritingPass(CompilerContextData ctx, in ResourceHandle h, int passId)
        {
#if UNITY_ENABLE_CHECKS
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
        public void RegisterReadingPass(CompilerContextData ctx, in ResourceHandle h, int passId, int index)
        {
#if UNITY_ENABLE_CHECKS
            ref var unversioned = ref ctx.resources.unversionedData[h.iType].ElementAt(h.index);
            if (numReaders >= unversioned.maxReadersPerVersion)
            {
                string passName = ctx.GetPassName(passId);
                string resourceName = ctx.GetResourceName(h);
                throw new Exception($"Maximum '{unversioned.maxReadersPerVersion}' passes can use a single graph output as input. Pass {passName} is trying to read {resourceName}.");
            }
#endif
            ctx.resources.readerData[h.iType][ctx.resources.IndexReader(h, numReaders)] = new ResourceReaderData(passId, index);
            numReaders++;
        }

        // Remove all the reads for the given pass of this resource version
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveReadingPass(CompilerContextData ctx, in ResourceHandle h, int passId)
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
    // Uses resource-level sparse allocation: each resource allocates only the versions it needs.
    // Reader allocation uses total read count as conservative upper bound (not true per-version sparse).
    // Allocation metadata is inlined in ResourceUnversionedData for optimal cache performance.
    internal class ResourcesData
    {
        public NativeList<ResourceUnversionedData>[] unversionedData; // Per-resource data (one per resource, includes allocation metadata)
        public NativeList<ResourceVersionedData>[] versionedData;     // Packed versioned data (sparse)
        public NativeList<ResourceReaderData>[] readerData;           // Partially packed reader data (semi-sparse)

        public DynamicArray<Name>[] resourceNames;

        public ResourcesData()
        {
            unversionedData = new NativeList<ResourceUnversionedData>[(int)RenderGraphResourceType.Count];
            versionedData = new NativeList<ResourceVersionedData>[(int)RenderGraphResourceType.Count];
            readerData = new NativeList<ResourceReaderData>[(int)RenderGraphResourceType.Count];
            resourceNames = new DynamicArray<Name>[(int)RenderGraphResourceType.Count];

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
            if (!nativeList.IsCreated)
                nativeList = new NativeList<T>(size, AllocatorManager.Persistent);

            // Resize the list (it will allocate if necessary)
            nativeList.Resize(size, options);
        }

        public void Initialize(RenderGraphResourceRegistry resources)
        {
            for (int t = 0; t < (int)RenderGraphResourceType.Count; t++)
            {
                RenderGraphResourceType resourceType = (RenderGraphResourceType) t;
                var numResources = resources.GetResourceCount(resourceType);

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

                // Compute allocation sizes and populate unversionedData in a single pass
                int totalVersionedDataCount = 0;
                int totalReaderDataCount = 0;

                // Null resource at index 0 already initialized to 0 in constructors
                // Process all resources in one pass for better cache locality
                for (int r = 1; r < numResources; r++)
                {
                    var h = new ResourceHandle(r, resourceType, false);
                    var rll = resources.GetResourceLowLevel(h);
                    resourceNames[t][r] = new Name(rll.GetName());

                    // Initialize unversionedData based on resource type
                    switch (t)
                    {
                        case (int)RenderGraphResourceType.Texture:
                            {
                                var tex = rll as TextureResource;
                                resources.GetRenderTargetInfo(h, out var info);
                                ref var desc = ref tex.desc;
                                bool isBackBuffer = resources.IsRenderGraphResourceBackBuffer(h);

                                unversionedData[t][r] = new ResourceUnversionedData(tex, ref info, ref desc, isBackBuffer);
                                break;
                            }
                        case (int)RenderGraphResourceType.Buffer:
                            {
                                ref var desc = ref (rll as BufferResource).desc;
                                bool isResourceShared = resources.IsRenderGraphResourceShared(h);
                                bool isBackBuffer = resources.IsRenderGraphResourceBackBuffer(h);

                                unversionedData[t][r] = new ResourceUnversionedData(rll, ref desc, isBackBuffer);
                                break;
                            }
                        case (int)RenderGraphResourceType.AccelerationStructure:
                            {
                                ref var desc = ref (rll as RayTracingAccelerationStructureResource).desc;
                                bool isResourceShared = resources.IsRenderGraphResourceShared(h);
                                bool isBackBuffer = resources.IsRenderGraphResourceBackBuffer(h);

                                unversionedData[t][r] = new ResourceUnversionedData(rll, ref desc, isBackBuffer);
                                break;
                            }
                        default:
                            throw new Exception("Unsupported resource type: " + t);
                    }

                    // Compute allocation metadata for this resource
                    // +1 for versions: v0 exists even without writes
                    int numVersions = (int)rll.writeCount + 1;
                    // +1 for readers: transient resources don't call IncrementReadCount, but BuildGraph adds 1 implicit read
                    // Note: rll.readCount is total across all versions (not per-version), so this is a conservative upper bound
                    int numReaders = (int)rll.readCount + 1;

                    // Populate allocation metadata
                    ref var unversioned = ref unversionedData[t].ElementAt(r);
                    unversioned.versionedDataOffset = totalVersionedDataCount;
                    unversioned.versionedDataCount = numVersions;
                    unversioned.readerDataOffset = totalReaderDataCount;
                    unversioned.maxReadersPerVersion = numReaders;

                    totalVersionedDataCount += numVersions;
                    totalReaderDataCount += numVersions * numReaders;
                }

                AllocateAndResizeNativeListIfNeeded(ref versionedData[t], totalVersionedDataCount, NativeArrayOptions.ClearMemory);
                AllocateAndResizeNativeListIfNeeded(ref readerData[t], totalReaderDataCount, NativeArrayOptions.ClearMemory);
            }
        }

        // Flatten array index using sparse allocation (uses inlined allocation metadata)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Index(in ResourceHandle h)
        {
            ref var unversioned = ref unversionedData[h.iType].ElementAt(h.index);
#if UNITY_EDITOR // Hot path
            if (h.version < 0 || h.version >= unversioned.versionedDataCount)
                throw new Exception("Invalid version: " + h.version);
#endif
            return unversioned.versionedDataOffset + h.version;
        }

        // Flatten array index for readers using sparse allocation (uses inlined allocation metadata)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexReader(in ResourceHandle h, int readerID)
        {
            ref var unversioned = ref unversionedData[h.iType].ElementAt(h.index);
#if UNITY_EDITOR // Hot path
            if (h.version < 0 || h.version >= unversioned.versionedDataCount)
                throw new Exception("Invalid version");
            if (readerID < 0 || readerID >= unversioned.maxReadersPerVersion)
                throw new Exception("Invalid reader");
#endif
            return unversioned.readerDataOffset + h.version * unversioned.maxReadersPerVersion + readerID;
        }

        // Lookup data for a given handle
        public ref ResourceVersionedData this[ResourceHandle h] => ref versionedData[h.iType].ElementAt(Index(h));

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
