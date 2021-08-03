using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    class RenderGraphResourceRegistry
    {
        static readonly ShaderTagId s_EmptyName = new ShaderTagId("");

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

        class IRenderGraphResource
        {
            public bool imported;
            public int cachedHash;
            public int transientPassIndex;
            public uint writeCount;
            public bool wasReleased;
            public bool requestFallBack;

            public virtual void Reset()
            {
                imported = false;
                cachedHash = -1;
                transientPassIndex = -1;
                wasReleased = false;
                requestFallBack = false;
                writeCount = 0;
            }

            public virtual string GetName()
            {
                return "";
            }

            public virtual bool IsCreated()
            {
                return false;
            }

            public void IncrementWriteCount()
            {
                writeCount++;
            }

            public bool NeedsFallBack()
            {
                return requestFallBack && writeCount == 0;
            }
        }

        #region Resources
        [DebuggerDisplay("Resource ({GetType().Name}:{GetName()})")]
        class RenderGraphResource<DescType, ResType>
            : IRenderGraphResource
            where DescType : struct
            where ResType : class
        {
            public DescType desc;
            public ResType resource;

            protected RenderGraphResource()
            {

            }

            public override void Reset()
            {
                base.Reset();
                resource = null;
            }

            public override bool IsCreated()
            {
                return resource != null;
            }
        }

        [DebuggerDisplay("TextureResource ({desc.name})")]
        class TextureResource : RenderGraphResource<TextureDesc, RTHandle>
        {
            public override string GetName()
            {
                if (imported)
                    return resource != null ? resource.name : "null resource";
                else
                    return desc.name;
            }
        }

        [DebuggerDisplay("ComputeBufferResource ({desc.name})")]
        class ComputeBufferResource : RenderGraphResource<ComputeBufferDesc, ComputeBuffer>
        {
            public override string GetName()
            {
                if (imported)
                    return "ImportedComputeBuffer"; // No getter for compute buffer name.
                else
                    return desc.name;
            }
        }

        internal struct RendererListResource
        {
            public RendererListDesc desc;
            public RendererList rendererList;

            internal RendererListResource(in RendererListDesc desc)
            {
                this.desc = desc;
                this.rendererList = new RendererList(); // Invalid by default
            }
        }

        #endregion

        DynamicArray<IRenderGraphResource>[] m_Resources = new DynamicArray<IRenderGraphResource>[(int)RenderGraphResourceType.Count];

        TexturePool                         m_TexturePool = new TexturePool();
        int                                 m_TextureCreationIndex;
        ComputeBufferPool                   m_ComputeBufferPool = new ComputeBufferPool();
        DynamicArray<RendererListResource>  m_RendererListResources = new DynamicArray<RendererListResource>();
        RenderGraphDebugParams              m_RenderGraphDebug;
        RenderGraphLogger                   m_Logger;
        int                                 m_CurrentFrameIndex;

        RTHandle                            m_CurrentBackbuffer;

        internal RTHandle GetTexture(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return null;

            return GetTextureResource(handle.handle).resource;
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

            return GetComputeBufferResource(handle.handle).resource;
        }

        #region Internal Interface
        private RenderGraphResourceRegistry()
        {

        }

        internal RenderGraphResourceRegistry(RenderGraphDebugParams renderGraphDebug, RenderGraphLogger logger)
        {
            m_RenderGraphDebug = renderGraphDebug;
            m_Logger = logger;

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_Resources[i] = new DynamicArray<IRenderGraphResource>();
        }

        internal void BeginRender(int currentFrameIndex, int executionCount)
        {
            m_CurrentFrameIndex = currentFrameIndex;
            ResourceHandle.NewFrame(executionCount);
            current = this;
        }

        internal void EndRender()
        {
            current = null;
        }

        void CheckHandleValidity(in ResourceHandle res)
        {
            CheckHandleValidity(res.type, res.index);
        }

        void CheckHandleValidity(RenderGraphResourceType type, int index)
        {
            var resources = m_Resources[(int)type];
            if (index >= resources.size)
                throw new ArgumentException($"Trying to access resource of type {type} with an invalid resource index {index}");
        }
        internal void IncrementWriteCount(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            m_Resources[res.iType][res.index].IncrementWriteCount();
        }

        internal string GetResourceName(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType][res.index].GetName();
        }

        internal string GetResourceName(RenderGraphResourceType type, int index)
        {
            CheckHandleValidity(type, index);
            return m_Resources[(int)type][index].GetName();
        }

        internal bool IsResourceImported(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType][res.index].imported;
        }

        internal bool IsResourceCreated(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType][res.index].IsCreated();
        }

        internal bool IsRendererListCreated(in RendererListHandle res)
        {
            return m_RendererListResources[res].rendererList.isValid;
        }

        internal bool IsResourceImported(RenderGraphResourceType type, int index)
        {
            CheckHandleValidity(type, index);
            return m_Resources[(int)type][index].imported;
        }

        internal int GetResourceTransientIndex(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType][res.index].transientPassIndex;
        }

        // Texture Creation/Import APIs are internal because creation should only go through RenderGraph
        internal TextureHandle ImportTexture(RTHandle rt)
        {
            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.Texture], out TextureResource texResource);
            texResource.resource = rt;
            texResource.imported = true;

            return new TextureHandle(newHandle);
        }

        internal TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
        {
            if (m_CurrentBackbuffer != null)
                m_CurrentBackbuffer.SetTexture(rt);
            else
                m_CurrentBackbuffer = RTHandles.Alloc(rt, "Backbuffer");

            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.Texture], out TextureResource texResource);
            texResource.resource = m_CurrentBackbuffer;
            texResource.imported = true;

            return new TextureHandle(newHandle);
        }

        int AddNewResource<ResType>(DynamicArray<IRenderGraphResource> resourceArray, out ResType outRes) where ResType : IRenderGraphResource, new()
        {
            // In order to not create garbage, instead of using Add, we keep the content of the array while resizing and we just reset the existing ref (or create it if it's null).
            int result = resourceArray.size;
            resourceArray.Resize(resourceArray.size + 1, true);
            if (resourceArray[result] == null)
                resourceArray[result] = new ResType();

            outRes = resourceArray[result] as ResType;
            outRes.Reset();
            return result;
        }

        internal TextureHandle CreateTexture(in TextureDesc desc, int transientPassIndex = -1)
        {
            ValidateTextureDesc(desc);

            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.Texture], out TextureResource texResource);
            texResource.requestFallBack = desc.fallBackToBlackTexture;
            texResource.desc = desc;
            texResource.transientPassIndex = transientPassIndex;
            return new TextureHandle(newHandle);
        }

        internal int GetTextureResourceCount()
        {
            return m_Resources[(int)RenderGraphResourceType.Texture].size;
        }

        TextureResource GetTextureResource(in ResourceHandle handle)
        {
            return m_Resources[(int)RenderGraphResourceType.Texture][handle] as TextureResource;
        }

        internal TextureDesc GetTextureResourceDesc(in ResourceHandle handle)
        {
            return (m_Resources[(int)RenderGraphResourceType.Texture][handle] as TextureResource).desc;
        }

        internal RendererListHandle CreateRendererList(in RendererListDesc desc)
        {
            ValidateRendererListDesc(desc);

            int newHandle = m_RendererListResources.Add(new RendererListResource(desc));
            return new RendererListHandle(newHandle);
        }

        internal ComputeBufferHandle ImportComputeBuffer(ComputeBuffer computeBuffer)
        {
            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.ComputeBuffer], out ComputeBufferResource bufferResource);
            bufferResource.resource = computeBuffer;
            bufferResource.imported = true;

            return new ComputeBufferHandle(newHandle);
        }

        internal ComputeBufferHandle CreateComputeBuffer(in ComputeBufferDesc desc, int transientPassIndex = -1)
        {
            ValidateComputeBufferDesc(desc);

            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.ComputeBuffer], out ComputeBufferResource bufferResource);
            bufferResource.desc = desc;
            bufferResource.transientPassIndex = transientPassIndex;

            return new ComputeBufferHandle(newHandle);
        }

        internal ComputeBufferDesc GetComputeBufferResourceDesc(in ResourceHandle handle)
        {
            return (m_Resources[(int)RenderGraphResourceType.ComputeBuffer][handle] as ComputeBufferResource).desc;
        }

        internal int GetComputeBufferResourceCount()
        {
            return m_Resources[(int)RenderGraphResourceType.ComputeBuffer].size;
        }

        ComputeBufferResource GetComputeBufferResource(in ResourceHandle handle)
        {
            return m_Resources[(int)RenderGraphResourceType.ComputeBuffer][handle] as ComputeBufferResource;
        }

        internal void CreateAndClearTexture(RenderGraphContext rgContext, int index)
        {
            var resource = m_Resources[(int)RenderGraphResourceType.Texture][index] as TextureResource;

            if (!resource.imported)
            {
                var desc = resource.desc;
                int hashCode = desc.GetHashCode();

                if (resource.resource != null)
                    throw new InvalidOperationException(string.Format("Trying to create an already created texture ({0}). Texture was probably declared for writing more than once in the same pass.", resource.desc.name));

                resource.resource = null;
                if (!m_TexturePool.TryGetResource(hashCode, out resource.resource))
                {
                    // Textures are going to be reused under different aliases along the frame so we can't provide a specific name upon creation.
                    // The name in the desc is going to be used for debugging purpose and render graph visualization.
                    string name = $"RenderGraphTexture_{m_TextureCreationIndex++}";

                    switch (desc.sizeMode)
                    {
                        case TextureSizeMode.Explicit:
                            resource.resource = RTHandles.Alloc(desc.width, desc.height, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                            desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, name);
                            break;
                        case TextureSizeMode.Scale:
                            resource.resource = RTHandles.Alloc(desc.scale, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                            desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, name);
                            break;
                        case TextureSizeMode.Functor:
                            resource.resource = RTHandles.Alloc(desc.func, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                            desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, name);
                            break;
                    }
                }

                resource.cachedHash = hashCode;

#if UNITY_2020_2_OR_NEWER
                var fastMemDesc = resource.desc.fastMemoryDesc;
                if(fastMemDesc.inFastMemory)
                {
                    resource.resource.SwitchToFastMemory(rgContext.cmd, fastMemDesc.residencyFraction, fastMemDesc.flags);
                }
#endif

                if (resource.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation)
                {
                    bool debugClear = m_RenderGraphDebug.clearRenderTargetsAtCreation && !resource.desc.clearBuffer;
                    var name = debugClear ? "RenderGraph: Clear Buffer (Debug)" : "RenderGraph: Clear Buffer";
                    using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.RenderGraphClear)))
                    {
                        var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.Depth : ClearFlag.Color;
                        var clearColor = debugClear ? Color.magenta : resource.desc.clearColor;
                        CoreUtils.SetRenderTarget(rgContext.cmd, resource.resource, clearFlag, clearColor);
                    }
                }

                m_TexturePool.RegisterFrameAllocation(hashCode, resource.resource);
                LogTextureCreation(resource);
            }
        }

        internal void CreateComputeBuffer(RenderGraphContext rgContext, int index)
        {
            var resource = m_Resources[(int)RenderGraphResourceType.ComputeBuffer][index] as ComputeBufferResource;
            if (!resource.imported)
            {
                var desc = resource.desc;
                int hashCode = desc.GetHashCode();

                if (resource.resource != null)
                    throw new InvalidOperationException(string.Format("Trying to create an already created Compute Buffer ({0}). Buffer was probably declared for writing more than once in the same pass.", resource.desc.name));

                resource.resource = null;
                if (!m_ComputeBufferPool.TryGetResource(hashCode, out resource.resource))
                {
                    resource.resource = new ComputeBuffer(resource.desc.count, resource.desc.stride, resource.desc.type);
                    resource.resource.name = $"RenderGraphComputeBuffer_{resource.desc.count}_{resource.desc.stride}_{resource.desc.type}";
                }
                resource.cachedHash = hashCode;

                m_ComputeBufferPool.RegisterFrameAllocation(hashCode, resource.resource);
                LogComputeBufferCreation(resource);
            }
        }

        internal void ReleaseTexture(RenderGraphContext rgContext, int index)
        {
            var resource = m_Resources[(int)RenderGraphResourceType.Texture][index] as TextureResource;

            if (!resource.imported)
            {
                if (resource.resource == null)
                    throw new InvalidOperationException($"Tried to release a texture ({resource.desc.name}) that was never created. Check that there is at least one pass writing to it first.");

                if (m_RenderGraphDebug.clearRenderTargetsAtRelease)
                {
                    using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.RenderGraphClearDebug)))
                    {
                        var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.Depth : ClearFlag.Color;
                        // Not ideal to do new TextureHandle here but GetTexture is a public API and we rather have it take an explicit TextureHandle parameters.
                        // Everywhere else internally int is better because it allows us to share more code.
                        CoreUtils.SetRenderTarget(rgContext.cmd, GetTexture(new TextureHandle(index)), clearFlag, Color.magenta);
                    }
                }

                LogTextureRelease(resource);
                m_TexturePool.ReleaseResource(resource.cachedHash, resource.resource, m_CurrentFrameIndex);
                m_TexturePool.UnregisterFrameAllocation(resource.cachedHash, resource.resource);
                resource.cachedHash = -1;
                resource.resource = null;
                resource.wasReleased = true;
            }
        }

        internal void ReleaseComputeBuffer(RenderGraphContext rgContext, int index)
        {
            var resource = m_Resources[(int)RenderGraphResourceType.ComputeBuffer][index] as ComputeBufferResource;

            if (!resource.imported)
            {
                if (resource.resource == null)
                    throw new InvalidOperationException($"Tried to release a compute buffer ({resource.desc.name}) that was never created. Check that there is at least one pass writing to it first.");

                LogComputeBufferRelease(resource);
                m_ComputeBufferPool.ReleaseResource(resource.cachedHash, resource.resource, m_CurrentFrameIndex);
                m_ComputeBufferPool.UnregisterFrameAllocation(resource.cachedHash, resource.resource);
                resource.cachedHash = -1;
                resource.resource = null;
                resource.wasReleased = true;
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
                m_Resources[i].Clear();
            m_RendererListResources.Clear();

            m_TexturePool.CheckFrameAllocation(onException, m_CurrentFrameIndex);
            m_ComputeBufferPool.CheckFrameAllocation(onException, m_CurrentFrameIndex);
        }

        internal void PurgeUnusedResources()
        {
            // TODO RENDERGRAPH: Might not be ideal to purge stale resources every frame.
            // In case users enable/disable features along a level it might provoke performance spikes when things are reallocated...
            // Will be much better when we have actual resource aliasing and we can manage memory more efficiently.
            m_TexturePool.PurgeUnusedResources(m_CurrentFrameIndex);
            m_ComputeBufferPool.PurgeUnusedResources(m_CurrentFrameIndex);
        }

        internal void Cleanup()
        {
            m_TexturePool.Cleanup();
            m_ComputeBufferPool.Cleanup();

            RTHandles.Release(m_CurrentBackbuffer);
        }

        void LogTextureCreation(TextureResource rt)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine($"Created Texture: {rt.desc.name} (Cleared: {rt.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation})");
            }
        }

        void LogTextureRelease(TextureResource rt)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine($"Released Texture: {rt.desc.name}");
            }
        }

        void LogComputeBufferCreation(ComputeBufferResource buffer)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine($"Created ComputeBuffer: {buffer.desc.name}");
            }
        }

        void LogComputeBufferRelease(ComputeBufferResource buffer)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine($"Released ComputeBuffer: {buffer.desc.name}");
            }
        }

        void LogResources()
        {
            if (m_RenderGraphDebug.logResources)
            {
                m_Logger.LogLine("==== Allocated Resources ====\n");

                m_TexturePool.LogResources(m_Logger);
                m_Logger.LogLine("");
                m_ComputeBufferPool.LogResources(m_Logger);
            }
        }

        #endregion
    }
}
