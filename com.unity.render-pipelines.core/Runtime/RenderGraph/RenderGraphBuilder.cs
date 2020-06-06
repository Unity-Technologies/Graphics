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
            CheckTransientTexture(input);

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
            CheckTransientTexture(input);

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
            CheckTransientTexture(input);

            m_RenderPass.AddTextureRead(input);
            return input;
        }

        /// <summary>
        /// Specify a Texture resource to write to during the pass.
        /// </summary>
        /// <param name="input">The Texture resource to write to during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle WriteTexture(TextureHandle input)
        {
            CheckTransientTexture(input);

            // TODO: Manage resource "version" for debugging purpose
            m_RenderPass.AddTextureWrite(input);
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
            m_RenderPass.AddTransientTexture(result);
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
            m_RenderPass.AddTransientTexture(result);
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
            m_RenderPass.AddBufferRead(input);
            return input;
        }

        /// <summary>
        /// Specify a Compute Buffer resource to write to during the pass.
        /// </summary>
        /// <param name="input">The Compute Buffer resource to write to during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public ComputeBufferHandle WriteComputeBuffer(ComputeBufferHandle input)
        {
            m_RenderPass.AddBufferWrite(input);
            return input;
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

        void CheckTransientTexture(TextureHandle input)
        {
            if (input.transientPassIndex != -1 && input.transientPassIndex != m_RenderPass.index)
            {
                throw new ArgumentException($"Trying to use a transient texture (pass index {input.transientPassIndex}) in a different pass (pass index {m_RenderPass.index}.");
            }
        }
        #endregion
    }
}
