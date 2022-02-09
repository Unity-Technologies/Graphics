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
            throw new NotImplementedException();
        }

        public ComputeBufferHandle ReadComputeBuffer(in ComputeBufferHandle input)
        {
            CheckResource(input.handle);
            m_RenderPass.AddResourceRead(input.handle);
            return input;
        }

        public void ReadTexture(in TextureHandle input)
        {
            CheckResource(input.handle);

            if (!m_Resources.IsRenderGraphResourceImported(input.handle) && m_Resources.TextureNeedsFallback(input))
            {
                // If texture is read from but never written to, return a fallback black texture to have valid reads
                // Return one from the preallocated default textures if possible
                // TODO: Handle this internally in graph using renaming.
                // Is this even safe to do say a shader does a tex.GetDimensions ? 
                var desc = m_Resources.GetTextureResourceDesc(input.handle);
                /*if (!desc.bindTextureMS)
                {
                    if (desc.dimension == TextureXR.dimension)
                        return m_RenderGraph.defaultResources.blackTextureXR;
                    else if (desc.dimension == TextureDimension.Tex3D)
                        return m_RenderGraph.defaultResources.blackTexture3DXR;
                    else
                        return m_RenderGraph.defaultResources.blackTexture;
                }*/
                // If not, force a write to the texture so that it gets allocated, and ensure it gets initialized with a clear color
                if (!desc.clearBuffer)
                    m_Resources.ForceTextureClear(input.handle, Color.black);
                WriteTexture(input);
            }

            m_RenderPass.AddResourceRead(input.handle);
        }

        public void ReadTextureFragment(TextureHandle tex, int index)
        {
            CheckResource(tex.handle, true);
            m_Resources.IncrementWriteCount(tex.handle);
            m_RenderPass.SetColorBuffer(tex, index, DepthAccess.Read);
        }

        public void ReadTextureFragmentDepth(TextureHandle tex)
        {
            CheckResource(tex.handle, true);
            m_Resources.IncrementWriteCount(tex.handle);
            m_RenderPass.SetDepthBuffer(tex, DepthAccess.ReadWrite);
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, ComputeGraphContext> renderFunc) where PassData : class, new()
        {
            throw new NotImplementedException();
        }

        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, RasterGraphContext> renderFunc) where PassData : class, new()
        {
            throw new NotImplementedException();
        }

        public void UseRendererList(in RendererListHandle input)
        {
            m_RenderPass.UseRendererList(input);
        }

        public TextureHandle VersionedHandle(in TextureHandle input)
        {
            throw new NotImplementedException();
        }

        public ComputeBufferHandle VersionedHandle(in ComputeBufferHandle input)
        {
            throw new NotImplementedException();
        }

        public ComputeBufferHandle WriteComputeBuffer(in ComputeBufferHandle input)
        {
            CheckResource(input.handle);
            m_RenderPass.AddResourceWrite(input.handle);
            m_Resources.IncrementWriteCount(input.handle);
            return input;
        }

        public void WriteTexture(in TextureHandle input)
        {
            CheckResource(input.handle);
            m_Resources.IncrementWriteCount(input.handle);
            m_RenderPass.AddResourceWrite(input.handle);
        }

        public void WriteTextureFragment(TextureHandle tex, int index)
        {
            CheckResource(tex.handle, true);
            m_Resources.IncrementWriteCount(tex.handle);
            m_RenderPass.SetColorBuffer(tex, index, DepthAccess.Write);
        }

        public void WriteTextureFragmentDepth(TextureHandle tex)
        {
            CheckResource(tex.handle, true);
            m_Resources.IncrementWriteCount(tex.handle);
            m_RenderPass.SetDepthBuffer(tex, DepthAccess.Write);
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
