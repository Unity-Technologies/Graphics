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
                throw new Exception(RenderGraph.RenderGraphExceptionMessages.k_UndisposedBuilderPreviousPass);
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
            UseTransientResource(result.handle);
            return result;
        }

        public BufferHandle CreateTransientBuffer(in BufferHandle computebuffer)
        {
            ref readonly var desc = ref m_Resources.GetBufferResourceDesc(computebuffer.handle);
            return CreateTransientBuffer(desc);
        }

        public TextureHandle CreateTransientTexture(in TextureDesc desc)
        {
            var result = m_Resources.CreateTexture(desc, m_RenderPass.index);
            UseTransientResource(result.handle);
            return result;
        }

        public TextureHandle CreateTransientTexture(in TextureHandle texture)
        {
            ref readonly var desc = ref m_Resources.GetTextureResourceDesc(texture.handle);
            return CreateTransientTexture(desc);
        }

        public void GenerateDebugData(bool value)
        {
            m_RenderPass.GenerateDebugData(value);
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
        private void CheckWriteTo(in ResourceHandle handle)
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
                    throw new InvalidOperationException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {handle.type} at index {handle.index} - " + RenderGraph.RenderGraphExceptionMessages.k_WriteToVersionedResource);
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
                    throw new InvalidOperationException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {handle.type} at index {handle.index} - " + RenderGraph.RenderGraphExceptionMessages.k_WriteToResourceTwice);
                }
            }
        }

        // Lifetime of a transient resource is one render graph pass
        private ResourceHandle UseTransientResource(in ResourceHandle inputHandle)
        {
            CheckResource(inputHandle);

            ResourceHandle versionedHandle = inputHandle.IsVersioned ? inputHandle : m_Resources.GetLatestVersionHandle(inputHandle);

            // Transient resources are always considered written and read in the render graph pass where they are used
            // Compiler will take it into account later, no need to add them to read and write lists
            m_RenderPass.AddTransientResource(versionedHandle);

            return versionedHandle;
        }

        private ResourceHandle UseResource(in ResourceHandle inputHandle, AccessFlags flags)
        {
            CheckResource(inputHandle);

            bool discard = (flags & AccessFlags.Discard) != 0;
            bool read = (flags & AccessFlags.Read) != 0;
            bool write = (flags & AccessFlags.Write) != 0;

            ResourceHandle versionedHandle = inputHandle.IsVersioned ? inputHandle : m_Resources.GetLatestVersionHandle(inputHandle);

            // If we are not discarding the current version and its data, add a "read" dependency on it
            // this is a bit of a misnomer it really means more like "Preserve existing content or read"
            if (!discard)
            {
                m_Resources.IncrementReadCount(versionedHandle);
                m_RenderPass.AddResourceRead(versionedHandle);

                // Implicit read - not user-specified
                if (!read)
                {
                    m_RenderPass.implicitReadsList.Add(versionedHandle);
                }
            }
            else
            {
                // We are discarding it but we still read it, so we add a dependency on version "0" of this resource
                if (read)
                {
                    var zeroVersionHandle = m_Resources.GetZeroVersionHandle(versionedHandle);
                    m_Resources.IncrementReadCount(zeroVersionHandle);
                    m_RenderPass.AddResourceRead(zeroVersionHandle);
                }
            }

            if (write)
            {
                CheckWriteTo(inputHandle);
                // New versioned written by this render graph pass
                versionedHandle = m_Resources.IncrementWriteCount(inputHandle);
                m_RenderPass.AddResourceWrite(versionedHandle);
            }

            return versionedHandle;
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
        private void CheckNotUseFragment(in TextureHandle tex)
        {
            if (RenderGraph.enableValidityChecks)
            {
                bool usedAsFragment = (m_RenderPass.depthAccess.textureHandle.IsValid() && m_RenderPass.depthAccess.textureHandle.handle.index == tex.handle.index);
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
                    throw new ArgumentException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {tex.handle.type} at index {tex.handle.index} - " + RenderGraph.RenderGraphExceptionMessages.k_TextureAlreadyBeingUsedThroughSetAttachment);
                }
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        private void CheckTextureUVOriginIsValid(in ResourceHandle handle, TextureResource texRes)
        {
            if (texRes.textureUVOrigin == TextureUVOriginSelection.TopLeft)
            {
                var name = m_Resources.GetRenderGraphResourceName(handle);
                throw new ArgumentException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type `{handle.type}` at index `{handle.index}` - " + RenderGraph.RenderGraphExceptionMessages.IncompatibleTextureUVOriginUseTexture(texRes.textureUVOrigin));
            }
        }

        public void UseTexture(in TextureHandle input, AccessFlags flags)
        {
            CheckNotUseFragment(input);
            UseResource(input.handle, flags);

            if ((flags & AccessFlags.Read) == AccessFlags.Read)
            {
                if (m_RenderGraph.renderTextureUVOriginStrategy == RenderTextureUVOriginStrategy.PropagateAttachmentOrientation)
                {
                    TextureResource texRes = m_Resources.GetTextureResource(input.handle);
                    CheckTextureUVOriginIsValid(input.handle, texRes);
                    texRes.textureUVOrigin = TextureUVOriginSelection.BottomLeft;
                }
            }
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
                // rose test this path
                var name = m_Resources.GetRenderGraphResourceName(h.handle);
                throw new ArgumentException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {h.handle.type} at index {h.handle.index} - " + RenderGraph.RenderGraphExceptionMessages.NoGlobalTextureAtPropertyID(propertyId));
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
        private void CheckUseFragment(in TextureHandle tex, bool isDepth)
        {
            if (RenderGraph.enableValidityChecks)
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
                    throw new InvalidOperationException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {tex.handle.type} at index {tex.handle.index} - " + RenderGraph.RenderGraphExceptionMessages.k_SetRenderAttachmentTextureAlreadyUsed);
                }

                m_Resources.GetRenderTargetInfo(tex.handle, out var info);

                // The old path is full of invalid uses that somehow work (or seem to work) so we skip validation if not using actual native renderpass
                if (m_RenderGraph.nativeRenderPassesEnabled)
                {
                    if (isDepth)
                    {
                        if (!GraphicsFormatUtility.IsDepthFormat(info.format))
                        {
                            var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                            throw new InvalidOperationException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {tex.handle.type} at index {tex.handle.index} - " + RenderGraph.RenderGraphExceptionMessages.UseDepthWithColorFormat(info.format));
                        }
                    }
                    else
                    {
                        if (GraphicsFormatUtility.IsDepthFormat(info.format))
                        {
                            var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                            throw new InvalidOperationException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {tex.handle.type} at index {tex.handle.index} - " + RenderGraph.RenderGraphExceptionMessages.k_SetRenderAttachmentOnDepthTexture);
                        }
                    }

                    if (m_RenderGraph.renderTextureUVOriginStrategy == RenderTextureUVOriginStrategy.PropagateAttachmentOrientation)
                    {
                        var texResource = m_Resources.GetTextureResource(tex.handle);
                        TextureResource checkTexResource = null;
                        for (int i = 0; i < m_RenderPass.fragmentInputMaxIndex + 1; ++i)
                        {
                            if (m_RenderPass.fragmentInputAccess[i].textureHandle.IsValid())
                            {
                                ref readonly TextureHandle texCheck = ref m_RenderPass.fragmentInputAccess[i].textureHandle;
                                checkTexResource = m_Resources.GetTextureResource(texCheck.handle);
                                if (texResource.textureUVOrigin != TextureUVOriginSelection.Unknown && checkTexResource.textureUVOrigin != TextureUVOriginSelection.Unknown && texResource.textureUVOrigin != checkTexResource.textureUVOrigin)
                                {
                                    var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                                    var checkName = m_Resources.GetRenderGraphResourceName(texCheck.handle);
                                    throw new InvalidOperationException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {tex.handle.type} at index {tex.handle.index} - " + RenderGraph.RenderGraphExceptionMessages.IncompatibleTextureUVOrigin(texResource.textureUVOrigin, "input", checkName, texCheck.handle.type, texCheck.handle.index, checkTexResource.textureUVOrigin));
                                }
                            }
                        }

                        for (int i = 0; i < m_RenderPass.colorBufferMaxIndex + 1; ++i)
                        {
                            if (m_RenderPass.colorBufferAccess[i].textureHandle.IsValid())
                            {
                                ref readonly TextureHandle texCheck = ref m_RenderPass.colorBufferAccess[i].textureHandle;
                                checkTexResource = m_Resources.GetTextureResource(texCheck.handle);
                                if (texResource.textureUVOrigin != TextureUVOriginSelection.Unknown && checkTexResource.textureUVOrigin != TextureUVOriginSelection.Unknown && texResource.textureUVOrigin != checkTexResource.textureUVOrigin)
                                {
                                    var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                                    var checkName = m_Resources.GetRenderGraphResourceName(texCheck.handle);
                                    throw new InvalidOperationException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {tex.handle.type} at index {tex.handle.index} - " + RenderGraph.RenderGraphExceptionMessages.IncompatibleTextureUVOrigin(texResource.textureUVOrigin, "render", checkName, texCheck.handle.type, texCheck.handle.index, checkTexResource.textureUVOrigin));
                                }
                            }
                        }

                        if (!isDepth && m_RenderPass.depthAccess.textureHandle.IsValid())
                        {
                            TextureHandle texCheck = m_RenderPass.depthAccess.textureHandle;
                            checkTexResource = m_Resources.GetTextureResource(texCheck.handle);
                            if (texResource.textureUVOrigin != TextureUVOriginSelection.Unknown && checkTexResource.textureUVOrigin != TextureUVOriginSelection.Unknown && texResource.textureUVOrigin != checkTexResource.textureUVOrigin)
                            {
                                var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                                var checkName = m_Resources.GetRenderGraphResourceName(texCheck.handle);
                                throw new InvalidOperationException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {tex.handle.type} at index {tex.handle.index} - " + RenderGraph.RenderGraphExceptionMessages.IncompatibleTextureUVOrigin(texResource.textureUVOrigin, "depth", checkName, texCheck.handle.type, texCheck.handle.index, checkTexResource.textureUVOrigin));
                            }
                        }
                    }
                }

                foreach (var globalTex in m_RenderPass.setGlobalsList)
                {
                    if (globalTex.Item1.handle.index == tex.handle.index)
                    {
                        var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                        throw new InvalidOperationException($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {tex.handle.type} at index {tex.handle.index} - "  + RenderGraph.RenderGraphExceptionMessages.k_SetRenderAttachmentOnGlobalTexture);
                    }
                }
            }
        }

        public void SetRenderAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice)
        {
            CheckUseFragment(tex, false);
            var versionedTextureHandle = new TextureHandle(UseResource(tex.handle, flags));
            m_RenderPass.SetColorBufferRaw(versionedTextureHandle, index, flags, mipLevel, depthSlice);
        }

        public void SetInputAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice)
        {
            CheckFrameBufferFetchEmulationIsSupported(tex);

            CheckUseFragment(tex, false);
            var versionedTextureHandle = new TextureHandle(UseResource(tex.handle, flags));
            m_RenderPass.SetFragmentInputRaw(versionedTextureHandle, index, flags, mipLevel, depthSlice);
        }

        public void SetRenderAttachmentDepth(TextureHandle tex, AccessFlags flags, int mipLevel, int depthSlice)
        {
            CheckUseFragment(tex, true);
            var versionedTextureHandle = new TextureHandle(UseResource(tex.handle, flags));
            m_RenderPass.SetDepthBufferRaw(versionedTextureHandle, flags, mipLevel, depthSlice);
        }

        public TextureHandle SetRandomAccessAttachment(TextureHandle input, int index, AccessFlags flags = AccessFlags.Read)
        {
            CheckNotUseFragment(input);
            ResourceHandle result = UseResource(input.handle, flags);
            m_RenderPass.SetRandomWriteResourceRaw(result, index, false, flags);
            return input;
        }

        public void SetShadingRateImageAttachment(in TextureHandle tex)
        {
            CheckNotUseFragment(tex);
            var versionedTextureHandle = new TextureHandle(UseResource(tex.handle, AccessFlags.Read));
            m_RenderPass.SetShadingRateImageRaw(versionedTextureHandle);
        }

        public BufferHandle UseBufferRandomAccess(BufferHandle input, int index, AccessFlags flags = AccessFlags.Read)
        {
            var h = UseBuffer(input, flags);
            m_RenderPass.SetRandomWriteResourceRaw(h.handle, index, true, flags);
            return input;
        }

        public BufferHandle UseBufferRandomAccess(BufferHandle input, int index, bool preserveCounterValue, AccessFlags flags = AccessFlags.Read)
        {
            var h = UseBuffer(input, flags);
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
        
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckResource(in ResourceHandle res, bool checkTransientReadWrite = false)
        {
            if (RenderGraph.enableValidityChecks)
            {
                if (res.IsValid())
                {
                    int transientIndex = m_Resources.GetRenderGraphResourceTransientIndex(res);
                    // We have dontCheckTransientReadWrite here because users may want to use UseColorBuffer/UseDepthBuffer API to benefit from render target auto binding. In this case we don't want to raise the error.
                    if (transientIndex == m_RenderPass.index && checkTransientReadWrite)
                    {
                        var name = m_Resources.GetRenderGraphResourceName(res);
                        Debug.LogError($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {res.type} at index {res.index} - "  + RenderGraph.RenderGraphExceptionMessages.k_ReadWriteTransient);
                    }

                    if (transientIndex != -1 && transientIndex != m_RenderPass.index)
                    {
                        var name = m_Resources.GetRenderGraphResourceName(res);
                        throw new ArgumentException(
                            $"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {res.type} at index {res.index} - " +
                            RenderGraph.RenderGraphExceptionMessages.UseTransientTextureInWrongPass(transientIndex));
                    }
                }
                else
                {
                    var name = m_Resources.GetRenderGraphResourceName(res);
                    throw new Exception($"In pass '{m_RenderPass.name}' when trying to use resource '{name}' of type {res.type} at index {res.index} - "  + RenderGraph.RenderGraphExceptionMessages.k_InvalidResource);
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

        public void SetShadingRateFragmentSize(ShadingRateFragmentSize shadingRateFragmentSize)
        {
            m_RenderPass.SetShadingRateFragmentSize(shadingRateFragmentSize);
        }

        public void SetShadingRateCombiner(ShadingRateCombinerStage stage, ShadingRateCombiner combiner)
        {
            m_RenderPass.SetShadingRateCombiner(stage, combiner);
        }

        public void SetExtendedFeatureFlags(ExtendedFeatureFlags extendedFeatureFlags)
        {
            m_RenderPass.SetExtendedFeatureFlags(extendedFeatureFlags);
        }
    }
}
