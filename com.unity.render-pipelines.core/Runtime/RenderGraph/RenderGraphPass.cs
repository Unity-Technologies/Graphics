using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

public static class HW1371_Helpers
{
    public static ref T As<T>(this UnsafeList self) where T : unmanaged
    {
        unsafe { return ref UnsafeUtility.AsRef<T>(self.Ptr); }
    }
        
    public static void AddRange(this ref FixedListInt32 self, IEnumerable<int> source) { foreach (var e in source) self.Add(e); }
    public static void AddRange<T>(this ref UnsafeList<T> self, IEnumerable<T> source) where T : unmanaged { foreach (var e in source) self.Add(e); }
}


namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    // RenderGraphPass managed data
    struct RenderGraphPassManagedCompanionData
    {
        public ProfilingSampler customSampler;
    }

    // Fully managed RenderGraphPass
    unsafe class RenderGraphPassManaged
    {
        internal delegate void RenderFuncManaged(object passData, RenderGraphContext renderGraphContext);
        
        internal RenderGraphPassUnmanaged* Pass;
        internal object PassData;
        internal RenderFuncManaged RenderFunc;

        public bool HasValidRenderFunc => !Pass->isUnmanagedPass && RenderFunc != null;

        public void Initialize(RenderGraphPassUnmanagedPtr passUnmanagedPtr, object passData, int passIndex, int seqPassIndex, FixedString64 passName)
        {
            Pass = passUnmanagedPtr;
            Pass->Initialize(false, passIndex, seqPassIndex, passName);
            PassData = passData;
            RenderFunc = null;
        }

        public void Release(RenderGraphObjectPool pool)
        {
            //TODO
            Pass->Clear();
            //pool.Release(data);
            // data = null;
            // renderFunc = null;

            // We need to do the release from here because we need the final type.
            //pool.Release(this);
        }
    }

    // Since we can't use pointer types for generic arguments, we need a wrapper type;
    unsafe struct RenderGraphPassUnmanagedPtr
    {
        public RenderGraphPassUnmanaged* Ptr;
        public ref RenderGraphPassUnmanaged Ref => ref *Ptr;

        public static implicit operator RenderGraphPassUnmanaged*(RenderGraphPassUnmanagedPtr ptr) => ptr.Ptr;
        public static implicit operator RenderGraphPassUnmanagedPtr(RenderGraphPassUnmanaged* ptr) => new() { Ptr = ptr };
    }

    public delegate void RenderGraphPassFuncUnmanaged(in UnsafeList passData, ref HW1371_RenderGraphContext renderGraphContext);

    [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
    [BurstCompatible]
    struct RenderGraphPassUnmanaged : IDisposable
    {
        public bool isUnmanagedPass;
        public FunctionPointer<RenderGraphPassFuncUnmanaged> renderFunc;
        
        public UnsafeList passData;

        public FixedString64 name;
        public int index;
        public int seqIndex;
        public bool enableAsyncCompute;
        public bool allowPassCulling;

        public TextureHandle depthBuffer;
        public UnsafeList<TextureHandle> colorBuffers;
        public int colorBufferMaxIndex;
        public int refCount;
        public bool generateDebugData;

        public bool allowRendererListCulling;

        public UnsafeList<UnsafeList<ResourceHandle>> resourceReadLists;
        public UnsafeList<UnsafeList<ResourceHandle>> resourceWriteLists;
        public UnsafeList<UnsafeList<ResourceHandle>> transientResourceList;

        public UnsafeList<RendererListHandle> usedRendererListList;

        public UnsafeList<RendererListHandle> dependsOnRendererListList;

        public static unsafe RenderGraphPassUnmanagedPtr Alloc(Allocator allocator = Allocator.Persistent)
        {
            var passUnmanagedPtr = (RenderGraphPassUnmanaged*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<RenderGraphPassUnmanaged>(), UnsafeUtility.AlignOf<RenderGraphPassUnmanaged>(), allocator);
            *passUnmanagedPtr = default;
            return passUnmanagedPtr;
        }

        public static unsafe void Free(RenderGraphPassUnmanagedPtr ptr, Allocator allocator = Allocator.Persistent)
        {
            UnsafeUtility.Free(ptr, allocator);
        }

        public bool HasValidRenderFunc => isUnmanagedPass && renderFunc.IsCreated;

        public void Create()
        {
            passData = new UnsafeList(Allocator.Persistent);
            
            colorBuffers = new UnsafeList<TextureHandle>(RenderGraph.kMaxMRTCount, Allocator.Persistent);
            colorBuffers.length = colorBuffers.capacity;
            colorBufferMaxIndex = -1;

            resourceReadLists = new UnsafeList<UnsafeList<ResourceHandle>>((int)RenderGraphResourceType.Count, Allocator.Persistent);
            resourceWriteLists = new UnsafeList<UnsafeList<ResourceHandle>>((int)RenderGraphResourceType.Count, Allocator.Persistent);
            transientResourceList = new UnsafeList<UnsafeList<ResourceHandle>>((int)RenderGraphResourceType.Count, Allocator.Persistent);
            resourceReadLists.length = resourceWriteLists.length = transientResourceList.length = (int) RenderGraphResourceType.Count;
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                resourceReadLists[i] = new UnsafeList<ResourceHandle>(0, Allocator.Persistent);
                resourceWriteLists[i] = new UnsafeList<ResourceHandle>(0, Allocator.Persistent);
                transientResourceList[i] = new UnsafeList<ResourceHandle>(0, Allocator.Persistent);
            }

            usedRendererListList = new UnsafeList<RendererListHandle>(0, Allocator.Persistent);
            dependsOnRendererListList = new UnsafeList<RendererListHandle>(0, Allocator.Persistent);
        }
        
        public void Dispose()
        {
            passData.Dispose();
            
            colorBuffers.Dispose();

            for (int i = 0; i < (int) RenderGraphResourceType.Count; ++i)
            {
                resourceReadLists[i].Dispose();
                resourceWriteLists[i].Dispose();
                transientResourceList[i].Dispose();
            }
            resourceReadLists.Dispose();
            resourceWriteLists.Dispose();
            transientResourceList.Dispose();
            
            usedRendererListList.Dispose();
            dependsOnRendererListList.Dispose();
        }
        
        public void Initialize(bool unmanagedPass, int passIndex, int seqPassIndex, in FixedString64 passName)
        {
            Clear();
            isUnmanagedPass = unmanagedPass; 
            index = passIndex;
            seqIndex = seqPassIndex;
            name = passName;
        }

        public void Clear()
        {
            passData.Clear();

            isUnmanagedPass = false;
            renderFunc = default;

            name = "";
            index = -1;
            seqIndex = -1;
            
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                resourceReadLists.ElementAt(i).Clear();
                resourceWriteLists.ElementAt(i).Clear();
                transientResourceList.ElementAt(i).Clear();
            }

            usedRendererListList.Clear();
            dependsOnRendererListList.Clear();
            enableAsyncCompute = false;
            allowPassCulling = true;
            allowRendererListCulling = true;
            generateDebugData = true;
            refCount = 0;

            // Invalidate everything
            colorBufferMaxIndex = -1;
            depthBuffer = TextureHandle.nullHandle;
            for (int i = 0; i < RenderGraph.kMaxMRTCount; ++i)
            {
                colorBuffers[i] = TextureHandle.nullHandle;
            }
        }

        public void AddResourceWrite(in ResourceHandle res)
        {
            resourceWriteLists.ElementAt(res.iType).Add(res);
        }

        public void AddResourceRead(in ResourceHandle res)
        {
            resourceReadLists.ElementAt(res.iType).Add(res);
        }

        public void AddTransientResource(in ResourceHandle res)
        {
            transientResourceList.ElementAt(res.iType).Add(res);
        }

        public void UseRendererList(RendererListHandle rendererList)
        {
            usedRendererListList.Add(rendererList);
        }

        public void DependsOnRendererList(RendererListHandle rendererList)
        {
            dependsOnRendererListList.Add(rendererList);
        }

        public void EnableAsyncCompute(bool value)
        {
            enableAsyncCompute = value;
        }

        public void AllowPassCulling(bool value)
        {
            allowPassCulling = value;
        }

        public void AllowRendererListCulling(bool value)
        {
            allowRendererListCulling = value;
        }

        public void GenerateDebugData(bool value)
        {
            generateDebugData = value;
        }

        public void SetColorBuffer(TextureHandle resource, int index)
        {
            Debug.Assert(index < RenderGraph.kMaxMRTCount && index >= 0);
            colorBufferMaxIndex = Math.Max(colorBufferMaxIndex, index);
            colorBuffers[index] = resource;
            AddResourceWrite(resource.handle);
        }

        public void SetDepthBuffer(TextureHandle resource, DepthAccess flags)
        {
            depthBuffer = resource;
            if ((flags & DepthAccess.Read) != 0)
                AddResourceRead(resource.handle);
            if ((flags & DepthAccess.Write) != 0)
                AddResourceWrite(resource.handle);
        }

        public unsafe void CreatePassData<T>(out T* passData) where T : unmanaged
        {
            this.passData.Resize(1, 16, UnsafeUtility.SizeOf<T>());
            passData = (T*)this.passData.Ptr;
            *passData = default;
        }
    }
}
