using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Use this struct to set up a new Render Pass.
    /// </summary>
    public struct RenderGraphBuilder : IDisposable
    {
        RenderGraphPass             m_RenderPass;
        RenderGraphResourceRegistry m_Resources;
        bool                        m_Disposed;

        #region Public Interface
        /// <summary>
        /// Specify that the pass will use a Texture resource as a color render target.
        /// This has the same effect as WriteTexture and also automatically sets the Texture to use as a render target.
        /// </summary>
        /// <param name="input">The Texture resource to use as a color render target.</param>
        /// <param name="index">Index for multiple render target usage.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle UseColorBuffer(TextureHandle input, int index)
        {
            CheckTransientResource(RenderGraphResourceType.Texture, input);
            m_RenderPass.SetColorBuffer(input, index);
            return input;
        }

        /// <summary>
        /// Specify that the pass will use a Texture resource as a depth buffer.
        /// </summary>
        /// <param name="input">The Texture resource to use as a depth buffer during the pass.</param>
        /// <param name="flags">Specify the access level for the depth buffer. This allows you to say whether you will read from or write to the depth buffer, or do both.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle UseDepthBuffer(TextureHandle input, DepthAccess flags)
        {
            CheckTransientResource(RenderGraphResourceType.Texture, input);
            m_RenderPass.SetDepthBuffer(input, flags);
            return input;
        }

        /// <summary>
        /// Specify a Texture resource to read from during the pass.
        /// </summary>
        /// <param name="input">The Texture resource to read from during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle ReadTexture(TextureHandle input)
        {
            CheckTransientResource(RenderGraphResourceType.Texture, input);
            m_RenderPass.AddResourceRead(RenderGraphResourceType.Texture, input);
            return input;
        }

        /// <summary>
        /// Specify a Texture resource to write to during the pass.
        /// </summary>
        /// <param name="input">The Texture resource to write to during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle WriteTexture(TextureHandle input)
        {
            CheckTransientResource(RenderGraphResourceType.Texture, input);
            // TODO RENDERGRAPH: Manage resource "version" for debugging purpose
            m_RenderPass.AddResourceWrite(RenderGraphResourceType.Texture, input);
            return input;
        }

        /// <summary>
        /// Create a new Render Graph Texture resource.
        /// This texture will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="desc">Texture descriptor.</param>
        /// <returns>A new transient TextureHandle.</returns>
        public TextureHandle CreateTransientTexture(in TextureDesc desc)
        {
            var result = m_Resources.CreateTexture(desc, 0, m_RenderPass.index);
            m_RenderPass.AddTransientResource(RenderGraphResourceType.Texture, result);
            return result;
        }

        /// <summary>
        /// Create a new Render Graph Texture resource using the descriptor from another texture.
        /// </summary>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <returns>A new transient TextureHandle.</returns>
        public TextureHandle CreateTransientTexture(TextureHandle texture)
        {
            var desc = m_Resources.GetTextureResourceDesc(texture);
            var result = m_Resources.CreateTexture(desc, 0, m_RenderPass.index);
            m_RenderPass.AddTransientResource(RenderGraphResourceType.Texture, result);
            return result;
        }

        /// <summary>
        /// Specify a Renderer List resource to use during the pass.
        /// </summary>
        /// <param name="input">The Renderer List resource to use during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public RendererListHandle UseRendererList(RendererListHandle input)
        {
            m_RenderPass.UseRendererList(input);
            return input;
        }

        /// <summary>
        /// Specify a Compute Buffer resource to read from during the pass.
        /// </summary>
        /// <param name="input">The Compute Buffer resource to read from during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public ComputeBufferHandle ReadComputeBuffer(ComputeBufferHandle input)
        {
            CheckTransientResource(RenderGraphResourceType.ComputeBuffer, input);
            m_RenderPass.AddResourceRead(RenderGraphResourceType.ComputeBuffer, input);
            return input;
        }

        /// <summary>
        /// Specify a Compute Buffer resource to write to during the pass.
        /// </summary>
        /// <param name="input">The Compute Buffer resource to write to during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public ComputeBufferHandle WriteComputeBuffer(ComputeBufferHandle input)
        {
            CheckTransientResource(RenderGraphResourceType.ComputeBuffer, input);
            m_RenderPass.AddResourceWrite(RenderGraphResourceType.ComputeBuffer, input);
            return input;
        }

        /// <summary>
        /// Create a new Render Graph Compute Buffer resource.
        /// This buffer will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="desc">Compute Buffer descriptor.</param>
        /// <returns>A new transient ComputeBufferHandle.</returns>
        public ComputeBufferHandle CreateTransientComputeBuffer(in ComputeBufferDesc desc)
        {
            var result = m_Resources.CreateComputeBuffer(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(RenderGraphResourceType.ComputeBuffer, result);
            return result;
        }

        /// <summary>
        /// Create a new Render Graph Texture resource using the descriptor from another texture.
        /// </summary>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <returns>A new transient TextureHandle.</returns>
        public ComputeBufferHandle CreateTransientComputeBuffer(ComputeBufferHandle buffer)
        {
            var desc = m_Resources.GetComputeBufferResourceDesc(buffer);
            var result = m_Resources.CreateComputeBuffer(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(RenderGraphResourceType.ComputeBuffer, result);
            return result;
        }

        /// <summary>
        /// Specify the render function to use for this pass.
        /// A call to this is mandatory for the pass to be valid.
        /// </summary>
        /// <typeparam name="PassData">The Type of the class that provides data to the Render Pass.</typeparam>
        /// <param name="renderFunc">Render function for the pass.</param>
        public void SetRenderFunc<PassData>(RenderFunc<PassData> renderFunc) where PassData : class, new()
        {
            ((RenderGraphPass<PassData>)m_RenderPass).renderFunc = renderFunc;
        }

        /// <summary>
        /// Enable asynchronous compute for this pass.
        /// </summary>
        /// <param name="value">Set to true to enable asynchronous compute.</param>
        public void EnableAsyncCompute(bool value)
        {
            m_RenderPass.EnableAsyncCompute(value);
        }

        /// <summary>
        /// Allow or not pass pruning
        /// By default all passes can be pruned out if the render graph detects it's not actually used.
        /// In some cases, a pass may not write or read any texture but rather do something with side effects (like setting a global texture parameter for example).
        /// This function can be used to tell the system that it should not prune this pass.
        /// </summary>
        /// <param name="value"></param>
        public void AllowPassPruning(bool value)
        {
            m_RenderPass.AllowPassPruning(value);
        }

        /// <summary>
        /// Dispose the RenderGraphBuilder instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #region Internal Interface
        internal RenderGraphBuilder(RenderGraphPass renderPass, RenderGraphResourceRegistry resources)
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

        void CheckTransientResource(RenderGraphResourceType type, int input)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (input != -1)
            {
                int transientIndex = m_Resources.GetResourceTransientIndex(type, input);
                if (transientIndex != -1 && transientIndex != m_RenderPass.index)
                {
                    throw new ArgumentException($"Trying to use a transient texture (pass index {transientIndex}) in a different pass (pass index {m_RenderPass.index}.");
                }
            }
#endif
        }
        #endregion
    }
}
