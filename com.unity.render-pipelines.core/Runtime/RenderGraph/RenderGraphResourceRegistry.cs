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
            public int  cachedHash;
            public int  transientPassIndex;
            public int sharedResourceLastFrameUsed;

            protected IRenderGraphResourcePool m_Pool;

            public virtual void Reset(IRenderGraphResourcePool pool)
            {
                imported = false;
                cachedHash = -1;
                transientPassIndex = -1;
                sharedResourceLastFrameUsed = -1;

                m_Pool = pool;
            }

            public virtual string GetName()
            {
                return "";
            }

            public virtual bool IsCreated()
            {
                return false;
            }

            public virtual void CreatePooledResource() { }
            public virtual void CreateResource(string name = "") { }
            public virtual void ReleasePooledResource(int frameIndex) { }
            public virtual void ReleaseResource() { }
            public virtual void LogCreation(RenderGraphLogger logger) { }
            public virtual void LogRelease(RenderGraphLogger logger) { }
        }

        #region Resources
        [DebuggerDisplay("Resource ({GetType().Name}:{GetName()})")]
        abstract class RenderGraphResource<DescType, ResType>
            : IRenderGraphResource
            where DescType : struct
            where ResType : class
        {
            public DescType desc;
            public ResType resource;

            protected RenderGraphResource()
            {
            }

            public override void Reset(IRenderGraphResourcePool pool)
            {
                base.Reset(pool);
                resource = null;
            }

            public override bool IsCreated()
            {
                return resource != null;
            }

            public override void CreatePooledResource()
            {
                Debug.Assert(m_Pool != null, "CreatePooledResource should only be called for regular pooled resources");

                int hashCode = desc.GetHashCode();

                if (resource != null)
                    throw new InvalidOperationException(string.Format("Trying to create an already created resource ({0}). Resource was probably declared for writing more than once in the same pass.", GetName()));

                var pool = m_Pool as RenderGraphResourcePool<ResType>;
                if (!pool.TryGetResource(hashCode, out resource))
                {
                    CreateResource();
                }

                cachedHash = hashCode;
                pool.RegisterFrameAllocation(cachedHash, resource);
            }

            public override void ReleasePooledResource(int frameIndex)
            {
                if (resource == null)
                    throw new InvalidOperationException($"Tried to release a resource ({GetName()}) that was never created. Check that there is at least one pass writing to it first.");

                // Shared resources don't use the pool
                var pool = m_Pool as RenderGraphResourcePool<ResType>;
                if (pool != null)
                {
                    pool.ReleaseResource(cachedHash, resource, frameIndex);
                    pool.UnregisterFrameAllocation(cachedHash, resource);
                }

                Reset(null);
            }

            public override void ReleaseResource()
            {
                base.ReleaseResource();

                Reset(null);
            }
        }

        [DebuggerDisplay("TextureResource ({desc.name})")]
        class TextureResource : RenderGraphResource<TextureDesc, RTHandle>
        {
            static int m_TextureCreationIndex;

            public override string GetName()
            {
                if (imported)
                    return resource != null ? resource.name : "null resource";
                else
                    return desc.name;
            }

            public override void CreateResource(string name = "")
            {
                // Textures are going to be reused under different aliases along the frame so we can't provide a specific name upon creation.
                // The name in the desc is going to be used for debugging purpose and render graph visualization.
                if (name == "")
                    name = $"RenderGraphTexture_{m_TextureCreationIndex++}";

                switch (desc.sizeMode)
                {
                    case TextureSizeMode.Explicit:
                        resource = RTHandles.Alloc(desc.width, desc.height, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, name);
                        break;
                    case TextureSizeMode.Scale:
                        resource = RTHandles.Alloc(desc.scale, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, name);
                        break;
                    case TextureSizeMode.Functor:
                        resource = RTHandles.Alloc(desc.func, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                        desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, name);
                        break;
                }
            }

            public override void ReleaseResource()
            {
                resource.Release();
                base.ReleaseResource();
            }

            public override void LogCreation(RenderGraphLogger logger)
            {
                logger.LogLine($"Created Texture: {desc.name} (Cleared: {desc.clearBuffer})");
            }

            public override void LogRelease(RenderGraphLogger logger)
            {
                logger.LogLine($"Released Texture: {desc.name}");
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

            public override void CreateResource(string name = "")
            {
                resource = new ComputeBuffer(desc.count, desc.stride, desc.type);
                resource.name = name == "" ? $"RenderGraphComputeBuffer_{desc.count}_{desc.stride}_{desc.type}" : name;
            }

            public override void ReleaseResource()
            {
                resource.Release();
                base.ReleaseResource();
            }

            public override void LogCreation(RenderGraphLogger logger)
            {
                logger.LogLine($"Created ComputeBuffer: {desc.name}");
            }

            public override void LogRelease(RenderGraphLogger logger)
            {
                logger.LogLine($"Released ComputeBuffer: {desc.name}");
            }
        }

        internal struct RendererListResource
        {
            public RendererListDesc desc;
            public RendererList     rendererList;

            internal RendererListResource(in RendererListDesc desc)
            {
                this.desc = desc;
                this.rendererList = new RendererList(); // Invalid by default
            }
        }

        #endregion

        delegate void ResourceCallback(RenderGraphContext rgContext, IRenderGraphResource res);

        class ResourcesData
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
                        resource.ReleaseResource();
                    }
                }
                // Then cleanup the pool
                pool.Cleanup();
            }

            public void PurgeUnusedResources(int frameIndex)
            {
                pool.PurgeUnusedResources(frameIndex);
            }

            public int AddNewResource<ResType>(out ResType outRes, bool pooledResource = true)
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

        ResourcesData[]                     m_Resources = new ResourcesData[(int)RenderGraphResourceType.Count];
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
            {
                m_Resources[i] = new ResourcesData();
            }

            m_Resources[(int)RenderGraphResourceType.Texture].createResourceCallback = CreateTextureCallback;
            m_Resources[(int)RenderGraphResourceType.Texture].releaseResourceCallback = ReleaseTextureCallback;
            m_Resources[(int)RenderGraphResourceType.Texture].pool = new TexturePool();

            m_Resources[(int)RenderGraphResourceType.ComputeBuffer].pool = new ComputeBufferPool();
        }

        internal void BeginRenderGraph(int executionCount)
        {
            ResourceHandle.NewFrame(executionCount);
        }

        internal void BeginExecute(int currentFrameIndex)
        {
            m_CurrentFrameIndex = currentFrameIndex;
            ManageSharedResources();
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
            var resources = m_Resources[(int)type].resourceArray;
            if (index >= resources.size)
                throw new ArgumentException($"Trying to access resource of type {type} with an invalid resource index {index}");
        }

        internal string GetResourceName(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType].resourceArray[res.index].GetName();
        }

        internal string GetResourceName(RenderGraphResourceType type, int index)
        {
            CheckHandleValidity(type, index);
            return m_Resources[(int)type].resourceArray[index].GetName();
        }

        internal bool IsResourceImported(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType].resourceArray[res.index].imported;
        }

        internal bool IsResourceCreated(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType].resourceArray[res.index].IsCreated();
        }

        internal bool IsRendererListCreated(in RendererListHandle res)
        {
            return m_RendererListResources[res].rendererList.isValid;
        }

        internal bool IsResourceImported(RenderGraphResourceType type, int index)
        {
            CheckHandleValidity(type, index);
            return m_Resources[(int)type].resourceArray[index].imported;
        }

        internal int GetResourceTransientIndex(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType].resourceArray[res.index].transientPassIndex;
        }

        // Texture Creation/Import APIs are internal because creation should only go through RenderGraph
        internal TextureHandle ImportTexture(RTHandle rt)
        {
            int newHandle = m_Resources[(int)RenderGraphResourceType.Texture].AddNewResource(out TextureResource texResource);
            texResource.resource = rt;
            texResource.imported = true;

            return new TextureHandle(newHandle);
        }

        internal TextureHandle CreateSharedTexture(in TextureDesc desc)
        {
            var textureResources = m_Resources[(int)RenderGraphResourceType.Texture];
            int sharedTextureCount = textureResources.sharedResourcesCount;

            Debug.Assert(textureResources.resourceArray.size <= sharedTextureCount);

            // try to find an available slot.
            TextureResource texResource = null;
            int textureIndex = -1;

            for (int i = 0; i < sharedTextureCount; ++i)
            {
                var resource = textureResources.resourceArray[i];
                if (resource.sharedResourceLastFrameUsed == -1) // unused
                {
                    texResource = (TextureResource)textureResources.resourceArray[i];
                    textureIndex = i;
                    break;
                }
            }

            // if none is available, add a new resource.
            if (texResource == null)
            {
                textureIndex = m_Resources[(int)RenderGraphResourceType.Texture].AddNewResource(out texResource, pooledResource: false);
            }

            texResource.imported = true;

            return new TextureHandle(textureIndex);
        }

        internal void ReleaseSharedTexture(TextureHandle texture)
        {
            var texResources = m_Resources[(int)RenderGraphResourceType.Texture];
            if (texture.handle >= texResources.sharedResourcesCount)
                throw new InvalidOperationException("Tried to release a non shared texture.");

            GetTextureResource(texture.handle).ReleaseResource();
        }

        internal TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
        {
            if (m_CurrentBackbuffer != null)
                m_CurrentBackbuffer.SetTexture(rt);
            else
                m_CurrentBackbuffer = RTHandles.Alloc(rt, "Backbuffer");

            int newHandle = m_Resources[(int)RenderGraphResourceType.Texture].AddNewResource(out TextureResource texResource);
            texResource.resource = m_CurrentBackbuffer;
            texResource.imported = true;

            return new TextureHandle(newHandle);
        }

        internal TextureHandle CreateTexture(in TextureDesc desc, int transientPassIndex = -1)
        {
            ValidateTextureDesc(desc);

            int newHandle = m_Resources[(int)RenderGraphResourceType.Texture].AddNewResource(out TextureResource texResource);
            texResource.desc = desc;
            texResource.transientPassIndex = transientPassIndex;
            return new TextureHandle(newHandle);
        }

        internal int GetTextureResourceCount()
        {
            return m_Resources[(int)RenderGraphResourceType.Texture].resourceArray.size;
        }

        TextureResource GetTextureResource(in ResourceHandle handle)
        {
            return m_Resources[(int)RenderGraphResourceType.Texture].resourceArray[handle] as TextureResource;
        }

        internal TextureDesc GetTextureResourceDesc(in ResourceHandle handle)
        {
            return (m_Resources[(int)RenderGraphResourceType.Texture].resourceArray[handle] as TextureResource).desc;
        }

        internal RendererListHandle CreateRendererList(in RendererListDesc desc)
        {
            ValidateRendererListDesc(desc);

            int newHandle = m_RendererListResources.Add(new RendererListResource(desc));
            return new RendererListHandle(newHandle);
        }

        internal ComputeBufferHandle ImportComputeBuffer(ComputeBuffer computeBuffer)
        {
            int newHandle = m_Resources[(int)RenderGraphResourceType.ComputeBuffer].AddNewResource(out ComputeBufferResource bufferResource);
            bufferResource.resource = computeBuffer;
            bufferResource.imported = true;

            return new ComputeBufferHandle(newHandle);
        }

        internal ComputeBufferHandle CreateComputeBuffer(in ComputeBufferDesc desc, int transientPassIndex = -1)
        {
            ValidateComputeBufferDesc(desc);

            int newHandle = m_Resources[(int)RenderGraphResourceType.ComputeBuffer].AddNewResource(out ComputeBufferResource bufferResource);
            bufferResource.desc = desc;
            bufferResource.transientPassIndex = transientPassIndex;

            return new ComputeBufferHandle(newHandle);
        }

        internal ComputeBufferDesc GetComputeBufferResourceDesc(in ResourceHandle handle)
        {
            return (m_Resources[(int)RenderGraphResourceType.ComputeBuffer].resourceArray[handle] as ComputeBufferResource).desc;
        }

        internal int GetComputeBufferResourceCount()
        {
            return m_Resources[(int)RenderGraphResourceType.ComputeBuffer].resourceArray.size;
        }

        ComputeBufferResource GetComputeBufferResource(in ResourceHandle handle)
        {
            return m_Resources[(int)RenderGraphResourceType.ComputeBuffer].resourceArray[handle] as ComputeBufferResource;
        }

        void ManageSharedResources()
        {
            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                var resources = m_Resources[type];
                for (int i = 0; i < resources.sharedResourcesCount; ++i)
                {
                    var resource = m_Resources[type].resourceArray[i];
                    // Alloc if needed.
                    if (resource.sharedResourceLastFrameUsed == m_CurrentFrameIndex && !resource.IsCreated())
                    {
                        // Here we want the resource to have the name given by users because we know that it won't be reused at all.
                        // So no need for an automatic generic name.
                        resource.CreateResource(resource.GetName());
                    }
                    // Release if not used anymore.
                    else
                    {
                        resource.ReleaseResource();
                    }
                }
            }
        }

        internal void CreatePooledResource(RenderGraphContext rgContext, int type, int index)
        {
            var resource = m_Resources[type].resourceArray[index];
            if (!resource.imported)
            {
                resource.CreatePooledResource();

                if (m_RenderGraphDebug.logFrameInformation)
                    resource.LogCreation(m_Logger);

                m_Resources[type].createResourceCallback?.Invoke(rgContext, resource);
            }
        }

        void CreateTextureCallback(RenderGraphContext rgContext, IRenderGraphResource res)
        {
            var resource = res as TextureResource;

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
                using (new ProfilingScope(rgContext.cmd, ProfilingSampler.Get(debugClear ? RenderGraphProfileId.RenderGraphClearDebug : RenderGraphProfileId.RenderGraphClear)))
                {
                    var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.Depth : ClearFlag.Color;
                    var clearColor = debugClear ? Color.magenta : resource.desc.clearColor;
                    CoreUtils.SetRenderTarget(rgContext.cmd, resource.resource, clearFlag, clearColor);
                }
            }
        }

        internal void ReleasePooledResource(RenderGraphContext rgContext, int type, int index)
        {
            var resource = m_Resources[type].resourceArray[index];

            if (!resource.imported)
            {
                    m_Resources[type].releaseResourceCallback?.Invoke(rgContext, resource);

                if (m_RenderGraphDebug.logFrameInformation)
                {
                    resource.LogRelease(m_Logger);
                }

                resource.ReleasePooledResource(m_CurrentFrameIndex);
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
                    CoreUtils.SetRenderTarget(rgContext.cmd, resource.resource, clearFlag, Color.magenta);
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
                m_Resources[i].Clear(onException, m_CurrentFrameIndex);
            m_RendererListResources.Clear();
        }

        internal void PurgeUnusedResources()
        {
            // TODO RENDERGRAPH: Might not be ideal to purge stale resources every frame.
            // In case users enable/disable features along a level it might provoke performance spikes when things are reallocated...
            // Will be much better when we have actual resource aliasing and we can manage memory more efficiently.
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_Resources[i].PurgeUnusedResources(m_CurrentFrameIndex);
        }

        internal void Cleanup()
        {
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_Resources[i].Cleanup();

            RTHandles.Release(m_CurrentBackbuffer);
        }

        void LogResources()
        {
            if (m_RenderGraphDebug.logResources)
            {
                m_Logger.LogLine("==== Allocated Resources ====\n");

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    m_Resources[type].pool.LogResources(m_Logger);
                    m_Logger.LogLine("");
                }
            }
        }

        #endregion
    }
}
