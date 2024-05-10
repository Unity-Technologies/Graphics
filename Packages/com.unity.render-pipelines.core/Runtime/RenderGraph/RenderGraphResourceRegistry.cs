using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting.APIUpdating;
// Typedefs for the in-engine RendererList API (to avoid conflicts with the experimental version)
using CoreRendererList = UnityEngine.Rendering.RendererList;
using CoreRendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Basic properties of a RTHandle needed by the render graph compiler. It is not always possible to derive these
    /// given an RTHandle so the user needs to pass these in.
    /// 
    /// We don't use a full RenderTargetDescriptor here as filling out a full descriptor may not be trivial for users and not all
    /// members of the descriptor are actually used by the render graph. This struct is the minimum set of info needed by the render graph.
    /// If you want to develop some utility framework to work with render textures, etc. it's probably better to use RenderTargetDescriptor.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct RenderTargetInfo
    {
        /// <summary>
        /// The width in pixels of the render texture.
        /// </summary>
        public int width;
        /// <summary>
        /// The height in pixels of the render texture.
        /// </summary>
        public int height;
        /// <summary>
        /// The number of volume/array slices of the render texture.
        /// </summary>
        public int volumeDepth;
        /// <summary>
        /// The number of msaa samples in the render texture.
        /// </summary>
        public int msaaSamples;
        /// <summary>
        /// The Graphics format of the render texture.
        /// </summary>
        public GraphicsFormat format;
        /// <summary>
        /// Set to true if the render texture needs to be bound as a multisampled texture in a shader.
        /// </summary>
        public bool bindMS;
    }

    /// <summary>
    /// A helper struct describing the clear behavior of imported textures.
    /// </summary>
    public struct ImportResourceParams
    {
        /// <summary>
        /// Clear the imported texture the first time it is used by the graph.
        /// </summary>
        public bool clearOnFirstUse;
        /// <summary>
        /// The color to clear with on first use. Ignored if clearOnFirstUse==false;
        /// </summary>
        public Color clearColor;
        /// <summary>
        /// Discard the imported texture the last time it is used by the graph.
        /// If MSAA enabled, only the multisampled version is discarded while the MSAA surface is always resolved.
        /// Fully discarding both multisampled and resolved data is not currently possible.
        /// </summary>
        public bool discardOnLastUse;
    }

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

        delegate void ResourceCallback(InternalRenderGraphContext rgContext, IRenderGraphResource res);

        class RenderGraphResourcesData
        {
            public DynamicArray<IRenderGraphResource> resourceArray = new DynamicArray<IRenderGraphResource>();
            public int sharedResourcesCount;
            public IRenderGraphResourcePool pool;
            public ResourceCallback createResourceCallback;
            public ResourceCallback releaseResourceCallback;

            public RenderGraphResourcesData()
            {
                resourceArray.Resize(1); // Element 0 is the null element
            }

            public void Clear(bool onException, int frameIndex)
            {
                resourceArray.Resize(sharedResourcesCount+1); // First N elements are reserved for shared persistent resources and are kept as is. Element 0 is null

                if (pool != null)
                    pool.CheckFrameAllocation(onException, frameIndex);
            }

            public void Cleanup()
            {
                // Cleanup all shared resources.
                for (int i = 1; i < sharedResourcesCount+1; ++i)
                {
                    var resource = resourceArray[i];
                    if (resource != null)
                    {
                        resource.ReleaseGraphicsResource();
                    }
                }
                // Then cleanup the pool
                if (pool != null)
                    pool.Cleanup();
            }

            public void PurgeUnusedGraphicsResources(int frameIndex)
            {
                if (pool != null)
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
        DynamicArray<RendererListLegacyResource> m_RendererListLegacyResources = new DynamicArray<RendererListLegacyResource>();

        RenderGraphDebugParams m_RenderGraphDebug;
        RenderGraphLogger m_ResourceLogger = new RenderGraphLogger();
        RenderGraphLogger m_FrameInformationLogger; // Comes from the RenderGraph instance.
        int m_CurrentFrameIndex;
        int m_ExecutionCount;

        RTHandle m_CurrentBackbuffer;

        const int kInitialRendererListCount = 256;
        List<CoreRendererList> m_ActiveRendererLists = new List<CoreRendererList>(kInitialRendererListCount);

        #region Internal Interface
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckTextureResource(TextureResource texResource)
        {
            if (texResource.graphicsResource == null && !texResource.imported)
                throw new InvalidOperationException($"Trying to use a texture ({texResource.GetName()}) that was already released or not yet created. Make sure you declare it for reading in your pass or you don't read it before it's been written to at least once.");
        }

        internal RTHandle GetTexture(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return null;

            var texResource = GetTextureResource(handle.handle);
            CheckTextureResource(texResource);
            return texResource.graphicsResource;
        }

        // This index overload will not check frame validity.
        // Its only purpose is because the NRP compiled graph stores resource handles that would not be frame valid when reused.
        internal RTHandle GetTexture(int index)
        {
            var texResource = GetTextureResource(index);
            CheckTextureResource(texResource);
            return texResource.graphicsResource;
        }

        internal bool TextureNeedsFallback(in TextureHandle handle)
        {
            if (!handle.IsValid())
                return false;

            return GetTextureResource(handle.handle).NeedsFallBack();
        }

        internal CoreRendererList GetRendererList(in RendererListHandle handle)
        {
            if (!handle.IsValid())
                return CoreRendererList.nullRendererList;

            switch (handle.type)
            {
                case RendererListHandleType.Renderers:
                    {
                        if (handle >= m_RendererListResources.size)
                            return CoreRendererList.nullRendererList;
                        return m_RendererListResources[handle].rendererList;
                    }
                case RendererListHandleType.Legacy:
                    {
                        if (handle >= m_RendererListLegacyResources.size)
                            return CoreRendererList.nullRendererList;
                        if (!m_RendererListLegacyResources[handle].isActive)
                            return CoreRendererList.nullRendererList;
                        return m_RendererListLegacyResources[handle].rendererList;
                    }
            }

            return CoreRendererList.nullRendererList;
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckBufferResource(BufferResource bufferResoruce)
        {
            if (bufferResoruce.graphicsResource == null)
                throw new InvalidOperationException("Trying to use a graphics buffer ({bufferResource.GetName()}) that was already released or not yet created. Make sure you declare it for reading in your pass or you don't read it before it's been written to at least once.");
        }

        internal GraphicsBuffer GetBuffer(in BufferHandle handle)
        {
            if (!handle.IsValid())
                return null;

            var bufferResource = GetBufferResource(handle.handle);
            CheckBufferResource(bufferResource);

            return bufferResource.graphicsResource;
        }

        // This index overload will not check frame validity.
        // Its only purpose is because the NRP compiled graph stores resource handles that would not be frame valid when reused.
        internal GraphicsBuffer GetBuffer(int index)
        {
            var bufferResource = GetBufferResource(index);
            CheckBufferResource(bufferResource);

            return bufferResource.graphicsResource;
        }

        internal RayTracingAccelerationStructure GetRayTracingAccelerationStructure(in RayTracingAccelerationStructureHandle handle)
        {
            if (!handle.IsValid())
                return null;

            var accelStructureResource = GetRayTracingAccelerationStructureResource(handle.handle);
            var resource = accelStructureResource.graphicsResource;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (resource == null)
                throw new InvalidOperationException("Trying to use a acceleration structure ({accelStructureResource.GetName()}) that was already released or not yet created. Make sure you declare it for reading in your pass or you don't read it before it's been written to at least once.");
#endif

            return resource;
        }

        internal int GetSharedResourceCount(RenderGraphResourceType type)
        {
            return m_RenderGraphResources[(int)type].sharedResourcesCount;
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

            m_RenderGraphResources[(int)RenderGraphResourceType.Buffer].pool = new BufferPool();

            // RayTracingAccelerationStructures can be imported only.
            m_RenderGraphResources[(int)RenderGraphResourceType.AccelerationStructure].pool = null;
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

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckHandleValidity(in ResourceHandle res)
        {
            CheckHandleValidity(res.type, res.index);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckHandleValidity(RenderGraphResourceType type, int index)
        {
            if(RenderGraph.enableValidityChecks)
            {
                var resources = m_RenderGraphResources[(int)type].resourceArray;
                if (index == 0)
                    throw new ArgumentException($"Trying to access resource of type {type} with an null resource index.");
                if (index >= resources.size)
                    throw new ArgumentException($"Trying to access resource of type {type} with an invalid resource index {index}");
            }
        }

        internal void IncrementWriteCount(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            m_RenderGraphResources[res.iType].resourceArray[res.index].IncrementWriteCount();
        }

        internal void NewVersion(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            m_RenderGraphResources[res.iType].resourceArray[res.index].NewVersion();
        }

        internal ResourceHandle GetLatestVersionHandle(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            var ver = m_RenderGraphResources[res.iType].resourceArray[res.index].version;
            if (IsRenderGraphResourceShared(res))
            {
                ver -= m_ExecutionCount; //TODO(ddebaets) is this a good solution ?
            }
            return new ResourceHandle(res, ver);
        }

        internal int GetLatestVersionNumber(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            var ver = m_RenderGraphResources[res.iType].resourceArray[res.index].version;
            if (IsRenderGraphResourceShared(res))
            {
                ver -= m_ExecutionCount;//TODO(ddebaets) is this a good solution ?
            }
            return ver;
        }

        internal ResourceHandle GetZeroVersionedHandle(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return new ResourceHandle(res, 0);
        }

        internal ResourceHandle GetNewVersionedHandle(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            var ver = m_RenderGraphResources[res.iType].resourceArray[res.index].NewVersion();
            if (IsRenderGraphResourceShared(res))
            {
                ver -= m_ExecutionCount;//TODO(ddebaets) is this a good solution ?
            }
            return new ResourceHandle(res, ver);
        }

        internal IRenderGraphResource GetResourceLowLevel(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_RenderGraphResources[res.iType].resourceArray[res.index];
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

        internal bool IsRenderGraphResourceForceReleased(RenderGraphResourceType type, int index)
        {
            CheckHandleValidity(type, index);
            return m_RenderGraphResources[(int)type].resourceArray[index].forceRelease;
        }

        internal bool IsRenderGraphResourceShared(RenderGraphResourceType type, int index)
        {
            CheckHandleValidity(type, index);
            return index <= m_RenderGraphResources[(int)type].sharedResourcesCount;
        }

        internal bool IsRenderGraphResourceShared(in ResourceHandle res) => IsRenderGraphResourceShared(res.type, res.index);

        internal bool IsGraphicsResourceCreated(in ResourceHandle res)
        {
            CheckHandleValidity(res);
            return m_RenderGraphResources[res.iType].resourceArray[res.index].IsCreated();
        }

        internal bool IsRendererListCreated(in RendererListHandle res)
        {
            switch (res.type)
            {
                case RendererListHandleType.Renderers:
                    return m_RendererListResources[res].rendererList.isValid;
                case RendererListHandleType.Legacy:
                    return m_RendererListLegacyResources[res].isActive && m_RendererListLegacyResources[res].rendererList.isValid;
            }
            return false;
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
        internal TextureHandle ImportTexture(in RTHandle rt, bool isBuiltin = false)
        {
            ImportResourceParams importParams = new ImportResourceParams();
            importParams.clearOnFirstUse = false;
            importParams.discardOnLastUse = false;

            return ImportTexture(rt, importParams, isBuiltin);
        }

        // Texture Creation/Import APIs are internal because creation should only go through RenderGraph
        internal TextureHandle ImportTexture(in RTHandle rt, in ImportResourceParams importParams, bool isBuiltin = false)
        {
            // Apparently existing code tries to import null textures !?? So we sort of allow them then :(
            // Not sure what this actually "means" it allocates a RG handle but nothing is behind it
            if (rt != null)
            {
                // Imported, try to get back to the original handle we imported and get the properties from there
                if (rt.m_RT != null)
                {
                    // RTHandle wrapping a RenderTexture, ok we can get properties from that
                }
                else if (rt.m_ExternalTexture != null)
                {
                    // RTHandle wrapping a regular 2D texture we can't render to that
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                else if (rt.m_NameID != emptyId)
                {

                    // RTHandle wrapping a RenderTargetIdentifier
                    throw new Exception("Invalid import, you are importing a texture handle that wraps a RenderTargetIdentifier. The render graph can't know the properties of these textures so please use the ImportTexture overload that takes a RenderTargetInfo argument instead.");
                }
                else
                {
                    throw new Exception("Invalid render target handle: RT, External texture and NameID are all null or zero.");
                }
#endif
            }

            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Texture].AddNewRenderGraphResource(out TextureResource texResource);
            texResource.graphicsResource = rt;
            texResource.imported = true;

            RenderTexture renderTexture = (rt != null) ? ((rt.m_RT != null) ? rt.m_RT : (rt.m_ExternalTexture as RenderTexture)) : null;
            if (renderTexture)
            {
                texResource.desc = new TextureDesc(renderTexture);
                texResource.validDesc = true;
            }
            texResource.desc.clearBuffer = importParams.clearOnFirstUse;
            texResource.desc.clearColor = importParams.clearColor;
            texResource.desc.discardBuffer = importParams.discardOnLastUse;

            var texHandle = new TextureHandle(newHandle, false, isBuiltin);

            // Try getting the info straight away so if something is wrong we throw at import time.
            // It is invalid to import a texture if we can't get its info somehow.
            // (The alternative is for the code calling ImportTexture to use the overload that takes a RenderTargetInfo).
            if(rt != null)
                ValidateRenderTarget(texHandle.handle);

            return texHandle;
        }

        // Texture Creation/Import APIs are internal because creation should only go through RenderGraph
        internal TextureHandle ImportTexture(in RTHandle rt, RenderTargetInfo info, in ImportResourceParams importParams)
        {
            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Texture].AddNewRenderGraphResource(out TextureResource texResource);
            texResource.graphicsResource = rt;
            texResource.imported = true;
            // Be sure to clear the desc to the default state
            texResource.desc = new TextureDesc();

            // Apparently existing code tries to import null textures !?? So we sort of allow them then :(
            if (rt != null)
            {
                if (rt.m_NameID != emptyId)
                {
                    // Store the info in the descriptor structure to avoid having a separate info structure being saved per resource
                    // This descriptor will then be used to reconstruct the info (see GetRenderTargetInfo) but is not a full featured descriptor.
                    // This is ok as this descriptor will never be used to create textures (as they are imported into the graph and thus externally created).
                    texResource.desc.width = info.width;
                    texResource.desc.height = info.height;
                    texResource.desc.slices = info.volumeDepth;

                    texResource.desc.msaaSamples = (MSAASamples)info.msaaSamples;
                    texResource.desc.colorFormat = info.format;
                    texResource.desc.bindTextureMS = info.bindMS;
                    texResource.desc.clearBuffer = importParams.clearOnFirstUse;
                    texResource.desc.clearColor = importParams.clearColor;
                    texResource.desc.discardBuffer = importParams.discardOnLastUse;
                    texResource.validDesc = false; // The desc above just contains enough info to make RenderTargetInfo not a full descriptor.
                                                   // This means GetRenderTargetInfo will work for the handle but GetTextureResourceDesc will throw
                }
                // Anything else is an error and should take the overload not taking a RenderTargetInfo
                else
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    throw new Exception("Invalid import, you are importing a texture handle that isn't wrapping a RenderTargetIdentifier. You cannot use the overload taking RenderTargetInfo as the graph will automatically determine the texture properties based on the passed in handle.");
#endif
                }
            }
            else
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                throw new Exception("Invalid import, null handle.");
#endif
            }

            var texHandle = new TextureHandle(newHandle);

            // Try getting the info straight away so if something is wrong we throw at import time.
            ValidateRenderTarget(texHandle.handle);

            return texHandle;
        }

        internal TextureHandle CreateSharedTexture(in TextureDesc desc, bool explicitRelease)
        {
            var textureResources = m_RenderGraphResources[(int)RenderGraphResourceType.Texture];
            int sharedTextureCount = textureResources.sharedResourcesCount;

            Debug.Assert(textureResources.resourceArray.size <= sharedTextureCount+1);

            // try to find an available slot.
            TextureResource texResource = null;
            int textureIndex = -1;

            for (int i = 1; i < sharedTextureCount+1; ++i)
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
            texResource.validDesc = true;

            return new TextureHandle(textureIndex, shared: true);
        }

        internal void RefreshSharedTextureDesc(in TextureHandle texture, in TextureDesc desc)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!IsRenderGraphResourceShared(RenderGraphResourceType.Texture, texture.handle.index))
            {
                throw new InvalidOperationException($"Trying to refresh texture {texture} that is not a shared resource.");
            }
#endif
            var texResource = GetTextureResource(texture.handle);
            texResource.ReleaseGraphicsResource();
            texResource.desc = desc;
        }

        internal void ReleaseSharedTexture(in TextureHandle texture)
        {
            var texResources = m_RenderGraphResources[(int)RenderGraphResourceType.Texture];

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (texture.handle.index == 0 || texture.handle.index >= texResources.sharedResourcesCount+1)
                throw new InvalidOperationException("Tried to release a non shared texture.");
#endif

            // Decrement if we release the last one.
            if (texture.handle.index == (texResources.sharedResourcesCount))
                texResources.sharedResourcesCount--;

            var texResource = GetTextureResource(texture.handle);
            texResource.ReleaseGraphicsResource();
            texResource.Reset();
        }

        internal TextureHandle ImportBackbuffer(RenderTargetIdentifier rt, in RenderTargetInfo info, in ImportResourceParams importParams)
        {
            if (m_CurrentBackbuffer != null)
                m_CurrentBackbuffer.SetTexture(rt);
            else
                m_CurrentBackbuffer = RTHandles.Alloc(rt, "Backbuffer");

            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Texture].AddNewRenderGraphResource(out TextureResource texResource);
            texResource.graphicsResource = m_CurrentBackbuffer;
            texResource.imported = true;
            texResource.desc = new TextureDesc();
            texResource.desc.width = info.width;
            texResource.desc.height = info.height;
            texResource.desc.slices = info.volumeDepth;
            texResource.desc.msaaSamples = (MSAASamples)info.msaaSamples;
            texResource.desc.bindTextureMS = info.bindMS;
            texResource.desc.colorFormat = info.format;
            texResource.desc.clearBuffer = importParams.clearOnFirstUse;
            texResource.desc.clearColor = importParams.clearColor;
            texResource.desc.discardBuffer = importParams.discardOnLastUse;
            texResource.validDesc = false;// The desc above just contains enough info to make RenderTargetInfo not a full descriptor.
                                          // This means GetRenderTargetInfo will work for the handle but GetTextureResourceDesc will throw

            var texHandle = new TextureHandle(newHandle);

            // Try getting the info straight away so if something wrong we get the exceptions directly at import time.
            ValidateRenderTarget(texHandle.handle);

            return texHandle;
        }

        static RenderTargetIdentifier emptyId = new RenderTargetIdentifier();
        static RenderTargetIdentifier builtinCameraRenderTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void ValidateRenderTarget(in ResourceHandle res)
        {
            if(RenderGraph.enableValidityChecks)
            {
                RenderTargetInfo outInfo;
                GetRenderTargetInfo(res, out outInfo);
            }
        }

        internal void GetRenderTargetInfo(in ResourceHandle res, out RenderTargetInfo outInfo)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (res.iType != (int)RenderGraphResourceType.Texture)
            {
                outInfo = new RenderTargetInfo();
                throw new ArgumentException("Invalid Resource Handle passed to GetRenderTargetInfo");
            }
#endif
            // You can never have enough ways to reference a render target...
            // Lots of legacy if's and but's
            TextureResource tex = GetTextureResource(res);
            if (tex.imported)
            {
                // Imported, try to get back to the original handle we imported and get the properties from there
                RTHandle handle = tex.graphicsResource;
                if (handle == null)
                {
                    // Apparently existing code tries to import null textures !?? So we sort of allow them then :(
                    // throw new Exception("Invalid imported texture. The RTHandle provided was null.");
                    outInfo = new RenderTargetInfo();
                }
                else if (handle.m_RT != null)
                {
                    outInfo = new RenderTargetInfo();
                    outInfo.width = handle.m_RT.width;
                    outInfo.height = handle.m_RT.height;
                    outInfo.volumeDepth = handle.m_RT.volumeDepth;
                    // If it's depth only, graphics format is null but depthStencilFormat is the real format
                    outInfo.format = (handle.m_RT.graphicsFormat != GraphicsFormat.None) ? handle.m_RT.graphicsFormat : handle.m_RT.depthStencilFormat;
                    outInfo.msaaSamples = handle.m_RT.antiAliasing;
                    outInfo.bindMS = handle.m_RT.bindTextureMS;
                }
                else if (handle.m_ExternalTexture != null)
                {
                    outInfo = new RenderTargetInfo();
                    outInfo.width = handle.m_ExternalTexture.width;
                    outInfo.height = handle.m_ExternalTexture.height;
                    outInfo.volumeDepth = 1; // XRTODO: Check dimension instead?
                    if (handle.m_ExternalTexture is RenderTexture)
                    {
                        RenderTexture rt = (RenderTexture)handle.m_ExternalTexture;
                        // If it's depth only, graphics format is null but depthStencilFormat is the real format
                        outInfo.format = (rt.graphicsFormat != GraphicsFormat.None) ? rt.graphicsFormat : rt.depthStencilFormat;
                        outInfo.msaaSamples = rt.antiAliasing;
                    }
                    else
                    {
                        //Note: This case will likely not work when used as an actual rendertarget. This is a regular 2D, Cube,...
                        //texture an not a rendertarget texture so it will fail when bound as a rendertarget later.
                        outInfo.format = handle.m_ExternalTexture.graphicsFormat;
                        outInfo.msaaSamples = 1;
                    }
                    outInfo.bindMS = false;
                }
                else if (handle.m_NameID != emptyId)
                {
                    // WE can't really get info about RenderTargetIdentifier back. It simply doesn't expose this info.
                    // But in some cases like the camera built-in target it also cant. If it's a BuiltinRenderTextureType
                    // we can't know from the size/format/... from the enum. It's implicitly defined by the current camera,
                    // screen resolution,.... we can't even hope to know or replicate the size calculation here
                    // so we just say we don't know what this rt is and rely on the user passing in the info to us.
                    var desc = GetTextureResourceDesc(res, true);
                    outInfo = new RenderTargetInfo();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    if (desc.width == 0 || desc.height == 0 || desc.slices == 0 || desc.msaaSamples == 0 || desc.colorFormat == GraphicsFormat.None)
                    {
                        throw new Exception("Invalid imported texture. A RTHandle wrapping an RenderTargetIdentifier was imported without providing valid RenderTargetInfo.");
                    }
#endif
                    outInfo.width = desc.width;
                    outInfo.height = desc.height;
                    outInfo.volumeDepth = desc.slices;

                    outInfo.msaaSamples = (int)desc.msaaSamples;
                    outInfo.format = desc.colorFormat;
                    outInfo.bindMS = desc.bindTextureMS;
                }
                else
                {
                    throw new Exception("Invalid imported texture. The RTHandle provided is invalid.");
                }
            }
            else
            {
                // Managed by rendergraph, it might not be created yet so we look at the desc to find out
                var desc = GetTextureResourceDesc(res);
                var dim = desc.CalculateFinalDimensions();
                outInfo = new RenderTargetInfo();
                outInfo.width = dim.x;
                outInfo.height = dim.y;
                outInfo.volumeDepth = desc.slices;

                outInfo.msaaSamples = (int)desc.msaaSamples;
                outInfo.bindMS = desc.bindTextureMS;

                if (desc.isShadowMap || desc.depthBufferBits != DepthBits.None)
                {
                    var format = desc.isShadowMap ? DefaultFormat.Shadow : DefaultFormat.DepthStencil;
                    outInfo.format = SystemInfo.GetGraphicsFormat(format);
                }
                else
                {
                    outInfo.format = desc.colorFormat;
                }
            }
        }

        internal TextureHandle CreateTexture(in TextureDesc desc, int transientPassIndex = -1)
        {
            ValidateTextureDesc(desc);

            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Texture].AddNewRenderGraphResource(out TextureResource texResource);
            texResource.desc = desc;
            texResource.validDesc = true;
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

        internal TextureResource GetTextureResource(in ResourceHandle handle)
        {
            Debug.Assert(handle.type == RenderGraphResourceType.Texture);
            return m_RenderGraphResources[(int)RenderGraphResourceType.Texture].resourceArray[handle.index] as TextureResource;
        }

        internal TextureResource GetTextureResource(int index)
        {
            return m_RenderGraphResources[(int)RenderGraphResourceType.Texture].resourceArray[index] as TextureResource;
        }

        internal TextureDesc GetTextureResourceDesc(in ResourceHandle handle, bool noThrowOnInvalidDesc = false)
        {
            Debug.Assert(handle.type == RenderGraphResourceType.Texture);
            var texture = (m_RenderGraphResources[(int)RenderGraphResourceType.Texture].resourceArray[handle.index] as TextureResource);
            if (!texture.validDesc && !noThrowOnInvalidDesc)
                throw new ArgumentException("The passed in texture handle does not have a valid descriptor. (This is most commonly cause by the handle referencing a built-in texture such as the system back buffer.)", "handle");
            return texture.desc;
        }

        internal RendererListHandle CreateRendererList(in CoreRendererListDesc desc)
        {
            ValidateRendererListDesc(desc);

            int newHandle = m_RendererListResources.Add(new RendererListResource(CoreRendererListDesc.ConvertToParameters(desc)));
            return new RendererListHandle(newHandle);
        }

        internal RendererListHandle CreateRendererList(in RendererListParams desc)
        {
            int newHandle = m_RendererListResources.Add(new RendererListResource(desc));
            return new RendererListHandle(newHandle);
        }

        internal RendererListHandle CreateShadowRendererList(ScriptableRenderContext context, ref ShadowDrawingSettings shadowDrawinSettings)
        {
            RendererListLegacyResource resource = new RendererListLegacyResource();
            resource.rendererList = context.CreateShadowRendererList(ref shadowDrawinSettings);
            int newHandle = m_RendererListLegacyResources.Add(resource);
            return new RendererListHandle(newHandle, RendererListHandleType.Legacy);
        }

        internal RendererListHandle CreateGizmoRendererList(ScriptableRenderContext context, in Camera camera, in GizmoSubset gizmoSubset)
        {
            RendererListLegacyResource resource = new RendererListLegacyResource();
            resource.rendererList = context.CreateGizmoRendererList(camera, gizmoSubset);
            int newHandle = m_RendererListLegacyResources.Add(resource);
            return new RendererListHandle(newHandle, RendererListHandleType.Legacy);
        }

        internal RendererListHandle CreateUIOverlayRendererList(ScriptableRenderContext context, in Camera camera, in UISubset uiSubset)
        {
            RendererListLegacyResource resource = new RendererListLegacyResource();
            resource.rendererList = context.CreateUIOverlayRendererList(camera, uiSubset);
            int newHandle = m_RendererListLegacyResources.Add(resource);
            return new RendererListHandle(newHandle, RendererListHandleType.Legacy);
        }

        internal RendererListHandle CreateWireOverlayRendererList(ScriptableRenderContext context, in Camera camera)
        {
            RendererListLegacyResource resource = new RendererListLegacyResource();
            resource.rendererList = context.CreateWireOverlayRendererList(camera);
            int newHandle = m_RendererListLegacyResources.Add(resource);
            return new RendererListHandle(newHandle, RendererListHandleType.Legacy);
        }

        internal RendererListHandle CreateSkyboxRendererList(ScriptableRenderContext context, in Camera camera)
        {
            RendererListLegacyResource resource = new RendererListLegacyResource();
            resource.rendererList = context.CreateSkyboxRendererList(camera);
            int newHandle = m_RendererListLegacyResources.Add(resource);
            return new RendererListHandle(newHandle, RendererListHandleType.Legacy);
        }

        internal RendererListHandle CreateSkyboxRendererList(ScriptableRenderContext context, in Camera camera, Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix)
        {
            RendererListLegacyResource resource = new RendererListLegacyResource();
            resource.rendererList = context.CreateSkyboxRendererList(camera, projectionMatrix, viewMatrix);
            int newHandle = m_RendererListLegacyResources.Add(resource);
            return new RendererListHandle(newHandle, RendererListHandleType.Legacy);
        }

        internal RendererListHandle CreateSkyboxRendererList(ScriptableRenderContext context, in Camera camera, Matrix4x4 projectionMatrixL, Matrix4x4 viewMatrixL, Matrix4x4 projectionMatrixR, Matrix4x4 viewMatrixR)
        {
            RendererListLegacyResource resource = new RendererListLegacyResource();
            resource.rendererList = context.CreateSkyboxRendererList(camera, projectionMatrixL, viewMatrixL, projectionMatrixR, viewMatrixR);
            int newHandle = m_RendererListLegacyResources.Add(resource);
            return new RendererListHandle(newHandle, RendererListHandleType.Legacy);
        }

        internal BufferHandle ImportBuffer(GraphicsBuffer graphicsBuffer, bool forceRelease = false)
        {
            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Buffer].AddNewRenderGraphResource(out BufferResource bufferResource);
            bufferResource.graphicsResource = graphicsBuffer;
            bufferResource.imported = true;
            bufferResource.forceRelease = forceRelease;
            bufferResource.validDesc = false;

            return new BufferHandle(newHandle);
        }

        internal BufferHandle CreateBuffer(in BufferDesc desc, int transientPassIndex = -1)
        {
            ValidateBufferDesc(desc);

            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.Buffer].AddNewRenderGraphResource(out BufferResource bufferResource);
            bufferResource.desc = desc;
            bufferResource.validDesc = true;
            bufferResource.transientPassIndex = transientPassIndex;

            return new BufferHandle(newHandle);
        }

        internal BufferDesc GetBufferResourceDesc(in ResourceHandle handle, bool noThrowOnInvalidDesc = false)
        {
            Debug.Assert(handle.type == RenderGraphResourceType.Buffer);
            var buffer = (m_RenderGraphResources[(int)RenderGraphResourceType.Buffer].resourceArray[handle.index] as BufferResource);
            if (!buffer.validDesc && !noThrowOnInvalidDesc)
                throw new ArgumentException("The passed in buffer handle does not have a valid descriptor. (This is most commonly cause by importing the buffer.)", "handle");
            return buffer.desc;
        }

        internal int GetBufferResourceCount()
        {
            return GetResourceCount(RenderGraphResourceType.Buffer);
        }

        BufferResource GetBufferResource(in ResourceHandle handle)
        {
            Debug.Assert(handle.type == RenderGraphResourceType.Buffer);
            return m_RenderGraphResources[(int)RenderGraphResourceType.Buffer].resourceArray[handle.index] as BufferResource;
        }

        BufferResource GetBufferResource(int index)
        {
            return m_RenderGraphResources[(int)RenderGraphResourceType.Buffer].resourceArray[index] as BufferResource;
        }

        RayTracingAccelerationStructureResource GetRayTracingAccelerationStructureResource(in ResourceHandle handle)
        {
            return m_RenderGraphResources[(int)RenderGraphResourceType.AccelerationStructure].resourceArray[handle.index] as RayTracingAccelerationStructureResource;
        }

        internal int GetRayTracingAccelerationStructureResourceCount()
        {
            return GetResourceCount(RenderGraphResourceType.AccelerationStructure);
        }

        internal RayTracingAccelerationStructureHandle ImportRayTracingAccelerationStructure(in RayTracingAccelerationStructure accelStruct, string name)
        {
            int newHandle = m_RenderGraphResources[(int)RenderGraphResourceType.AccelerationStructure].AddNewRenderGraphResource(out RayTracingAccelerationStructureResource accelStructureResource, false);
            accelStructureResource.graphicsResource = accelStruct;
            accelStructureResource.imported = true;
            accelStructureResource.forceRelease = false;
            accelStructureResource.desc.name = name;

            return new RayTracingAccelerationStructureHandle(newHandle);
        }

        internal void UpdateSharedResourceLastFrameIndex(int type, int index)
        {
            m_RenderGraphResources[type].resourceArray[index].sharedResourceLastFrameUsed = m_ExecutionCount;
        }
        internal void UpdateSharedResourceLastFrameIndex(in ResourceHandle handle) => UpdateSharedResourceLastFrameIndex((int)handle.type, handle.index);


        void ManageSharedRenderGraphResources()
        {
            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                var resources = m_RenderGraphResources[type];
                for (int i = 1; i < resources.sharedResourcesCount+1; ++i)
                {
                    var resource = m_RenderGraphResources[type].resourceArray[i];
                    bool isCreated = resource.IsCreated();
                    // Alloc if needed
                    if (resource.sharedResourceLastFrameUsed == m_ExecutionCount && !isCreated)
                    {
                        // User name provided through the resource desc will be used
                        resource.CreateGraphicsResource();
                    }
                    // Release if not used anymore
                    else if (isCreated && !resource.sharedExplicitRelease && ((resource.sharedResourceLastFrameUsed + kSharedResourceLifetime) < m_ExecutionCount))
                    {
                        resource.ReleaseGraphicsResource();
                    }
                }
            }
        }

        internal void CreatePooledResource(InternalRenderGraphContext rgContext, int type, int index)
        {
            Debug.Assert(index != 0, "Index 0 indicates the null object it can't be used here");

            var resource = m_RenderGraphResources[type].resourceArray[index];
            if (!resource.imported)
            {
                resource.CreatePooledGraphicsResource();

                if (m_RenderGraphDebug.enableLogging)
                    resource.LogCreation(m_FrameInformationLogger);

                m_RenderGraphResources[type].createResourceCallback?.Invoke(rgContext, resource);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CreatePooledResource(InternalRenderGraphContext rgContext, in ResourceHandle handle)
        {
            CreatePooledResource(rgContext, handle.iType, handle.index);
        }

        // Only modified by native compiler when using native render pass
        internal bool forceManualClearOfResource = true;

        void CreateTextureCallback(InternalRenderGraphContext rgContext, IRenderGraphResource res)
        {
            var resource = res as TextureResource;

#if UNITY_2020_2_OR_NEWER
            var fastMemDesc = resource.desc.fastMemoryDesc;
            if (fastMemDesc.inFastMemory)
            {
                resource.graphicsResource.SwitchToFastMemory(rgContext.cmd, fastMemDesc.residencyFraction, fastMemDesc.flags);
            }
#endif

            if ((forceManualClearOfResource && resource.desc.clearBuffer) || m_RenderGraphDebug.clearRenderTargetsAtCreation)
            {
                bool debugClear = m_RenderGraphDebug.clearRenderTargetsAtCreation && !resource.desc.clearBuffer;
                var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.DepthStencil : ClearFlag.Color;
                var clearColor = debugClear ? Color.magenta : resource.desc.clearColor;
                CoreUtils.SetRenderTarget(rgContext.cmd, resource.graphicsResource, clearFlag, clearColor);
            }
        }

        internal void ReleasePooledResource(InternalRenderGraphContext rgContext, int type, int index)
        {
            var resource = m_RenderGraphResources[type].resourceArray[index];

            if (!resource.imported || resource.forceRelease)
            {
                m_RenderGraphResources[type].releaseResourceCallback?.Invoke(rgContext, resource);

                if (m_RenderGraphDebug.enableLogging)
                {
                    resource.LogRelease(m_FrameInformationLogger);
                }

                resource.ReleasePooledGraphicsResource(m_CurrentFrameIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReleasePooledResource(InternalRenderGraphContext rgContext, in ResourceHandle handle)
        {
            ReleasePooledResource(rgContext, handle.iType, handle.index);
        }

        void ReleaseTextureCallback(InternalRenderGraphContext rgContext, IRenderGraphResource res)
        {
            var resource = res as TextureResource;

            if (m_RenderGraphDebug.clearRenderTargetsAtRelease)
            {
                var clearFlag = resource.desc.depthBufferBits != DepthBits.None ? ClearFlag.DepthStencil : ClearFlag.Color;
                CoreUtils.SetRenderTarget(rgContext.cmd, resource.graphicsResource, clearFlag, Color.magenta);
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void ValidateTextureDesc(in TextureDesc desc)
        {
            if(RenderGraph.enableValidityChecks)
            {
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
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void ValidateRendererListDesc(in CoreRendererListDesc desc)
        {
            if(RenderGraph.enableValidityChecks)
            {
                if (!desc.IsValid())
                {
                    throw new ArgumentException("Renderer List descriptor is not valid.");
                }

                if (desc.renderQueueRange.lowerBound == 0 && desc.renderQueueRange.upperBound == 0)
                {
                    throw new ArgumentException("Renderer List creation descriptor must have a valid RenderQueueRange.");
                }
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void ValidateBufferDesc(in BufferDesc desc)
        {
            if(RenderGraph.enableValidityChecks)
            {
                if (desc.stride % 4 != 0)
                {
                    throw new ArgumentException("Invalid Graphics Buffer creation descriptor: Graphics Buffer stride must be at least 4.");
                }
                if (desc.count == 0)
                {
                    throw new ArgumentException("Invalid Graphics Buffer creation descriptor: Graphics Buffer count  must be non zero.");
                }
            }
        }

        internal void CreateRendererLists(List<RendererListHandle> rendererLists, ScriptableRenderContext context, bool manualDispatch = false)
        {
            // We gather the active renderer lists of a frame in a list/array before we pass it in the core API for batch processing
            m_ActiveRendererLists.Clear();

            foreach (var rendererList in rendererLists)
            {
                switch(rendererList.type)
                {
                    case RendererListHandleType.Renderers:
                    {
                        ref var rendererListResource = ref m_RendererListResources[rendererList];
                        ref var desc = ref rendererListResource.desc;
                        rendererListResource.rendererList = context.CreateRendererList(ref desc);
                        m_ActiveRendererLists.Add(rendererListResource.rendererList);
                        break;
                    }
                    case RendererListHandleType.Legacy:
                    {
                        // Legacy rendererLists are created upfront in recording phase. Simply activate them.
                        ref var rendererListResource = ref m_RendererListLegacyResources[rendererList];
                        rendererListResource.isActive = true;
                        break;
                    }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    default:
                    {
                        throw new ArgumentException("Invalid RendererListHandle: RendererListHandleType is not recognized.");
                    }
#endif
                }

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
            m_RendererListLegacyResources.Clear();
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
                    if (m_RenderGraphResources[type].pool != null)
                    {
                        m_RenderGraphResources[type].pool.LogResources(m_ResourceLogger);
                        m_ResourceLogger.LogLine("");
                    }
                }
            }
        }

        #endregion
    }
}
