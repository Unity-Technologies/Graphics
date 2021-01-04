using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;

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

        RenderGraphResourcesData[]          m_RenderGraphResources = new RenderGraphResourcesData[(int)RenderGraphResourceType.Count];
        DynamicArray<RendererListResource>  m_RendererListResources = new DynamicArray<RendererListResource>();
        RenderGraphDebugParams              m_RenderGraphDebug;
        RenderGraphLogger                   m_Logger;
        int                                 m_CurrentFrameIndex;
        int                                 m_ExecutionCount;

        RTHandle                            m_CurrentBackbuffer;

        #region Internal Interface
        internal RTHandle GetTexture(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return null;

            return GetTextureResource(handle.handle).graphicsResource;
        }

        internal bool TextureNeedsFallback(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return false;

            return GetTextureResource(handle.handle).NeedsFallBack();
        }

        internal RendererList GetRendererList(in RendererListHandle handle)
        {
            if (!handle.IsValid() || handle >= m_RendererListResources.size)
                return RendererList.nullRendererList;

            return m_RendererListResources[handle].rendererList;
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

        internal RenderGraphResourceRegistry(RenderGraphDebugParams renderGraphDebug, RenderGraphLogger logger)
        {
            m_RenderGraphDebug = renderGraphDebug;
            m_Logger = logger;

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

        internal int GetTextureResourceCount()
        {
            return m_RenderGraphResources[(int)RenderGraphResourceType.Texture].resourceArray.size;
        }

        TextureResource GetTextureResource(in ResourceHandle handle)
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

                if (m_RenderGraphDebug.logFrameInformation)
                    resource.LogCreation(m_Logger);

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
                    var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.Depth : ClearFlag.Color;
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

                if (m_RenderGraphDebug.logFrameInformation)
                {
                    resource.LogRelease(m_Logger);
                }

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
                    var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.Depth : ClearFlag.Color;
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
                if (desc.enableMSAA)
                    throw new ArgumentException("enableMSAA TextureDesc parameter is not supported for textures using Explicit size mode.");
            }

            if (desc.sizeMode == TextureSizeMode.Scale || desc.sizeMode == TextureSizeMode.Functor)
            {
                if (desc.msaaSamples != MSAASamples.None)
                    throw new ArgumentException("msaaSamples TextureDesc parameter is not supported for textures using Scale or Functor size mode.");
            }
#endif
        }

        void ValidateRendererListDesc(in RendererListDesc desc)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR

            if (desc.passName != ShaderTagId.none && desc.passNames != null
                || desc.passName == ShaderTagId.none && desc.passNames == null)
            {
                throw new ArgumentException("Renderer List creation descriptor must contain either a single passName or an array of passNames.");
            }

            if (desc.renderQueueRange.lowerBound == 0 && desc.renderQueueRange.upperBound == 0)
            {
                throw new ArgumentException("Renderer List creation descriptor must have a valid RenderQueueRange.");
            }

            if (desc.camera == null)
            {
                throw new ArgumentException("Renderer List creation descriptor must have a valid Camera.");
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

        internal void CreateRendererLists(List<RendererListHandle> rendererLists)
        {
            // For now we just create a simple structure
            // but when the proper API is available in trunk we'll kick off renderer lists creation jobs here.
            foreach (var rendererList in rendererLists)
            {
                ref var rendererListResource = ref m_RendererListResources[rendererList];
                ref var desc = ref rendererListResource.desc;
                RendererList newRendererList = RendererList.Create(desc);
                rendererListResource.rendererList = newRendererList;
            }
        }

        internal void Clear(bool onException)
        {
            LogResources();

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_RenderGraphResources[i].Clear(onException, m_CurrentFrameIndex);
            m_RendererListResources.Clear();
        }

        internal void PurgeUnusedGraphicsResources()
        {
            // TODO RENDERGRAPH: Might not be ideal to purge stale resources every frame.
            // In case users enable/disable features along a level it might provoke performance spikes when things are reallocated...
            // Will be much better when we have actual resource aliasing and we can manage memory more efficiently.
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_RenderGraphResources[i].PurgeUnusedGraphicsResources(m_CurrentFrameIndex);
        }

        internal void Cleanup()
        {
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_RenderGraphResources[i].Cleanup();

            RTHandles.Release(m_CurrentBackbuffer);
        }

        void LogResources()
        {
            if (m_RenderGraphDebug.logResources)
            {
                m_Logger.LogLine("==== Allocated Resources ====\n");

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    m_RenderGraphResources[type].pool.LogResources(m_Logger);
                    m_Logger.LogLine("");
                }
            }
        }

        #endregion
    }
}
