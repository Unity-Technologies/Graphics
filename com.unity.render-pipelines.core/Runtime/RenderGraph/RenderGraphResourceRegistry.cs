using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    class RenderGraphResourceRegistry
    {
        const int kSharedResourceLifetime = 30;

        static RenderGraphResourceRegistry m_CurrentRegistry;
        internal static RenderGraphResourceRegistry current
        {
            get
            {
                // We assume that it's enough to only check in editor because we don't want to pay the cost at runtime.
#if UNITY_EDITOR
                if (m_CurrentRegistry == null)
                {
                    throw new InvalidOperationException("Current Render Graph Resource Registry is not set. You are probably trying to cast a Render Graph handle to a resource outside of a Render Graph Pass.");
                }
#endif
                return m_CurrentRegistry;
            }
            set
            {
                m_CurrentRegistry = value;
            }
        }

        delegate void ResourceCallback(RenderGraphContext rgContext, IRenderGraphResource res);

        class RenderGraphResourcesData
        {
            public DynamicArray<IRenderGraphResource>   resourceArray = new DynamicArray<IRenderGraphResource>();
            public int                                  sharedResourcesCount;
            public IRenderGraphResourcePool             pool;
            public ResourceCallback                     createResourceCallback;
            public ResourceCallback                     releaseResourceCallback;

            public void Clear(bool onException, int frameIndex)
            {
                resourceArray.Resize(sharedResourcesCount); // First N elements are reserved for shared persistent resources and are kept as is.
                pool.CheckFrameAllocation(onException, frameIndex);
            }

            public void Cleanup()
            {
                // Cleanup all shared resources.
                for (int i = 0; i < sharedResourcesCount; ++i)
                {
                    var resource = resourceArray[i];
                    if (resource != null)
                    {
                        resource.ReleaseGraphicsResource();
                    }
                }
                // Then cleanup the pool
                pool.Cleanup();
            }

            public void PurgeUnusedGraphicsResources(int frameIndex)
            {
                pool.PurgeUnusedResources(frameIndex);
            }

            public int AddNewRenderGraphResource<ResType>(out ResType outRes, bool pooledResource = true)
                where ResType : IRenderGraphResource, new()
            {
                // In order to not create garbage, instead of using Add, we keep the content of the array while resizing and we just reset the existing ref (or create it if it's null).
                int result = resourceArray.size;
                resourceArray.Resize(resourceArray.size + 1, true);
                if (resourceArray[result] == null)
                    resourceArray[result] = new ResType();

                outRes = resourceArray[result] as ResType;
                outRes.Reset(pooledResource ? pool : null);
                return result;
            }
        }

        const int                           kInitialRendererListCount = 256;

        RenderGraphResourcesData[]          m_RenderGraphResources = new RenderGraphResourcesData[(int)RenderGraphResourceType.Count];
        DynamicArray<RendererListDesc>      m_RendererListDescs = new();
public
        UnsafeList<RendererList>            m_RendererLists = new(kInitialRendererListCount, Allocator.Persistent);
        RenderGraphDebugParams              m_RenderGraphDebug;
        RenderGraphLogger                   m_ResourceLogger = new RenderGraphLogger();
        RenderGraphLogger                   m_FrameInformationLogger; // Comes from the RenderGraph instance.
        int                                 m_CurrentFrameIndex;
        int                                 m_ExecutionCount;

        RTHandle                            m_CurrentBackbuffer;

        List<RendererList>                  m_ActiveRendererLists = new List<RendererList>(kInitialRendererListCount);

        UnsafeList<TextureHandle>                       m_TextureHandleImplsFree = new(64, Allocator.Persistent);
        UnsafeHashMap<TextureHandle.Key, TextureHandle> m_TextureHandleImplsUsed = new(64, Allocator.Persistent);

        UnsafeList<RendererListHandle>                  m_RendererListHandleImplsFree = new(64, Allocator.Persistent);
        UnsafeList<RendererListHandle>                  m_RendererListHandleImplsUsed = new(64, Allocator.Persistent);

        unsafe TextureHandle GetTextureHandle(TextureHandle.Key key, RenderTargetIdentifier renderTargetIdentifier = default)
        {
            if (!m_TextureHandleImplsUsed.TryGetValue(key, out var textureHandle))
            {
                var freeCount = m_TextureHandleImplsFree.Length;
                if (freeCount-- > 0)
                {
                    textureHandle = m_TextureHandleImplsFree[freeCount];
                    textureHandle.Clear(key);

                    m_TextureHandleImplsFree.Resize(freeCount);
                    m_TextureHandleImplsUsed.Add(key, textureHandle);
                }
                else
                {
                    var textureHandleImpl = (TextureHandleImpl*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<TextureHandleImpl>(), UnsafeUtility.AlignOf<TextureHandleImpl>(), Allocator.Persistent);
                    textureHandle = new TextureHandle(textureHandleImpl, key);
                    m_TextureHandleImplsUsed.Add(key, textureHandle);
                }
            }
            
            if(renderTargetIdentifier != default)
                ResolveTextureHandle(textureHandle, renderTargetIdentifier);

            return textureHandle;
        }

        unsafe TextureHandle GetTextureHandle(TextureHandle.Key key, RenderTargetIdentifier renderTargetIdentifier, RenderTextureDescriptor renderTextureDescriptor)
        {
            var textureHandle = GetTextureHandle(key, renderTargetIdentifier);
            
            // Hackishly store some desc values that we need..
            textureHandle.Ptr->volumeSlicesHack = renderTextureDescriptor.volumeDepth;

            return textureHandle;
        }

        unsafe TextureHandle GetTextureHandle(TextureHandle.Key key, RenderTargetIdentifier renderTargetIdentifier, TextureDesc textureDesc)
        {
            var textureHandle = GetTextureHandle(key, renderTargetIdentifier);
            
            // Hackishly store some desc values that we need..
            textureHandle.Ptr->volumeSlicesHack = textureDesc.slices;

            return textureHandle;
        }

        void ReturnAllTextureHandles()
        {
            using var usedHandles = m_TextureHandleImplsUsed.GetValueArray(Allocator.Temp);
            m_TextureHandleImplsFree.AddRange(usedHandles);
            m_TextureHandleImplsUsed.Clear();
        }

        unsafe void FreeAllTextureHandles()
        {
            ReturnAllTextureHandles();
            
            foreach (var textureHandle in m_TextureHandleImplsFree)
                UnsafeUtility.Free(textureHandle.Ptr, Allocator.Persistent);

            m_TextureHandleImplsFree.Clear();
        }
        
        unsafe RendererListHandle GetRendererListHandle(RendererListHandle.Key key)
        {
            Debug.Assert(key.Handle == m_RendererListHandleImplsUsed.Length);

            RendererListHandle rendererListHandle;
            
            var freeCount = m_RendererListHandleImplsFree.Length; 
            if (freeCount-- > 0)
            {
                rendererListHandle = m_RendererListHandleImplsFree[freeCount];
                rendererListHandle.Clear(key);

                m_RendererListHandleImplsFree.Resize(freeCount);
                m_RendererListHandleImplsUsed.Add(rendererListHandle);
            }
            else
            {
                var rendererListHandleImpl = (RendererListHandleImpl*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<RendererListHandleImpl>(), UnsafeUtility.AlignOf<RendererListHandleImpl>(), Allocator.Persistent);
                rendererListHandle = new RendererListHandle(rendererListHandleImpl, key);
                m_RendererListHandleImplsUsed.Add(rendererListHandle);
            }

            return rendererListHandle;
        }

        void ReturnAllRendererListHandles()
        {
            m_RendererListHandleImplsFree.AddRange(m_RendererListHandleImplsUsed);
            m_RendererListHandleImplsUsed.Clear();
        }

        unsafe void FreeAllRendererListHandles()
        {
            ReturnAllRendererListHandles();
            
            foreach (var rendererListHandle in m_RendererListHandleImplsFree)
                UnsafeUtility.Free(rendererListHandle.Ptr, Allocator.Persistent);

            m_RendererListHandleImplsFree.Clear();
        }
        
        #region Internal Interface
        internal RTHandle GetTexture(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return null;

            var resource = GetTextureResource(handle.handle).graphicsResource;
            if (resource == null && handle.fallBackResource != TextureHandle.nullHandle.handle)
                return GetTextureResource(handle.fallBackResource).graphicsResource;

            return resource;
        }

        internal bool TextureNeedsFallback(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return false;

            return GetTextureResource(handle.handle).NeedsFallBack();
        }

        internal RendererList GetRendererList(in RendererListHandle handle)
        {
            if (!handle.IsValid() || handle >= m_RendererLists.Length)
                return RendererList.nullRendererList;

            return m_RendererLists[handle];
        }

        internal ComputeBuffer GetComputeBuffer(in ComputeBufferHandle handle)
        {
            if (!handle.IsValid())
                return null;

            return GetComputeBufferResource(handle.handle).graphicsResource;
        }

        private RenderGraphResourceRegistry()
        {
        }

        internal RenderGraphResourceRegistry(RenderGraphDebugParams renderGraphDebug, RenderGraphLogger frameInformationLogger)
        {
            m_RenderGraphDebug = renderGraphDebug;
            m_FrameInformationLogger = frameInformationLogger;

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                m_RenderGraphResources[i] = new RenderGraphResourcesData();
            }

            m_RenderGraphResources[(int)RenderGraphResourceType.Texture].createResourceCallback = CreateTextureCallback;
            m_RenderGraphResources[(int)RenderGraphResourceType.Texture].releaseResourceCallback = ReleaseTextureCallback;
            m_RenderGraphResources[(int)RenderGraphResourceType.Texture].pool = new TexturePool();

            m_RenderGraphResources[(int)RenderGraphResourceType.ComputeBuffer].pool = new ComputeBufferPool();
        }

        internal void BeginRenderGraph(int executionCount)
        {
            m_ExecutionCount = executionCount;
            ResourceHandle.NewFrame(executionCount);

            // We can log independently of current execution name since resources are shared across all executions of render graph.
            if (m_RenderGraphDebug.enableLogging)
                m_ResourceLogger.Initialize("RenderGraph Resources");
        }

        internal void BeginExecute(int currentFrameIndex)
        {
            m_CurrentFrameIndex = currentFrameIndex;
            ManageSharedRenderGraphResources();
            current = this;
        }

        internal void EndExecute()
        {
            current = null;
        }

        void CheckHandleValidity(in ResourceHandle res)
        {
            CheckHandleValidity(res.type, res.index);
        }

        void CheckHandleValidity(RenderGraphResourceType type, int index)
        {
            var resources = m_RenderGraphResources[(int)type].resourceArray;
            if (index >= resources.size)
                throw new ArgumentException($"Trying to access resource of type {type} with an invalid resource index {index}");
        }

        internal void IncrementWriteCount(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            m_RenderGraphResources[res.iType].resourceArray[res.index].IncrementWriteCount();
        }

        internal string GetRenderGraphResourceName(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_RenderGraphResources[res.iType].resourceArray[res.index].GetName();
        }

        internal string GetRenderGraphResourceName(RenderGraphResourceType type, int index)
        {
            CheckHandleValidity(type, index);
            return m_RenderGraphResources[(int)type].resourceArray[index].GetName();
        }

        internal bool IsRenderGraphResourceImported(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_RenderGraphResources[res.iType].resourceArray[res.index].imported;
        }

        internal bool IsRenderGraphResourceShared(RenderGraphResourceType type, int index)
        {
            CheckHandleValidity(type, index);
            return index < m_RenderGraphResources[(int)type].sharedResourcesCount;
        }

        internal bool IsGraphicsResourceCreated(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_RenderGraphResources[res.iType].resourceArray[res.index].IsCreated();
        }

        internal bool IsRendererListCreated(in RendererListHandle res)
        {
            return m_RendererLists[res].isValid;
        }

        internal bool IsRenderGraphResourceImported(RenderGraphResourceType type, int index)
        {
            CheckHandleValidity(type, index);
            return m_RenderGraphResources[(int)type].resourceArray[index].imported;
        }

        internal int GetRenderGraphResourceTransientIndex(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_RenderGraphResources[res.iType].resourceArray[res.index].transientPassIndex;
        }

        // Texture Creation/Import APIs are internal because creation should only go through RenderGraph
        internal TextureHandle ImportTexture(RTHandle rt)
        {
            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Texture].AddNewRenderGraphResource(out TextureResource texResource);
            texResource.graphicsResource = rt;
            texResource.imported = true;

            return GetTextureHandle(new TextureHandle.Key(newHandle), rt, rt.rt != null ? rt.rt.descriptor : default);
        }

        internal TextureHandle CreateSharedTexture(in TextureDesc desc, bool explicitRelease)
        {
            var textureResources = m_RenderGraphResources[(int)RenderGraphResourceType.Texture];
            int sharedTextureCount = textureResources.sharedResourcesCount;

            Debug.Assert(textureResources.resourceArray.size <= sharedTextureCount);

            // try to find an available slot.
            TextureResource texResource = null;
            int textureIndex = -1;

            for (int i = 0; i < sharedTextureCount; ++i)
            {
                var resource = textureResources.resourceArray[i];
                if (resource.shared == false) // unused
                {
                    texResource = (TextureResource)textureResources.resourceArray[i];
                    textureIndex = i;
                    break;
                }
            }

            // if none is available, add a new resource.
            if (texResource == null)
            {
                textureIndex = m_RenderGraphResources[(int)RenderGraphResourceType.Texture].AddNewRenderGraphResource(out texResource, pooledResource: false);
                textureResources.sharedResourcesCount++;
            }

            texResource.imported = true;
            texResource.shared = true;
            texResource.sharedExplicitRelease = explicitRelease;
            texResource.desc = desc;

            return GetTextureHandle(new TextureHandle.Key(textureIndex, true));
        }

        internal void ReleaseSharedTexture(TextureHandle texture)
        {
            var texResources = m_RenderGraphResources[(int)RenderGraphResourceType.Texture];
            if (texture.handle >= texResources.sharedResourcesCount)
                throw new InvalidOperationException("Tried to release a non shared texture.");

            // Decrement if we release the last one.
            if (texture.handle == (texResources.sharedResourcesCount - 1))
                texResources.sharedResourcesCount--;

            var texResource = GetTextureResource(texture.handle);
            texResource.ReleaseGraphicsResource();
            texResource.Reset(null);
        }

        internal TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
        {
            if (m_CurrentBackbuffer != null)
                m_CurrentBackbuffer.SetTexture(rt);
            else
                m_CurrentBackbuffer = RTHandles.Alloc(rt, "Backbuffer");

            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Texture].AddNewRenderGraphResource(out TextureResource texResource);
            texResource.graphicsResource = m_CurrentBackbuffer;
            texResource.imported = true;

            return GetTextureHandle(new TextureHandle.Key(newHandle), m_CurrentBackbuffer);
        }

        internal TextureHandle CreateTexture(in TextureDesc desc, int transientPassIndex = -1)
        {
            ValidateTextureDesc(desc);

            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Texture].AddNewRenderGraphResource(out TextureResource texResource);
            texResource.desc = desc;
            texResource.transientPassIndex = transientPassIndex;
            texResource.requestFallBack = desc.fallBackToBlackTexture;

            return GetTextureHandle(new TextureHandle.Key(newHandle), default, desc);
        }

        internal int GetTextureResourceCount()
        {
            return m_RenderGraphResources[(int)RenderGraphResourceType.Texture].resourceArray.size;
        }

        TextureResource GetTextureResource(ResourceHandle handle)
        {
            return m_RenderGraphResources[(int)RenderGraphResourceType.Texture].resourceArray[handle] as TextureResource;
        }

        internal TextureDesc GetTextureResourceDesc(in ResourceHandle handle)
        {
            return (m_RenderGraphResources[(int)RenderGraphResourceType.Texture].resourceArray[handle] as TextureResource).desc;
        }

        internal RendererListHandle CreateRendererList(in RendererListDesc desc)
        {
            ValidateRendererListDesc(desc);

            int newHandle = m_RendererListDescs.Add(desc);
            m_RendererLists.Resize(newHandle + 1, NativeArrayOptions.ClearMemory);
            
            return GetRendererListHandle(new RendererListHandle.Key(newHandle));
        }

        internal ComputeBufferHandle ImportComputeBuffer(ComputeBuffer computeBuffer)
        {
            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.ComputeBuffer].AddNewRenderGraphResource(out ComputeBufferResource bufferResource);
            bufferResource.graphicsResource = computeBuffer;
            bufferResource.imported = true;

            return new ComputeBufferHandle(newHandle);
        }

        internal ComputeBufferHandle CreateComputeBuffer(in ComputeBufferDesc desc, int transientPassIndex = -1)
        {
            ValidateComputeBufferDesc(desc);

            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.ComputeBuffer].AddNewRenderGraphResource(out ComputeBufferResource bufferResource);
            bufferResource.desc = desc;
            bufferResource.transientPassIndex = transientPassIndex;

            return new ComputeBufferHandle(newHandle);
        }

        internal ComputeBufferDesc GetComputeBufferResourceDesc(in ResourceHandle handle)
        {
            return (m_RenderGraphResources[(int)RenderGraphResourceType.ComputeBuffer].resourceArray[handle] as ComputeBufferResource).desc;
        }

        internal int GetComputeBufferResourceCount()
        {
            return m_RenderGraphResources[(int)RenderGraphResourceType.ComputeBuffer].resourceArray.size;
        }

        ComputeBufferResource GetComputeBufferResource(in ResourceHandle handle)
        {
            return m_RenderGraphResources[(int)RenderGraphResourceType.ComputeBuffer].resourceArray[handle] as ComputeBufferResource;
        }

        internal void UpdateSharedResourceLastFrameIndex(int type, int index)
        {
            m_RenderGraphResources[type].resourceArray[index].sharedResourceLastFrameUsed = m_ExecutionCount;
        }

        void ManageSharedRenderGraphResources()
        {
            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                var resources = m_RenderGraphResources[type];
                for (int i = 0; i < resources.sharedResourcesCount; ++i)
                {
                    var resource = m_RenderGraphResources[type].resourceArray[i];
                    bool isCreated = resource.IsCreated();
                    // Alloc if needed.
                    if (resource.sharedResourceLastFrameUsed == m_ExecutionCount && !isCreated)
                    {
                        // Here we want the resource to have the name given by users because we know that it won't be reused at all.
                        // So no need for an automatic generic name.
                        resource.CreateGraphicsResource(resource.GetName());

                        var textureResource = (TextureResource) resource;
                        ResolveTextureHandle(new TextureHandle.Key(i), textureResource.graphicsResource);
                    }
                    // Release if not used anymore.
                    else if (isCreated && !resource.sharedExplicitRelease && ((resource.sharedResourceLastFrameUsed + kSharedResourceLifetime) < m_ExecutionCount))
                    {
#if UNITY_EDITOR
                        ResolveTextureHandle(new TextureHandle.Key(i), default);
#endif
                        resource.ReleaseGraphicsResource();
                    }
                }
            }
        }

        internal void CreatePooledResource(RenderGraphContext rgContext, int type, int index)
        {
            var resource = m_RenderGraphResources[type].resourceArray[index];
            if (!resource.imported)
            {
                resource.CreatePooledGraphicsResource();

                 if(resource is TextureResource textureResource)
                     ResolveTextureHandle(new TextureHandle.Key(index), textureResource.graphicsResource);
                        
                if (m_RenderGraphDebug.enableLogging)
                    resource.LogCreation(m_FrameInformationLogger);

                m_RenderGraphResources[type].createResourceCallback?.Invoke(rgContext, resource);
            }
        }

        void CreateTextureCallback(RenderGraphContext rgContext, IRenderGraphResource res)
        {
            var resource = res as TextureResource;

#if UNITY_2020_2_OR_NEWER
            var fastMemDesc = resource.desc.fastMemoryDesc;
            if (fastMemDesc.inFastMemory)
            {
                resource.graphicsResource.SwitchToFastMemory(rgContext.cmd, fastMemDesc.residencyFraction, fastMemDesc.flags);
            }
#endif

            if (resource.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation)
            {
                bool debugClear = m_RenderGraphDebug.clearRenderTargetsAtCreation && !resource.desc.clearBuffer;
                using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(debugClear ? RenderGraphProfileId.RenderGraphClearDebug : RenderGraphProfileId.RenderGraphClear)))
                {
                    var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.DepthStencil : ClearFlag.Color;
                    var clearColor = debugClear ? Color.magenta : resource.desc.clearColor;
                    CoreUtils.SetRenderTarget(rgContext.cmd, resource.graphicsResource, clearFlag, clearColor);
                }
            }
        }

        internal void ReleasePooledResource(RenderGraphContext rgContext, int type, int index)
        {
            var resource = m_RenderGraphResources[type].resourceArray[index];

            if (!resource.imported)
            {
                m_RenderGraphResources[type].releaseResourceCallback?.Invoke(rgContext, resource);

                if (m_RenderGraphDebug.enableLogging)
                {
                    resource.LogRelease(m_FrameInformationLogger);
                }

#if UNITY_EDITOR
                if(type == (int)RenderGraphResourceType.Texture)
                    ResolveTextureHandle(new TextureHandle.Key(index), default);
#endif

                resource.ReleasePooledGraphicsResource(m_CurrentFrameIndex);
            }
        }

        void ReleaseTextureCallback(RenderGraphContext rgContext, IRenderGraphResource res)
        {
            var resource = res as TextureResource;

            if (m_RenderGraphDebug.clearRenderTargetsAtRelease)
            {
                using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.RenderGraphClearDebug)))
                {
                    var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.DepthStencil : ClearFlag.Color;
                    CoreUtils.SetRenderTarget(rgContext.cmd, resource.graphicsResource, clearFlag, Color.magenta);
                }
            }
        }

        void ValidateTextureDesc(in TextureDesc desc)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (desc.colorFormat == GraphicsFormat.None && desc.depthBufferBits == DepthBits.None)
            {
                throw new ArgumentException("Texture was created with an invalid color format.");
            }

            if (desc.dimension == TextureDimension.None)
            {
                throw new ArgumentException("Texture was created with an invalid texture dimension.");
            }

            if (desc.slices == 0)
            {
                throw new ArgumentException("Texture was created with a slices parameter value of zero.");
            }

            if (desc.sizeMode == TextureSizeMode.Explicit)
            {
                if (desc.width == 0 || desc.height == 0)
                    throw new ArgumentException("Texture using Explicit size mode was create with either width or height at zero.");
            }
#endif
        }

        void ValidateRendererListDesc(in RendererListDesc desc)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR

            if (!desc.IsValid())
            {
                throw new ArgumentException("Renderer List descriptor is not valid.");
            }

            if (desc.renderQueueRange.lowerBound == 0 && desc.renderQueueRange.upperBound == 0)
            {
                throw new ArgumentException("Renderer List creation descriptor must have a valid RenderQueueRange.");
            }
#endif
        }

        void ValidateComputeBufferDesc(in ComputeBufferDesc desc)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (desc.stride % 4 != 0)
            {
                throw new ArgumentException("Invalid Compute Buffer creation descriptor: Compute Buffer stride must be at least 4.");
            }
            if (desc.count == 0)
            {
                throw new ArgumentException("Invalid Compute Buffer creation descriptor: Compute Buffer count  must be non zero.");
            }
#endif
        }

        internal void CreateRendererLists(UnsafeList<RendererListHandle> rendererListHandles, ScriptableRenderContext context, bool manualDispatch = false)
        {
            // We gather the active renderer lists of a frame in a list/array before we pass it in the core API for batch processing
            m_ActiveRendererLists.Clear();

            foreach (var rendererListHandle in rendererListHandles)
            {
                var rendererList = context.CreateRendererList(m_RendererListDescs[rendererListHandle]);
                m_RendererLists[rendererListHandle] = rendererList;
                m_ActiveRendererLists.Add(rendererList);

                ResolveRendererListHandle(rendererListHandle.key, rendererList);
            }

            if (manualDispatch)
                context.PrepareRendererListsAsync(m_ActiveRendererLists);
        }

        public unsafe void ResolveRendererListHandle(RendererListHandle.Key key, RendererList rendererList)
        {
            m_RendererListHandleImplsUsed[key.Handle].Ptr->ResolvedRendererList = rendererList;
        }
        
        public void ResolveTextureHandle(TextureHandle.Key key, RenderTargetIdentifier rtt)
        {
            ResolveTextureHandle(m_TextureHandleImplsUsed[key], rtt);
        }

        public unsafe void ResolveTextureHandle(TextureHandle textureHandle, RenderTargetIdentifier rtt)
        {
            textureHandle.Ptr->ResolvedIdentifier = rtt;
        }

        internal unsafe void ResolveResources()
        {
            for (int i = 0, n = m_RendererListHandleImplsUsed.Length; i < n; ++i)
            {
                ref var rendererListHandle = ref m_RendererListHandleImplsUsed.ElementAt(i);
                rendererListHandle.Ptr->ResolvedRendererList = m_RendererLists[rendererListHandle.handle];
            }

            using var textureHandles = m_TextureHandleImplsUsed.GetValueArray(Allocator.Temp);
            for (int i = 0, n = textureHandles.Length; i < n; ++i)
            {
                var textureHandle = textureHandles[i];
                textureHandle.Ptr->ResolvedIdentifier = GetTextureResource(textureHandle.handle).graphicsResource;
            }
        }

        internal void Clear(bool onException)
        {
            LogResources();

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_RenderGraphResources[i].Clear(onException, m_CurrentFrameIndex);
            m_RendererListDescs.Clear();
            m_RendererLists.Clear();
            m_ActiveRendererLists.Clear();
            
            ReturnAllTextureHandles();
            ReturnAllRendererListHandles();
        }

        internal void PurgeUnusedGraphicsResources()
        {
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_RenderGraphResources[i].PurgeUnusedGraphicsResources(m_CurrentFrameIndex);
        }

        internal void Cleanup()
        {
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_RenderGraphResources[i].Cleanup();

            m_RendererLists.Dispose();
            
            FreeAllTextureHandles();
            FreeAllRendererListHandles();
            
            m_TextureHandleImplsFree.Dispose();
            m_TextureHandleImplsUsed.Dispose();

            RTHandles.Release(m_CurrentBackbuffer);
        }

        internal void FlushLogs()
        {
            Debug.Log(m_ResourceLogger.GetAllLogs());
        }

        void LogResources()
        {
            if (m_RenderGraphDebug.enableLogging)
            {
                m_ResourceLogger.LogLine("==== Allocated Resources ====\n");

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    m_RenderGraphResources[type].pool.LogResources(m_ResourceLogger);
                    m_ResourceLogger.LogLine("");
                }
            }
        }

        #endregion
    }
}
