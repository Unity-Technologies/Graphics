using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    // Note pass=node in the graph, both are sometimes mixed up here
    // Datastructure that contains passes and dependencies and allow yo to iterate and reason on them more like a graph
    internal class CompilerContextData
    {
        public CompilerContextData(int estimatedNumPasses, int estimatedNumResourcesPerType)
        {
            passData = new DynamicArray<PassData>(estimatedNumPasses, false);
            inputData = new DynamicArray<PassInputData>(estimatedNumPasses * 2, false);
            outputData = new DynamicArray<PassOutputData>(estimatedNumPasses * 2, false);
            fragmentData = new DynamicArray<PassFragmentData>(estimatedNumPasses * 4, false);
            resources = new ResourcesData(estimatedNumResourcesPerType);
            nativePassData = new DynamicArray<NativePassData>(estimatedNumPasses, false);// assume nothing gets merged
            nativeSubPassData = new DynamicArray<SubPassDescriptor>(estimatedNumPasses, false);// there should "never" be more subpasses than graph passes
            createData = new DynamicArray<ResourceHandle>(estimatedNumPasses * 2, false);    // assume every pass creates two resources
            destroyData = new DynamicArray<ResourceHandle>(estimatedNumPasses * 2, false);   // assume every pass destroys two resources
        }

        public void Initialize(RenderGraphResourceRegistry resourceRegistry)
        {
            resources.Initialize(resourceRegistry);
        }

        public void Clear()
        {
            passData.Clear();
            inputData.Clear();
            outputData.Clear();
            fragmentData.Clear();
            resources.Clear();
            nativePassData.Clear();
            nativeSubPassData.Clear();
            createData.Clear();
            destroyData.Clear();
        }

        public ResourcesData resources;

        public ref ResourceUnversionedData UnversionedResourceData(ResourceHandle h)
        {
            return ref resources.unversionedData[h.iType][h.index];
        }

        public ref ResourceVersionData VersionedResourceData(ResourceHandle h)
        {
            return ref resources[h];
        }

        // Iterate over all the readers of a particular resource
        public DynamicArray<ResourceReaderData>.RangeEnumerable Readers(ResourceHandle h)
        {
            int numReaders = resources[h].numReaders;
            return resources.readerData[h.iType].SubRange(ResourcesData.IndexReader(h, 0), numReaders);
        }

        // Get the i'th reader of a resource
        public ResourceReaderData ResourceReader(ResourceHandle h, int i)
        {
            int numReaders = resources[h].numReaders;
            if (i >= numReaders)
            {
                throw new Exception("Invalid reader id");
            }

            return resources.readerData[h.iType][ResourcesData.IndexReader(h, 0) + i];
        }

        // Data per graph level renderpass
        public DynamicArray<PassData> passData;

        // Tightly packed lists all passes add to these lists then index in it using offset+count
        public DynamicArray<PassInputData> inputData;
        public DynamicArray<PassOutputData> outputData;
        public DynamicArray<PassFragmentData> fragmentData;
        public DynamicArray<ResourceHandle> createData;
        public DynamicArray<ResourceHandle> destroyData;


        // Data per native renderpas
        public DynamicArray<NativePassData> nativePassData;
        public DynamicArray<SubPassDescriptor> nativeSubPassData; //Tighty packed list of per nrp subpasses

        // resources can be added as fragment both as input and output so make sure not to add them twice (return true upon new addition)
        public bool AddToFragmentList(ResourceHandle h, IBaseRenderGraphBuilder.AccessFlags accessFlags, int listFirstIndex, int numItems)
        {
            for (var i = listFirstIndex; i < listFirstIndex + numItems; ++i)
            {
                if (fragmentData[i].resource.index == h.index)
                {
                    if (fragmentData[i].resource.version != h.version)
                    {
                        //this would mean you're trying to attach say both v1 and v2 of a resource to the same pass as an attachment
                        //this is not allowed
                        throw new Exception("Trying to UseFragment two versions of the same resource");
                    }
                    return false;
                }
            }

            // Validate that we're correctly building up the fragment lists we can only append to the last list
            // not int the middle of lists
            Debug.Assert(listFirstIndex + numItems == fragmentData.size);

            fragmentData.Add(new PassFragmentData()
            {
                resource = h,
                accessFlags = accessFlags
            });
            return true;
        }

        // Mark all passes as unvisited this is usefull for graph algorithms that do something with the tag
        public void TagAll(int value)
        {
            for (int passId = 0; passId < passData.size; passId++)
            {
                passData[passId].tag = value;
            }
        }

        // Helper to loop over nodes
        public struct ActivePassIterator
        {
            CompilerContextData ctx;
            int passId;

            public ActivePassIterator(CompilerContextData ctx)
            {
                this.ctx = ctx;
                this.passId = -1;
            }

            public int Current
            {
                get { return passId; }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    passId++;
                    if (passId >=  ctx.passData.size || ctx.passData[passId].culled == false) break;
                }
                return passId < ctx.passData.size;
            }

            public void Reset()
            {
                passId = -1;
            }

            public ActivePassIterator GetEnumerator()
            {
                return this;
            }
        }

        public ActivePassIterator ActivePasses
        {
            get
            {
                return new ActivePassIterator(this);
            }
        }

        // Helper to loop over native passes
        public struct NativePassIterator
        {
            CompilerContextData ctx;
            int nativePassId;

            public NativePassIterator(CompilerContextData ctx)
            {
                this.ctx = ctx;
                this.nativePassId = -1;
            }

            public int Current
            {
                get { return nativePassId; }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    nativePassId++;
                    if (nativePassId >= ctx.nativePassData.size || ctx.nativePassData[nativePassId].IsValid()) break;
                }
                return nativePassId < ctx.nativePassData.size;
            }

            public void Reset()
            {
                nativePassId = -1;
            }

            public NativePassIterator GetEnumerator()
            {
                return this;
            }
        }

        // Iterate only the active native passes
        // the list may contain empty dummy entries after merging
        public NativePassIterator NativePasses
        {
            get
            {
                return new NativePassIterator(this);
            }
        }

        // Use for testing only
        internal List<NativePassData> GetNativePasses()
        {
            var result = new List<NativePassData>();
            foreach (var pass in this.NativePasses)
            {
                result.Add(nativePassData[pass]);
            }
            return result;
        }
    }
}
