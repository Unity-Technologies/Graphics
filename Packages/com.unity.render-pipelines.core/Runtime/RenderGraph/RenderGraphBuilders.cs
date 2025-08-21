using System;
using System.Diagnostics;
using UnityEngine.Experimental.Rendering;
using static UnityEngine.Rendering.RenderGraphModule.RenderGraph;

namespace UnityEngine.Rendering.RenderGraphModule
{
    // This is a class making it a struct wouldn't help as we pas it around as an interface which means it would be boxed/unboxed anyway
    // Publicly this class has different faces to help the users with different pass types through type safety but internally
    // we just have a single implementation for all builders
    internal class RenderGraphBuilders : IBaseRenderGraphBuilder, IComputeRenderGraphBuilder, IRasterRenderGraphBuilder, IUnsafeRenderGraphBuilder
    {
        RenderGraphPass m_RenderPass;
        RenderGraphResourceRegistry m_Resources;
        RenderGraph m_RenderGraph;
        bool m_Disposed;


        public RenderGraphBuilders()
        {
            m_RenderPass = null;
            m_Resources = null;
            m_RenderGraph = null;
            m_Disposed = true;
        }

        public void Setup(RenderGraphPass renderPass, RenderGraphResourceRegistry resources, RenderGraph renderGraph)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // If the object is not disposed yet this is an error as the pass is not finished (only in the dispose we register it with the rendergraph)
            // This is likely cause by a user not doing a clean using and then forgetting to manually dispose the object.
            if (m_Disposed != true)
            {
                throw new Exception("Please finish building the previous pass first by disposing the pass builder object before adding a new pass.");
            }
#endif
            m_RenderPass = renderPass;
            m_Resources = resources;
            m_RenderGraph = renderGraph;
            m_Disposed = false;

            renderPass.useAllGlobalTextures = false;

            if (renderPass.type == RenderGraphPassType.Raster)
            {
                CommandBuffer.ThrowOnSetRenderTarget = true;
            }
        }


        public void EnableAsyncCompute(bool value)
        {
            m_RenderPass.EnableAsyncCompute(value);
        }

        public void AllowPassCulling(bool value)
        {
            // This pass cannot be culled if it allows global state modifications
            if (value && m_RenderPass.allowGlobalState)
                return;

            m_RenderPass.AllowPassCulling(value);
        }

        public void AllowGlobalStateModification(bool value)
        {
            m_RenderPass.AllowGlobalState(value);

            // This pass cannot be culled if it allows global state modifications
            if (value)
            {
                AllowPassCulling(false);
            }
        }

        /// <summary>
        /// Enable foveated rendering for this pass.
        /// </summary>
        /// <param name="value">True to enable foveated rendering.</param>
        public void EnableFoveatedRasterization(bool value)
        {
            m_RenderPass.EnableFoveatedRasterization(value);
        }

        public BufferHandle CreateTransientBuffer(in BufferDesc desc)
        {
            var result = m_Resources.CreateBuffer(desc, m_RenderPass.index);
            UseResource(result.handle, AccessFlags.Write | AccessFlags.Read, isTransient: true);
            return result;
        }

        public BufferHandle CreateTransientBuffer(in BufferHandle computebuffer)
        {
            var desc = m_Resources.GetBufferResourceDesc(computebuffer.handle);
            return CreateTransientBuffer(desc);
        }

        public TextureHandle CreateTransientTexture(in TextureDesc desc)
        {
            var result = m_Resources.CreateTexture(desc, m_RenderPass.index);
            UseResource(result.handle, AccessFlags.Write | AccessFlags.Read, isTransient: true);
            return result;
        }

        public TextureHandle CreateTransientTexture(in TextureHandle texture)
        {
            var desc = m_Resources.GetTextureResourceDesc(texture.handle);
            return CreateTransientTexture(desc);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            try
            {
                if (disposing)
                {
                    m_RenderGraph.RenderGraphState = RenderGraphState.RecordingGraph;

                    // Use all globals simply means this... we do a UseTexture on all globals so the pass has the correct dependencies.
                    // This of course goes to show how bad an idea shader-system wide globals really are dependency/lifetime tracking wise :-)
                    if (m_RenderPass.useAllGlobalTextures)
                    {
                        foreach (var texture in m_RenderGraph.AllGlobals())
                        {
                            if (texture.IsValid())
                                this.UseTexture(texture, AccessFlags.Read);
                        }
                    }

                    // Set globals on the graph fronted side so subsequent passes can have pass dependencies on these global texture handles
                    foreach (var t in m_RenderPass.setGlobalsList)
                    {
                        m_RenderGraph.SetGlobal(t.Item1, t.Item2);
                    }

                    m_RenderGraph.OnPassAdded(m_RenderPass);
                }
            }
            finally
            {
                if (m_RenderPass.type == RenderGraphPassType.Raster)
                {
                    CommandBuffer.ThrowOnSetRenderTarget = false;
                }

                m_RenderPass = null;
                m_Resources = null;
                m_RenderGraph = null;
                m_Disposed = true;
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void ValidateWriteTo(in ResourceHandle handle)
        {
            if (RenderGraph.enableValidityChecks)
            {
                // Write by design generates a new version of the resource. However
                // you could in theory write to v2 of the resource while there is already
                // a v3 so this write would then introduce a new v4 of the resource.
                // This would mean a divergence in the versioning history subsequent versions based on v2 and other subsequent versions based on v3
                // this would be very confusing as they are all still refered to by the same texture just different versions
                // so we decide to disallow this. It can always be (at zero cost) handled by using a "Move" pass to move the divergent version
                // so it's own texture resource which again has it's own single history of versions.
                if (handle.IsVersioned)
                {
                    var name = m_Resources.GetRenderGraphResourceName(handle);
                    throw new InvalidOperationException($"Trying to write to a versioned resource handle. You can only write to unversioned resource handles to avoid branches in the resource history. (pass {m_RenderPass.name} resource{name}).");
                }

                if (m_RenderPass.IsWritten(handle))
                {
                    // We bump the version and write count if you call USeResource with writing flags. So calling this several times
                    // would lead to the side effect of new versions for every call to UseResource leading to incorrect versions.
                    // In theory we could detect and ignore the second UseResource but we decided to just disallow is as it might also
                    // Stem from user confusion.
                    // It seems the most likely cause of such a situation would be something like:
                    // TextureHandle b = a;
                    // ...
                    // much much code in between, user lost track that a=b
                    // ...
                    // builder.WriteTexture(a)
                    // builder.WriteTexture(b)
                    // > Get this error they were probably thinking they were writing two separate outputs... but they are just two versions of resource 'a'
                    // where they can only differ between by careful management of versioned resources.
                    var name = m_Resources.GetRenderGraphResourceName(handle);
                    throw new InvalidOperationException($"Trying to write a resource twice in a pass. You can only write the same resource once within a pass (pass {m_RenderPass.name} resource{name}).");
                }
            }
        }

        private ResourceHandle UseResource(in ResourceHandle handle, AccessFlags flags, bool isTransient = false)
        {
            CheckResource(handle);

            // If we are not discarding the resource, add a "read" dependency on the current version
            // this "Read" is a bit of a misnomer it really means more like "Preserve existing content or read"
            if ((flags & AccessFlags.Discard) == 0)
            {
                ResourceHandle versioned;
                if (!handle.IsVersioned)
                {
                    versioned = m_Resources.GetLatestVersionHandle(handle);
                }
                else
                {
                    versioned = handle;
                }

                if (isTransient)
                {
                    m_RenderPass.AddTransientResource(versioned);
                    return GetLatestVersionHandle(handle);
                }

                m_RenderPass.AddResourceRead(versioned);
                m_Resources.IncrementReadCount(handle);

                if ((flags & AccessFlags.Read) == 0)
                {
                    // Flag the resource as being an "implicit read" so that we can distinguish it from a user-specified read
                    m_RenderPass.implicitReadsList.Add(versioned);
                }
            }
            else
            {
                // We are discarding it but we still read it, so we add a dependency on version "0" of this resource
                if ((flags & AccessFlags.Read) != 0)
                {
                    m_RenderPass.AddResourceRead(m_Resources.GetZeroVersionedHandle(handle));
                    m_Resources.IncrementReadCount(handle);
                }
            }

            if ((flags & AccessFlags.Write) != 0)
            {
                ValidateWriteTo(handle);
                m_RenderPass.AddResourceWrite(m_Resources.GetNewVersionedHandle(handle));
                m_Resources.IncrementWriteCount(handle);
            }

            return GetLatestVersionHandle(handle);
        }

        public BufferHandle UseBuffer(in BufferHandle input, AccessFlags flags)
        {
            UseResource(input.handle, flags);
            return input;
        }

        // UseTexture and SetRenderAttachment are currently forced to be mutually exclusive in the same pass
        // check this.
        // We currently ignore the version. In theory there might be some cases that are actually allowed with versioning
        // for ample UseTexture(myTexV1, read) UseFragment(myTexV2, ReadWrite) as they are different versions
        // but for now we don't allow any of that.
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void CheckNotUseFragment(TextureHandle tex)
        {
            if(RenderGraph.enableValidityChecks)
            {
                bool usedAsFragment = false;
                usedAsFragment = (m_RenderPass.depthAccess.textureHandle.IsValid() && m_RenderPass.depthAccess.textureHandle.handle.index == tex.handle.index);
                if (!usedAsFragment)
                {
                    for (int i = 0; i <= m_RenderPass.colorBufferMaxIndex; i++)
                    {
                        if (m_RenderPass.colorBufferAccess[i].textureHandle.IsValid() && m_RenderPass.colorBufferAccess[i].textureHandle.handle.index == tex.handle.index)
                        {
                            usedAsFragment = true;
                            break;
                        }
                    }
                }

                if (usedAsFragment)
                {
                    var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                    throw new ArgumentException($"Trying to UseTexture on a texture that is already used through SetRenderAttachment. Consider updating your code. (pass {m_RenderPass.name} resource{name}).");
                }
            }
        }

        public void UseTexture(in TextureHandle input, AccessFlags flags)
        {
            CheckNotUseFragment(input);
            UseResource(input.handle, flags);
        }

        public void UseGlobalTexture(int propertyId, AccessFlags flags)
        {
            var h = m_RenderGraph.GetGlobal(propertyId);
            if (h.IsValid())
            {
                UseTexture(h, flags);
            }
            else
            {
                throw new ArgumentException($"Trying to read global texture property {propertyId} but no previous pass in the graph assigned a value to this global.");
            }
        }

        public void UseAllGlobalTextures(bool enable)
        {
            m_RenderPass.useAllGlobalTextures = enable;
        }

        public void SetGlobalTextureAfterPass(in TextureHandle input, int propertyId)
        {
            m_RenderPass.setGlobalsList.Add(ValueTuple.Create(input, propertyId));
        }

        // Shared validation between SetRenderAttachment/SetRenderAttachmentDepth
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void CheckUseFragment(TextureHandle tex, bool isDepth)
        {
            if(RenderGraph.enableValidityChecks)
            {
                // We ignore the version as we don't allow mixing UseTexture/UseFragment between different versions
                // even though it should theoretically work (and we might do so in the future) for now we're overly strict.
                bool alreadyUsed = false;

                //TODO: Check grab textures here and allow if it's grabbed. For now
                // SetRenderAttachment()
                // UseTexture(grab)
                // will work but not the other way around
                for (int i = 0; i < m_RenderPass.resourceReadLists[tex.handle.iType].Count; i++)
                {
                    if (m_RenderPass.resourceReadLists[tex.handle.iType][i].index == tex.handle.index)
                    {
                        alreadyUsed = true;
                        break;
                    }
                }

                for (int i = 0; i < m_RenderPass.resourceWriteLists[tex.handle.iType].Count; i++)
                {
                    if (m_RenderPass.resourceWriteLists[tex.handle.iType][i].index == tex.handle.index)
                    {
                        alreadyUsed = true;
                        break;
                    }
                }

                if (alreadyUsed)
                {
                    var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                    throw new InvalidOperationException($"Trying to SetRenderAttachment on a texture that is already used through UseTexture/SetRenderAttachment. Consider updating your code. (pass '{m_RenderPass.name}' resource '{name}').");
                }

                m_Resources.GetRenderTargetInfo(tex.handle, out var info);

                // The old path is full of invalid uses that somehow work (or seemt to work) so we skip the tests if not using actual native renderpass
                if (m_RenderGraph.nativeRenderPassesEnabled)
                {
                    if (isDepth)
                    {
                        if (!GraphicsFormatUtility.IsDepthFormat(info.format))
                        {
                            var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                            throw new InvalidOperationException($"Trying to SetRenderAttachmentDepth on a texture that has a color format {info.format}. Use a texture with a depth format instead. (pass '{m_RenderPass.name}' resource '{name}').");
                        }
                    }
                    else
                    {
                        if (GraphicsFormatUtility.IsDepthFormat(info.format))
                        {
                            var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                            throw new InvalidOperationException($"Trying to SetRenderAttachment on a texture that has a depth format. Use a texture with a color format instead. (pass '{m_RenderPass.name}' resource '{name}').");
                        }
                    }
                }

                foreach (var globalTex in m_RenderPass.setGlobalsList)
                {
                    if (globalTex.Item1.handle.index == tex.handle.index)
                    {
                        throw new InvalidOperationException("Trying to SetRenderAttachment on a texture that is currently set on a global texture slot. Shaders might be using the texture using samplers. You should ensure textures are not set as globals when using them as fragment attachments.");
                    }
                }
            }
        }

        public void SetRenderAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice)
        {
            CheckUseFragment(tex, false);
            ResourceHandle result = UseResource(tex.handle, flags);
            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track of the handle->mrt index mapping
            var th = new TextureHandle();
            th.handle = result;
            m_RenderPass.SetColorBufferRaw(th, index, flags, mipLevel, depthSlice);
        }

        public void SetInputAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice)
        {
            CheckFrameBufferFetchEmulationIsSupported(tex);

            CheckUseFragment(tex, false);
            ResourceHandle result = UseResource(tex.handle, flags);
            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track of the handle->mrt index mapping
            var th = new TextureHandle();
            th.handle = result;
            m_RenderPass.SetFragmentInputRaw(th, index, flags, mipLevel, depthSlice);
        }

        public void SetRenderAttachmentDepth(TextureHandle tex, AccessFlags flags, int mipLevel, int depthSlice)
        {
            CheckUseFragment(tex, true);
            ResourceHandle result = UseResource(tex.handle, flags);
            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track to bind this handle as a depth texture.
            var th = new TextureHandle();
            th.handle = result;
            m_RenderPass.SetDepthBufferRaw(th, flags, mipLevel, depthSlice);
        }

        public TextureHandle SetRandomAccessAttachment(TextureHandle input, int index, AccessFlags flags = AccessFlags.Read)
        {
            CheckNotUseFragment(input);
            ResourceHandle result = UseResource(input.handle, flags);

            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track of the resources to bind before execution
            var th = new TextureHandle();
            th.handle = result;
            m_RenderPass.SetRandomWriteResourceRaw(th.handle, index, false, flags);
            return input;
        }

        public BufferHandle UseBufferRandomAccess(BufferHandle input, int index, AccessFlags flags = AccessFlags.Read)
        {
            var h = UseBuffer(input, flags);

            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track of the resources to bind before execution
            m_RenderPass.SetRandomWriteResourceRaw(h.handle, index, true, flags);
            return input;
        }

        public BufferHandle UseBufferRandomAccess(BufferHandle input, int index, bool preserveCounterValue, AccessFlags flags = AccessFlags.Read)
        {
            var h = UseBuffer(input, flags);

            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track of the resources to bind before execution
            m_RenderPass.SetRandomWriteResourceRaw(h.handle, index, preserveCounterValue, flags);
            return input;
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, ComputeGraphContext> renderFunc) where PassData : class, new()
        {
            ((ComputeRenderGraphPass<PassData>)m_RenderPass).renderFunc = renderFunc;
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, RasterGraphContext> renderFunc) where PassData : class, new()
        {
            ((RasterRenderGraphPass<PassData>)m_RenderPass).renderFunc = renderFunc;
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, UnsafeGraphContext> renderFunc) where PassData : class, new()
        {
            ((UnsafeRenderGraphPass<PassData>)m_RenderPass).renderFunc = renderFunc;
        }

        public void UseRendererList(in RendererListHandle input)
        {
            m_RenderPass.UseRendererList(input);
        }

        private ResourceHandle GetLatestVersionHandle(in ResourceHandle handle)
        {
            // Transient resources can't be used outside the pass so can never be versioned
            if (m_Resources.GetRenderGraphResourceTransientIndex(handle) >= 0)
            {
                return handle;
            }

            return m_Resources.GetLatestVersionHandle(handle);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckResource(in ResourceHandle res, bool checkTransientReadWrite = false)
        {
            if(RenderGraph.enableValidityChecks)
            {
                if (res.IsValid())
                {
                    int transientIndex = m_Resources.GetRenderGraphResourceTransientIndex(res);
                    // We have dontCheckTransientReadWrite here because users may want to use UseColorBuffer/UseDepthBuffer API to benefit from render target auto binding. In this case we don't want to raise the error.
                    if (transientIndex == m_RenderPass.index && checkTransientReadWrite)
                    {
                        Debug.LogError($"Trying to read or write a transient resource at pass {m_RenderPass.name}.Transient resource are always assumed to be both read and written.");
                    }

                    if (transientIndex != -1 && transientIndex != m_RenderPass.index)
                    {
                        throw new ArgumentException($"Trying to use a transient {res.type} (pass index {transientIndex}) in a different pass (pass index {m_RenderPass.index}).");
                    }
                }
                else
                {
                    throw new Exception($"Trying to use an invalid resource (pass {m_RenderPass.name}).");
                }
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckFrameBufferFetchEmulationIsSupported(in TextureHandle tex)
        {
            if (enableValidityChecks)
            {
                if (!Util.RenderGraphUtils.IsFramebufferFetchEmulationSupportedOnCurrentPlatform())
                {
                    throw new InvalidOperationException($"This API is not supported on the current platform: {SystemInfo.graphicsDeviceType}");
                }

                if (!Util.RenderGraphUtils.IsFramebufferFetchEmulationMSAASupportedOnCurrentPlatform())
                {
                    var sourceInfo = m_RenderGraph.GetRenderTargetInfo(tex);
                    if (sourceInfo.bindMS)
                        throw new InvalidOperationException($"This API is not supported with MSAA attachments on the current platform: {SystemInfo.graphicsDeviceType}");
                }
            }
        }

        public void SetShadingRateImageAttachment(in TextureHandle sriTextureHandle)
        {
            CheckNotUseFragment(sriTextureHandle);

            // shading rate image access flag is always read, only 1 mip and 1 slice
            var newSriTextureHandle = new TextureHandle();
            newSriTextureHandle.handle = UseResource(sriTextureHandle.handle, AccessFlags.Read);

            m_RenderPass.SetShadingRateImage(newSriTextureHandle, AccessFlags.Read, 0, 0);
        }

        public void SetShadingRateFragmentSize(ShadingRateFragmentSize shadingRateFragmentSize)
        {
            m_RenderPass.SetShadingRateFragmentSize(shadingRateFragmentSize);
        }

        public void SetShadingRateCombiner(ShadingRateCombinerStage stage, ShadingRateCombiner combiner)
        {
            m_RenderPass.SetShadingRateCombiner(stage, combiner);
        }
    }
}
