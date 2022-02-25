using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    // This is a class making it a struct wouldn't help as we pas it around as an interface which means it would be boxed/unboxed anyway
    // Publicly this class has different faces to help the users with different pass types through type safety but internally
    // we just have a single implementation for all builders
    internal class RenderGraphBuilders : IBaseRenderGraphBuilder, IComputeRenderGraphBuilder, IRasterRenderGraphBuilder
    {
        RenderGraphPass m_RenderPass;
        RenderGraphResourceRegistry m_Resources;
        RenderGraph m_RenderGraph;
        bool m_Disposed;

        public RenderGraphBuilders(RenderGraphPass renderPass, RenderGraphResourceRegistry resources, RenderGraph renderGraph)
        {
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

        public ComputeBufferHandle CreateTransientComputeBuffer(in ComputeBufferDesc desc)
        {
            var result = m_Resources.CreateComputeBuffer(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(result.handle);
            return result;
        }

        public ComputeBufferHandle CreateTransientComputeBuffer(in ComputeBufferHandle computebuffer)
        {
            var desc = m_Resources.GetComputeBufferResourceDesc(computebuffer.handle);
            var result = m_Resources.CreateComputeBuffer(desc, m_RenderPass.index);
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

        private void UseResource(in ResourceHandle handle, IBaseRenderGraphBuilder.AccessFlags flags)
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

            // Write obviously always writes to a new resource version
            // Note you could in theory write to v2 of the resource while there is already
            // a v3 so this write would then introduce a new v4 of the resource.
            // Which might be confusing to users using implicit versioning. Should we disallow
            // "writing" when using an explicilty versioned handle (even if technically this just works fine?)
            if ((flags & IBaseRenderGraphBuilder.AccessFlags.Write) != 0)
            {
                if (m_RenderPass.IsWriten(handle))
                {
                    // We bump the version and write count if you use a resource with writing flags. Calling this several times
                    // would lead to two nev versions of the resource being created. Thight might make sense but seems overall bad
                    // practice so we disallow it for now.
                    // It seems the most likely cause of such a situation would be something like:
                    // TextureHandle b = a;
                    // ...
                    // much much code in between, user lost track that a=b
                    // ...
                    // builder.WriteTexture(a)
                    // builder.WriteTexture(b)
                    // > Get this error they were probably thinking they were writing two outputs... but they are just two versions of resource 'a'
                    // where they can only differ between by carefull management of versioned resources.
                    var name = m_Resources.GetRenderGraphResourceName(handle);
                    throw new ArgumentException($"Trying to write a resource twice in a pass. You can only write the same resource once within a pass (pass {m_RenderPass.name} resource{name}).");
                }
                m_RenderPass.AddResourceWrite(m_Resources.GetNewVersionedHandle(handle));
            }
        }

        public void UseComputeBuffer(in ComputeBufferHandle input, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            if ((flags & IBaseRenderGraphBuilder.AccessFlags.GrabRead) != 0)
            {
                throw new Exception("GrabRead is only valid on UseTexture");
            }
            UseResource(input.handle, flags);
        }

        // UseTexture and UseTextureFragment are currently forced to be mutually exclusive in the same pass
        // check this.
        // We currently ignore the version. In theory there might be some cases that are actually allowed with versioning
        // for ample UseTexture(myTexV1, read) UseFragment(myTexV2, ReadWrite) as they are different versions
        // but for now we don't allow any of that.
        private void CheckNotUseFragment(TextureHandle tex, IBaseRenderGraphBuilder.AccessFlags flags)
        {
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
        }

        public void UseTexture(in TextureHandle input, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            CheckNotUseFragment(input, flags);
            UseResource(input.handle, flags);
        }

        // Shared validation between UseTextureFragment/UseTextureFragmentDepth
        private void CheckUseFragment(TextureHandle tex, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            if ((flags & IBaseRenderGraphBuilder.AccessFlags.GrabRead) == IBaseRenderGraphBuilder.AccessFlags.GrabRead)
            {
                throw new Exception("GrabRead is only valid on UseTexture");
            }

            // We ignore the version as we don't allow mixing UseTexture/UseFragment between different versions
            // even though it should theoretically work (and we might do so in the future) for now we're overly strict.,
            bool alreadyUsed = false;

            //TODO: Check grab textures here and allow if it's grabbed. For now
            // UseTextureFragment()
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
                throw new ArgumentException($"Trying to UseTextureFragment on a texture that is already used through UseTexture. Consider using a Grab access mode or update your code. (pass {m_RenderPass.name} resource{name}).");
            }
        }

        public void UseTextureFragment(TextureHandle tex, int index, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            CheckUseFragment(tex, flags);
            UseResource(tex.handle, flags);
            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track of the handle->mrt index mapping
            m_RenderPass.SetColorBufferRaw(VersionedHandle(tex), index);
        }

        public void UseTextureFragmentDepth(TextureHandle tex, IBaseRenderGraphBuilder.AccessFlags flags)
        {
            CheckUseFragment(tex, flags);
            UseResource(tex.handle, flags);
            // Note the version for the attachments is a bit arbitrary so we just use the latest for now
            // it doesn't really matter as it's really the Read/Write lists that determine that
            // This is just to keep track to bind this handle as a depth texture.
            m_RenderPass.SetDepthBufferRaw(VersionedHandle(tex));
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, ComputeGraphContext> renderFunc) where PassData : class, new()
        {
            ((RenderGraphPass<PassData>)m_RenderPass).renderFunc = (PassData pass, InternalRenderGraphContext ctx) =>
            {
                var c = new ComputeGraphContext();
                c.FromGraphContext(ctx);
                renderFunc(pass, c);
            };
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, RasterGraphContext> renderFunc) where PassData : class, new()
        {
            ((RenderGraphPass<PassData>)m_RenderPass).renderFunc = (PassData pass, InternalRenderGraphContext ctx) =>
            {
                var c = new RasterGraphContext();
                c.FromGraphContext(ctx);
                renderFunc(pass, c);
            };
        }

        public void UseRendererList(in RendererListHandle input)
        {
            m_RenderPass.UseRendererList(input);
        }

        public TextureHandle VersionedHandle(in TextureHandle input)
        {
            if (m_Resources.GetRenderGraphResourceTransientIndex(input.handle) >= 0)
            {
                var name = m_Resources.GetRenderGraphResourceName(input.handle);
                throw new ArgumentException($"Trying to version a transient resource in a pass. Transient resources do not outlive the pass and cannot be versioned. (pass {m_RenderPass.name} resource{name}).");
            }

            TextureHandle h = new TextureHandle();
            h.handle = m_Resources.GetLatestVersionHandle(input.handle);
            return h;
        }

        public ComputeBufferHandle VersionedHandle(in ComputeBufferHandle input)
        {
            if (m_Resources.GetRenderGraphResourceTransientIndex(input.handle) >= 0)
            {
                var name = m_Resources.GetRenderGraphResourceName(input.handle);
                throw new ArgumentException($"Trying to version a transient resource in a pass. Transient resources do not outlive the pass and cannot be versioned. (pass {m_RenderPass.name} resource{name}).");
            }

            ComputeBufferHandle h = new ComputeBufferHandle();
            h.handle = m_Resources.GetLatestVersionHandle(input.handle);
            return h;
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
