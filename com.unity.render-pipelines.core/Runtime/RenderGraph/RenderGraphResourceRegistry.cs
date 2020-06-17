using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// The RenderGraphResourceRegistry holds all resource allocated during Render Graph execution.
    /// </summary>
    public class RenderGraphResourceRegistry
    {
        static readonly ShaderTagId s_EmptyName = new ShaderTagId("");

        class IRenderGraphResource
        {
            public bool imported;
            public int  cachedHash;
            public int  shaderProperty;
            public int  transientPassIndex;
            public bool wasReleased;

            public virtual void Reset()
            {
                imported = false;
                cachedHash = -1;
                shaderProperty = 0;
                transientPassIndex = -1;
                wasReleased = false;
            }

            public virtual string GetName()
            {
                return "";
            }
        }

        #region Resources
        [DebuggerDisplay("Resource ({ResType}:{GetName()})")]
        class RenderGraphResource<DescType, ResType>
            : IRenderGraphResource
            where DescType : struct
            where ResType : class
        {
            public DescType desc;
            public ResType  resource;

            protected RenderGraphResource()
            {

            }

            public override void Reset()
            {
                base.Reset();
                resource = null;
            }
        }

        [DebuggerDisplay("TextureResource ({desc.name})")]
        class TextureResource : RenderGraphResource<TextureDesc, RTHandle>
        {
            public override string GetName()
            {
                return desc.name;
            }
        }

        [DebuggerDisplay("ComputeBufferResource ({desc.name})")]
        class ComputeBufferResource : RenderGraphResource<ComputeBufferDesc, ComputeBuffer>
        {
            public override string GetName()
            {
                return desc.name;
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

        DynamicArray<IRenderGraphResource>[] m_Resources = new DynamicArray<IRenderGraphResource>[(int)RenderGraphResourceType.Count];

        TexturePool                         m_TexturePool = new TexturePool();
        ComputeBufferPool                   m_ComputeBufferPool = new ComputeBufferPool();
        DynamicArray<RendererListResource>  m_RendererListResources = new DynamicArray<RendererListResource>();
        RTHandleSystem                      m_RTHandleSystem = new RTHandleSystem();
        RenderGraphDebugParams              m_RenderGraphDebug;
        RenderGraphLogger                   m_Logger;
        int                                 m_CurrentFrameIndex;

        RTHandle                            m_CurrentBackbuffer;

        #region Public Interface
        /// <summary>
        /// Returns the RTHandle associated with the provided resource handle.
        /// </summary>
        /// <param name="handle">Handle to a texture resource.</param>
        /// <returns>The RTHandle associated with the provided resource handle or null if the handle is invalid.</returns>
        public RTHandle GetTexture(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return null;

            return GetTextureResource(handle.handle).resource;
        }

        /// <summary>
        /// Returns the RendererList associated with the provided resource handle.
        /// </summary>
        /// <param name="handle">Handle to a Renderer List resource.</param>
        /// <returns>The Renderer List associated with the provided resource handle or an invalid renderer list if the handle is invalid.</returns>
        public RendererList GetRendererList(in RendererListHandle handle)
        {
            if (!handle.IsValid() || handle >= m_RendererListResources.size)
                return RendererList.nullRendererList;

            return m_RendererListResources[handle].rendererList;
        }

        /// <summary>
        /// Returns the Compute Buffer associated with the provided resource handle.
        /// </summary>
        /// <param name="handle">Handle to a Compute Buffer resource.</param>
        /// <returns>The Compute Buffer associated with the provided resource handle or a null reference if the handle is invalid.</returns>
        public ComputeBuffer GetComputeBuffer(in ComputeBufferHandle handle)
        {
            if (!handle.IsValid())
                return null;

            return GetComputeBufferResource(handle.handle).resource;
        }
        #endregion

        #region Internal Interface
        private RenderGraphResourceRegistry()
        {

        }

        internal RenderGraphResourceRegistry(bool supportMSAA, MSAASamples initialSampleCount, RenderGraphDebugParams renderGraphDebug, RenderGraphLogger logger)
        {
            // We initialize to screen width/height to avoid multiple realloc that can lead to inflated memory usage (as releasing of memory is delayed).
            m_RTHandleSystem.Initialize(Screen.width, Screen.height, supportMSAA, initialSampleCount);
            m_RenderGraphDebug = renderGraphDebug;
            m_Logger = logger;

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_Resources[i] = new DynamicArray<IRenderGraphResource>();
        }

        ResType GetResource<DescType, ResType>(DynamicArray<IRenderGraphResource> resourceArray, int index)
            where DescType : struct
            where ResType : class
        {
            var res = resourceArray[index] as RenderGraphResource<DescType, ResType>;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (res.resource == null && !res.wasReleased)
                throw new InvalidOperationException(string.Format("Trying to access resource \"{0}\" that was never created. Check that it was written at least once before trying to get it.", res.GetName()));

            if (res.resource == null && res.wasReleased)
                throw new InvalidOperationException(string.Format("Trying to access resource \"{0}\" that was already released. Check that the last pass where it's read is after this one.", res.GetName()));
#endif
            return res.resource;
        }

        internal void BeginRender(int width, int height, MSAASamples msaaSamples, int currentFrameIndex)
        {
            m_CurrentFrameIndex = currentFrameIndex;
            // Update RTHandleSystem with size for this rendering pass.
            m_RTHandleSystem.SetReferenceSize(width, height, msaaSamples);
        }

        internal RTHandleProperties GetRTHandleProperties() { return m_RTHandleSystem.rtHandleProperties; }

        void CheckHandleValidity(in ResourceHandle res)
        {
            var resources = m_Resources[res.iType];
            if (res.handle >= resources.size)
                throw new ArgumentException($"Trying to access resource of type {res.type} with an invalid resource index {res.handle}");
        }

        internal string GetResourceName(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType][res.handle].GetName();
        }

        internal bool IsResourceImported(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType][res.handle].imported;
        }

        internal int GetResourceTransientIndex(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_Resources[res.iType][res.handle].transientPassIndex;
        }

        // Texture Creation/Import APIs are internal because creation should only go through RenderGraph
        internal TextureHandle ImportTexture(RTHandle rt, int shaderProperty = 0)
        {
            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.Texture], out TextureResource texResource);
            texResource.resource = rt;
            texResource.imported = true;
            texResource.shaderProperty = shaderProperty;

            return new TextureHandle(newHandle);
        }

        internal TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
        {
            if (m_CurrentBackbuffer != null)
                m_CurrentBackbuffer.SetTexture(rt);
            else
                m_CurrentBackbuffer = m_RTHandleSystem.Alloc(rt);

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

        internal TextureHandle CreateTexture(in TextureDesc desc, int shaderProperty = 0, int transientPassIndex = -1)
        {
            ValidateTextureDesc(desc);

            int newHandle = AddNewResource(m_Resources[(int)RenderGraphResourceType.Texture], out TextureResource texResource);
            texResource.desc = desc;
            texResource.shaderProperty = shaderProperty;
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
                    string name = desc.name;
                    if (m_RenderGraphDebug.tagResourceNamesWithRG)
                        name = $"RenderGraph_{name}";

                    // Note: Name used here will be the one visible in the memory profiler so it means that whatever is the first pass that actually allocate the texture will set the name.
                    // TODO: Find a way to display name by pass.
                    switch (desc.sizeMode)
                    {
                        case TextureSizeMode.Explicit:
                            resource.resource = m_RTHandleSystem.Alloc(desc.width, desc.height, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                            desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.msaaSamples, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.name);
                            break;
                        case TextureSizeMode.Scale:
                            resource.resource = m_RTHandleSystem.Alloc(desc.scale, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                            desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.name);
                            break;
                        case TextureSizeMode.Functor:
                            resource.resource = m_RTHandleSystem.Alloc(desc.func, desc.slices, desc.depthBufferBits, desc.colorFormat, desc.filterMode, desc.wrapMode, desc.dimension, desc.enableRandomWrite,
                            desc.useMipMap, desc.autoGenerateMips, desc.isShadowMap, desc.anisoLevel, desc.mipMapBias, desc.enableMSAA, desc.bindTextureMS, desc.useDynamicScale, desc.memoryless, desc.name);
                            break;
                    }
                }

                //// Try to update name when re-using a texture.
                //// TODO RENDERGRAPH: Check if that actually works.
                //resource.rt.name = desc.name;

                resource.cachedHash = hashCode;

                var fastMemDesc = resource.desc.fastMemoryDesc;
                if(fastMemDesc.inFastMemory)
                {
                    resource.resource.SwitchToFastMemory(rgContext.cmd, fastMemDesc.residencyFraction, fastMemDesc.flags);
                }

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
                LogTextureCreation(resource.resource, resource.desc.clearBuffer || m_RenderGraphDebug.clearRenderTargetsAtCreation);
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
                    resource.resource.name = m_RenderGraphDebug.tagResourceNamesWithRG ? $"RenderGraph_{resource.desc.name}" : resource.desc.name;
                }
                resource.cachedHash = hashCode;

                m_ComputeBufferPool.RegisterFrameAllocation(hashCode, resource.resource);
                LogComputeBufferCreation(resource.resource);
            }
        }

        void SetGlobalTextures(RenderGraphContext rgContext, List<ResourceHandle> textures, bool bindDummyTexture)
        {
            foreach (var resource in textures)
            {
                var resourceDesc = GetTextureResource(resource);
                if (resourceDesc.shaderProperty != 0)
                {
                    if (resourceDesc.resource != null)
                    {
                        rgContext.cmd.SetGlobalTexture(resourceDesc.shaderProperty, bindDummyTexture ? TextureXR.GetMagentaTexture() : resourceDesc.resource);
                    }
                }
            }
        }


        internal void PreRenderPassSetGlobalTextures(RenderGraphContext rgContext, List<ResourceHandle> textures)
        {
            SetGlobalTextures(rgContext, textures, false);
        }

        internal void PostRenderPassUnbindGlobalTextures(RenderGraphContext rgContext, List<ResourceHandle> textures)
        {
            SetGlobalTextures(rgContext, textures, true);
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

                LogTextureRelease(resource.resource);
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

                LogComputeBufferRelease(resource.resource);
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
            // TODO RENDERGRAPH: Check actual condition on stride.
            //if (desc.stride % 4 != 0)
            //{
            //    throw new ArgumentException("Compute Buffer stride must be at least 4.");
            //}
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

            // TODO RENDERGRAPH: Might not be ideal to purge stale resources every frame.
            // In case users enable/disable features along a level it might provoke performance spikes when things are reallocated...
            // Will be much better when we have actual resource aliasing and we can manage memory more efficiently.
            m_TexturePool.PurgeUnusedResources(m_CurrentFrameIndex);
            m_ComputeBufferPool.PurgeUnusedResources(m_CurrentFrameIndex);
        }

        internal void ResetRTHandleReferenceSize(int width, int height)
        {
            m_RTHandleSystem.ResetReferenceSize(width, height);
        }

        internal void Cleanup()
        {
            m_TexturePool.Cleanup();
            m_ComputeBufferPool.Cleanup();
        }

        void LogTextureCreation(RTHandle rt, bool cleared)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine($"Created Texture: {rt.rt.name} (Cleared: {cleared})");
            }
        }

        void LogTextureRelease(RTHandle rt)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine($"Released Texture: {rt.rt.name}");
            }
        }

        void LogComputeBufferCreation(ComputeBuffer buffer)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine($"Created ComputeBuffer: {buffer}");
            }
        }

        void LogComputeBufferRelease(ComputeBuffer buffer)
        {
            if (m_RenderGraphDebug.logFrameInformation)
            {
                m_Logger.LogLine($"Released ComputeBuffer: {buffer}");
            }
        }

        void LogResources()
        {
            if (m_RenderGraphDebug.logResources)
            {
                m_Logger.LogLine("==== Allocated Resources ====\n");

                m_TexturePool.LogResources(m_Logger);
                m_ComputeBufferPool.LogResources(m_Logger);
            }
        }

        #endregion
    }
}
