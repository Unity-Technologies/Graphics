using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    // This is a class making it a struct wouldn't help as we pas it around as an interface which means it would be boxed/unboxed anyway
    // Publicly this class has different faces to help the users with different pass types through type safety but internally
    // we just have a single implementation for all builders
    internal class RenderGraphBuilders : IBaseRenderGraphBuilder, IComputeRenderGraphBuilder, IRasterRenderGraphBuilder, ILowLevelRenderGraphBuilder
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
            // If the object is not disposed yet this is an error as the pass is not finished (only in the dispose we register it with the rendergraph)
            // This is likely cause by a user not doing a clean using and then forgetting to manually dispose the object.
            if (m_Disposed != true)
            {
                throw new Exception("Please finish building the previous pass first by disposing the pass builder object before adding a new pass.");
            }

            m_RenderPass = renderPass;
            m_Resources = resources;
            m_RenderGraph = renderGraph;
            m_Disposed = false;
        }


        public void EnableAsyncCompute(bool value)
        {
            m_RenderPass.EnableAsyncCompute(value);
        }

        public void AllowPassCulling(bool value)
        {
            m_RenderPass.AllowPassCulling(value);
        }

        public void AllowGlobalStateModification(bool value)
        {
            m_RenderPass.AllowGlobalState(value);
        }

        public BufferHandle CreateTransientBuffer(in BufferDesc desc)
        {
            var result = m_Resources.CreateBuffer(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(result.handle);
            return result;
        }

        public BufferHandle CreateTransientBuffer(in BufferHandle computebuffer)
        {
            var desc = m_Resources.GetBufferResourceDesc(computebuffer.handle);
            var result = m_Resources.CreateBuffer(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(result.handle);
            return result;
        }

        public TextureHandle CreateTransientTexture(in TextureDesc desc)
        {
            var result = m_Resources.CreateTexture(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(result.handle);
            return result;
        }

        public TextureHandle CreateTransientTexture(in TextureHandle texture)
        {
            var desc = m_Resources.GetTextureResourceDesc(texture.handle);
            var result = m_Resources.CreateTexture(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(result.handle);
            return result;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
            {
                m_RenderGraph.OnPassAdded(m_RenderPass);
            }

            m_Disposed = true;
        }

        private ResourceHandle UseResource(in ResourceHandle handle, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            CheckResource(handle);

            // If we are not discarding the resource, add a "read" dependency on the current version
            // this "Read" is a bit of a misnomer it really means more like "Preserve existing content or read"
            if ((flags & IBaseRenderGraphBuilder.AccessFlags.Discard) == 0)
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
                m_RenderPass.AddResourceRead(versioned);
            }
            else
            {
                // We are discarding it but we still read it, so we add a dependency on version "0" of this resource
                if ((flags & IBaseRenderGraphBuilder.AccessFlags.Read) != 0)
                {
                    m_RenderPass.AddResourceRead(m_Resources.GetZeroVersionedHandle(handle));
                }
            }

            if ((flags & IBaseRenderGraphBuilder.AccessFlags.Write) != 0)
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
                m_RenderPass.AddResourceWrite(m_Resources.GetNewVersionedHandle(handle));
                m_Resources.IncrementWriteCount(handle);
            }

            return GetLatestVersionHandle(handle);
        }

        public BufferHandle UseBuffer(in BufferHandle input, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            if ((flags & IBaseRenderGraphBuilder.AccessFlags.GrabRead) != 0)
            {
                throw new ArgumentException("GrabRead is only valid on UseTexture");
            }
            return new BufferHandle(UseResource(input.handle, flags));
        }

        // UseTexture and UseTextureFragment are currently forced to be mutually exclusive in the same pass
        // check this.
        // We currently ignore the version. In theory there might be some cases that are actually allowed with versioning
        // for ample UseTexture(myTexV1, read) UseFragment(myTexV2, ReadWrite) as they are different versions
        // but for now we don't allow any of that.
        private void CheckNotUseFragment(TextureHandle tex, IBaseRenderGraphBuilder.AccessFlags flags)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (flags == IBaseRenderGraphBuilder.AccessFlags.GrabRead)
            {
                return;
            }


            bool usedAsFragment = false;
            usedAsFragment = (m_RenderPass.depthBuffer.handle.index == tex.handle.index);
            if (!usedAsFragment)
            {
                for (int i = 0; i <= m_RenderPass.colorBufferMaxIndex; i++)
                {
                    if (m_RenderPass.depthBuffer.handle.index == tex.handle.index)
                    {
                        usedAsFragment = true;
                        break;
                    }
                }
            }

            if (usedAsFragment)
            {
                var name = m_Resources.GetRenderGraphResourceName(tex.handle);
                throw new ArgumentException($"Trying to UseTexture on a texture that is already used through UseTextureFragment. Consider using a Grab access mode or update your code. (pass {m_RenderPass.name} resource{name}).");
            }
#endif
        }

        public TextureHandle UseTexture(in TextureHandle input, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            CheckUseFragment(input, flags);
            TextureHandle h = new TextureHandle();
            h.handle = UseResource(input.handle, flags);
            return h;
        }

        // Shared validation between UseTextureFragment/UseTextureFragmentDepth
        private void CheckUseFragment(TextureHandle tex, IBaseRenderGraphBuilder.AccessFlags flags)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if ((flags & IBaseRenderGraphBuilder.AccessFlags.GrabRead) == IBaseRenderGraphBuilder.AccessFlags.GrabRead)
            {
                throw new ArgumentException("GrabRead is only valid on UseTexture");
            }

            // We ignore the version as we don't allow mixing UseTexture/UseFragment between different versions
            // even though it should theoretically work (and we might do so in the future) for now we're overly strict.,
            bool alreadyUsed = false;

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
                throw new InvalidOperationException($"Trying to UseTextureFragment on a texture that is already used through UseTexture/UseTextureFragment. Consider using a Grab access mode or update your code. (pass {m_RenderPass.name} resource{name}).");
            }
#endif
        }

        public TextureHandle UseTextureFragment(TextureHandle tex, int index, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            CheckUseFragment(tex, flags);
            var result = UseResource(tex.handle, flags);
            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track of the handle->mrt index mapping
            var th = new TextureHandle(result);
            m_RenderPass.SetColorBufferRaw(th, index);
            return th;
        }

        public TextureHandle UseTextureFragmentDepth(TextureHandle tex, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            CheckUseFragment(tex, flags);
            var result = UseResource(tex.handle, flags);
            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track to bind this handle as a depth texture.
            m_RenderPass.SetDepthBufferRaw(new TextureHandle(GetLatestVersionHandle(tex.handle)));
            return new TextureHandle(result);
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, ComputeGraphContext> renderFunc) where PassData : class, new()
        {
            ((ComputeRenderGraphPass<PassData>)m_RenderPass).renderFunc = renderFunc;
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, RasterGraphContext> renderFunc) where PassData : class, new()
        {
            ((RasterRenderGraphPass<PassData>)m_RenderPass).renderFunc = renderFunc;
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, LowLevelGraphContext> renderFunc) where PassData : class, new()
        {
            ((LowLevelRenderGraphPass<PassData>)m_RenderPass).renderFunc = renderFunc;
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

        void CheckResource(in ResourceHandle res, bool dontCheckTransientReadWrite = false)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (res.IsValid())
            {
                int transientIndex = m_Resources.GetRenderGraphResourceTransientIndex(res);
                // We have dontCheckTransientReadWrite here because users may want to use UseColorBuffer/UseDepthBuffer API to benefit from render target auto binding. In this case we don't want to raise the error.
                if (transientIndex == m_RenderPass.index && !dontCheckTransientReadWrite)
                {
                    Debug.LogError($"Trying to read or write a transient resource at pass {m_RenderPass.name}.Transient resource are always assumed to be both read and written.");
                }

                if (transientIndex != -1 && transientIndex != m_RenderPass.index)
                {
                    throw new ArgumentException($"Trying to use a transient texture (pass index {transientIndex}) in a different pass (pass index {m_RenderPass.index}).");
                }
            }
            else
            {
                throw new ArgumentException($"Trying to use an invalid resource (pass {m_RenderPass.name}).");
            }
#endif
        }
    }
}
