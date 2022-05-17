using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

// Typedefs for the in-engine RendererList API (to avoid conflicts with the experimental version)
using CoreRendererList = UnityEngine.Rendering.RendererList;
using CoreRendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

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
            public DynamicArray<IRenderGraphResource> resourceArray = new DynamicArray<IRenderGraphResource>();
            public int sharedResourcesCount;
            public IRenderGraphResourcePool pool;
            public ResourceCallback createResourceCallback;
            public ResourceCallback releaseResourceCallback;

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

        RenderGraphResourcesData[] m_RenderGraphResources = new RenderGraphResourcesData[(int)RenderGraphResourceType.Count];
        DynamicArray<RendererListResource> m_RendererListResources = new DynamicArray<RendererListResource>();
        RenderGraphDebugParams m_RenderGraphDebug;
        RenderGraphLogger m_ResourceLogger = new RenderGraphLogger();
        RenderGraphLogger m_FrameInformationLogger; // Comes from the RenderGraph instance.
        int m_CurrentFrameIndex;
        int m_ExecutionCount;

        RTHandle m_CurrentBackbuffer;

        const int kInitialRendererListCount = 256;
        List<CoreRendererList> m_ActiveRendererLists = new List<CoreRendererList>(kInitialRendererListCount);

        #region Internal Interface
        internal RTHandle GetTexture(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return null;

            var texResource = GetTextureResource(handle.handle);
            var resource = texResource.graphicsResource;
            if (resource == null)
            {
                if (handle.fallBackResource != TextureHandle.nullHandle.handle)
                    return GetTextureResource(handle.fallBackResource).graphicsResource;
                else if (!texResource.imported)
                    throw new InvalidOperationException($"Trying to use a texture ({texResource.GetName()}) that was already released or not yet created. Make sure you declare it for reading in your pass or you don't read it before it's been written to at least once.");
            }

            return resource;
        }

        internal bool TextureNeedsFallback(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return false;

            return GetTextureResource(handle.handle).NeedsFallBack();
        }

        internal CoreRendererList GetRendererList(in RendererListHandle handle)
        {
            if (!handle.IsValid() || handle >= m_RendererListResources.size)
                return CoreRendererList.nullRendererList;

            return m_RendererListResources[handle].rendererList;
        }

        internal ComputeBuffer GetComputeBuffer(in ComputeBufferHandle handle)
        {
            if (!handle.IsValid())
                return null;

            var bufferResource = GetComputeBufferResource(handle.handle);
            var resource = bufferResource.graphicsResource;
            if (resource == null)
                throw new InvalidOperationException("Trying to use a compute buffer ({bufferResource.GetName()}) that was already released or not yet created. Make sure you declare it for reading in your pass or you don't read it before it's been written to at least once.");

            return resource;
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
            return m_RendererListResources[res].rendererList.isValid;
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

            return new TextureHandle(newHandle);
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

            return new TextureHandle(textureIndex, shared: true);
        }

        internal void RefreshSharedTextureDesc(TextureHandle texture, in TextureDesc desc)
        {
            if (!IsRenderGraphResourceShared(RenderGraphResourceType.Texture, texture.handle))
            {
                throw new InvalidOperationException($"Trying to refresh texture {texture} that is not a shared resource.");
            }

            var texResource = GetTextureResource(texture.handle);
            texResource.ReleaseGraphicsResource();
            texResource.desc = desc;
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

            return new TextureHandle(newHandle);
        }

        internal TextureHandle CreateTexture(in TextureDesc desc, int transientPassIndex = -1)
        {
            ValidateTextureDesc(desc);

            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Texture].AddNewRenderGraphResource(out TextureResource texResource);
            texResource.desc = desc;
            texResource.transientPassIndex = transientPassIndex;
            texResource.requestFallBack = desc.fallBackToBlackTexture;
            return new TextureHandle(newHandle);
        }

        internal int GetResourceCount(RenderGraphResourceType type)
        {
            return m_RenderGraphResources[(int)type].resourceArray.size;
        }

        internal int GetTextureResourceCount()
        {
            return GetResourceCount(RenderGraphResourceType.Texture);
        }

        TextureResource GetTextureResource(in ResourceHandle handle)
        {
            return m_RenderGraphResources[(int)RenderGraphResourceType.Texture].resourceArray[handle] as TextureResource;
        }

        internal TextureDesc GetTextureResourceDesc(in ResourceHandle handle)
        {
            return (m_RenderGraphResources[(int)RenderGraphResourceType.Texture].resourceArray[handle] as TextureResource).desc;
        }

        internal void ForceTextureClear(in ResourceHandle handle, Color clearColor)
        {
            GetTextureResource(handle).desc.clearBuffer = true;
            GetTextureResource(handle).desc.clearColor = clearColor;
        }

        internal RendererListHandle CreateRendererList(in CoreRendererListDesc desc)
        {
            ValidateRendererListDesc(desc);

            int newHandle = m_RendererListResources.Add(new RendererListResource(desc));
            return new RendererListHandle(newHandle);
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
            return GetResourceCount(RenderGraphResourceType.ComputeBuffer);
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
                    }
                    // Release if not used anymore.
                    else if (isCreated && !resource.sharedExplicitRelease && ((resource.sharedResourceLastFrameUsed + kSharedResourceLifetime) < m_ExecutionCount))
                    {
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

                if (m_RenderGraphDebug.enableLogging)
                    resource.LogCreation(m_FrameInformationLogger);

                m_RenderGraphResources[type].createResourceCallback?.Invoke(rgContext, resource);
            }
        }
        internal void CreatePooledResource(RenderGraphContext rgContext, ResourceHandle handle)
        {
            CreatePooledResource(rgContext, handle.iType, handle.index);
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

                resource.ReleasePooledGraphicsResource(m_CurrentFrameIndex);
            }
        }

        internal void ReleasePooledResource(RenderGraphContext rgContext, ResourceHandle handle)
        {
            ReleasePooledResource(rgContext, handle.iType, handle.index);
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

        void ValidateRendererListDesc(in CoreRendererListDesc desc)
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

        internal void CreateRendererLists(List<RendererListHandle> rendererLists, ScriptableRenderContext context, bool manualDispatch = false)
        {
            // We gather the active renderer lists of a frame in a list/array before we pass it in the core API for batch processing
            m_ActiveRendererLists.Clear();

            foreach (var rendererList in rendererLists)
            {
                ref var rendererListResource = ref m_RendererListResources[rendererList];
                ref var desc = ref rendererListResource.desc;
                rendererListResource.rendererList = context.CreateRendererList(desc);
                m_ActiveRendererLists.Add(rendererListResource.rendererList);
            }

            if (manualDispatch)
                context.PrepareRendererListsAsync(m_ActiveRendererLists);
        }

        internal void Clear(bool onException)
        {
            LogResources();

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_RenderGraphResources[i].Clear(onException, m_CurrentFrameIndex);
            m_RendererListResources.Clear();
            m_ActiveRendererLists.Clear();
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
