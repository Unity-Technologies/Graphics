using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    public struct RenderGraphBuilder : IDisposable
    {
        RenderGraph.RenderPass      m_RenderPass;
        RenderGraphResourceRegistry m_Resources;
        bool                        m_Disposed;

        #region Public Interface
        public RenderGraphMutableResource UseColorBuffer(in RenderGraphMutableResource input, int index)
        {
            if (input.type != RenderGraphResourceType.Texture)
                throw new ArgumentException("Trying to write to a resource that is not a texture or is invalid.");

            m_RenderPass.SetColorBuffer(input, index);
            m_Resources.UpdateTextureFirstWrite(input, m_RenderPass.index);
            return input;
        }

        public RenderGraphMutableResource UseDepthBuffer(in RenderGraphMutableResource input, DepthAccess flags)
        {
            if (input.type != RenderGraphResourceType.Texture)
                throw new ArgumentException("Trying to write to a resource that is not a texture or is invalid.");

            m_RenderPass.SetDepthBuffer(input, flags);
            if ((flags | DepthAccess.Read) != 0)
                m_Resources.UpdateTextureLastRead(input, m_RenderPass.index);
            if ((flags | DepthAccess.Write) != 0)
                m_Resources.UpdateTextureFirstWrite(input, m_RenderPass.index);
            return input;
        }

        public RenderGraphResource ReadTexture(in RenderGraphResource input)
        {
            if (input.type != RenderGraphResourceType.Texture)
                throw new ArgumentException("Trying to read a resource that is not a texture or is invalid.");
            m_RenderPass.resourceReadList.Add(input);
            m_Resources.UpdateTextureLastRead(input, m_RenderPass.index);
            return input;
        }

        public RenderGraphMutableResource WriteTexture(in RenderGraphMutableResource input)
        {
            if (input.type != RenderGraphResourceType.Texture)
                throw new ArgumentException("Trying to write to a resource that is not a texture or is invalid.");
            // TODO: Manage resource "version" for debugging purpose
            m_RenderPass.resourceWriteList.Add(input);
            m_Resources.UpdateTextureFirstWrite(input, m_RenderPass.index);
            return input;
        }

        public RenderGraphResource UseRendererList(in RenderGraphResource resource)
        {
            if (resource.type != RenderGraphResourceType.RendererList)
                throw new ArgumentException("Trying use a resource that is not a renderer list.");
            m_RenderPass.usedRendererListList.Add(resource);
            return resource;
        }
        public void SetRenderFunc<PassData>(RenderFunc<PassData> renderFunc) where PassData : class, new()
        {
            ((RenderGraph.RenderPass<PassData>)m_RenderPass).renderFunc = renderFunc;
        }

        public void EnableAsyncCompute(bool value)
        {
            m_RenderPass.enableAsyncCompute = value;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #region Internal Interface
        internal RenderGraphBuilder(RenderGraph.RenderPass renderPass, RenderGraphResourceRegistry resources)
        {
            m_RenderPass = renderPass;
            m_Resources = resources;
            m_Disposed = false;
        }

        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            m_Disposed = true;
        }
        #endregion
    }
}
