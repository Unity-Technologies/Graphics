using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    public interface IBaseRenderGraphBuilder : IDisposable
    {
        // Converts the handle from implicit latest to a concrete version.
        // This is only needed if you want to refer to a certain version at a later
        // point in time after additional latest version commands have been recorded.
        public TextureHandle VersionedHandle(in TextureHandle input);
        public ComputeBufferHandle VersionedHandle(in ComputeBufferHandle input);

        /// <summary>
        /// This pass will read the input texture. This registers a dependency that all previous writes to the texture have to be complete before this pass can run.
        /// </summary>
        /// <param name="input">The Texture resource to read from during the pass.</param>
        public void ReadTexture(in TextureHandle input);

        /// <summary>
        /// This pass will write the input texture. This registers a dependency that this pass will run before any subsequent reads from this texture.
        /// This bumps the handle version. To get an explicit versioned handle please use "VersionedHandle" before/after the call to writetexture.
        /// </summary>
        /// <param name="input">The Texture resource to read from during the pass.</param>
        public void WriteTexture(in TextureHandle input);

        /// <summary>
        /// A shorthand of ReadTexture(handle); WriteTexture(handle); in that exact order. See documentation of ReadTexture/WriteTexture for more details.
        /// </summary>
        /// <param name="input">The Texture resource to act on.</param>
        public void ReadWriteTexture(in TextureHandle input)
        {
            ReadTexture(input);
            WriteTexture(input);
        }

        /// <summary>
        /// This pass will read the input buffer. This registers a dependency that all previous writes to the buffer have to be complete before this pass can run.
        /// </summary>
        /// <param name="input">The Compute Buffer resource to read from during the pass.</param>
        public ComputeBufferHandle ReadComputeBuffer(in ComputeBufferHandle input);

        /// <summary>
        /// This pass will write the input buffer. This registers a dependency that this pass will run before any subsequent reads from this buffer.
        /// This bumps the handle version. To get an explicit versioned handle please use "VersionedHandle" before/after the call to WriteComputeBuffer.
        /// </summary>
        /// <param name="input">The Compute Buffer resource to write to during the pass.</param>
        public ComputeBufferHandle WriteComputeBuffer(in ComputeBufferHandle input);

        /// <summary>
        /// A shorthand of ReadComputeBuffer(handle); WriteComputeBuffer(handle); in that exact order. See documentation of ReadComputeBuffer/WriteComputeBuffer for more details.
        /// </summary>
        /// <param name="input">The Texture resource to act on.</param>
        public void ReadWriteComputeBuffer(in ComputeBufferHandle input)
        {
            ReadComputeBuffer(input);
            WriteComputeBuffer(input);
        }

        /// <summary>
        /// Create a new Render Graph Texture resource.
        /// This texture will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="desc">Texture descriptor.</param>
        /// <returns>A new transient TextureHandle.</returns>
        public TextureHandle CreateTransientTexture(in TextureDesc desc);

        /// <summary>
        /// Create a new Render Graph Texture resource using the descriptor from another texture.
        /// This texture will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <returns>A new transient TextureHandle.</returns>
        public TextureHandle CreateTransientTexture(in TextureHandle texture);

        /// <summary>
        /// Create a new Render Graph Compute Buffer resource.
        /// This Compute Buffer will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="desc">Compute Buffer descriptor.</param>
        /// <returns>A new transient ComputeBufferHandle.</returns>
        public ComputeBufferHandle CreateTransientComputeBuffer(in ComputeBufferDesc desc);

        /// <summary>
        /// Create a new Render Graph Compute Buffer resource using the descriptor from another Compute Buffer.
        /// This Compute Buffer will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="computebuffer">Compute Buffer from which the descriptor should be used.</param>
        /// <returns>A new transient ComputeBufferHandle.</returns>
        public ComputeBufferHandle CreateTransientComputeBuffer(in ComputeBufferHandle computebuffer);

        /// <summary>
        /// Specify a Renderer List resource to use during the pass.
        /// </summary>
        /// <param name="input">The Renderer List resource to use during the pass.</param>
        public void UseRendererList(in RendererListHandle input);
    }

    public interface IComputeRenderGraphBuilder : IBaseRenderGraphBuilder
    {

        /// <summary>
        /// Specify the render function to use for this pass.
        /// A call to this is mandatory for the pass to be valid.
        /// </summary>
        /// <typeparam name="PassData">The Type of the class that provides data to the Render Pass.</typeparam>
        /// <param name="renderFunc">Render function for the pass.</param>
        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, ComputeGraphContext> renderFunc)
            where PassData : class, new();
    }

    public interface IRasterRenderGraphBuilder : IBaseRenderGraphBuilder
    {
        /// <summary>
        /// Indicate this pass will write a texture on the current fragment position and sample index through MRT writes.
        /// The graph will automatically bind the texture as an MRT output on the indicated index slot.
        /// Write in shader as  float4 out : SV_Target{index} = value; This texture always needs to be written as an
        /// render target (SV_Targetx) writing using other methods (like `operator[] =` ) may not work even if
        /// using the current fragment+sampleIdx pos. When using operator[] please use the WriteTexture function instead.
        /// </summary>
        void WriteTextureFragment(TextureHandle tex, int index);

        /// <summary>
        /// Indicates this pass will read a texture on the current fragment position. If the texture is msaa then the sample index to read
        /// may be chosen by the shader. This informs the graph that any shaders in pass will only read from this
        /// texture at the current fragment position using the LOAD_FRAMEBUFFER_INPUT(idx)/LOAD_FRAMEBUFFER_INPUT_MS(idx,sampleIdx) macros.
        /// I.e this texture is bound as an render pass attachment on compatible hardware (or as a regular texture
        ///  as a fall back).
        /// The index passed to LOAD_FRAMEBUFFER_INPUT needs to match the index passed to ReadTextureFragment for this texture.
        /// </summary>
        void ReadTextureFragment(TextureHandle tex, int index);

        /// <summary>
        /// A shorthand of ReadTextureFragment(handle); WriteTextureFragment(handle); in that exact order. See documentation of ReadTextureFragment/WriteTextureFragment for more details.
        /// </summary>
        /// <param name="input">The Texture resource to act on.</param>
        public void ReadWriteTextureFragment(in TextureHandle input, int index)
        {
            ReadTextureFragment(input, index);
            WriteTextureFragment(input, index);
        }

        /// <summary>
        /// Indicate a texture will be used as an input for the depth testing unit.
        /// NOTE: The depth testing unit can only test against a single texture. Doing multiple ReadTextureFragmentDepth's in a single pass is an error.
        /// Note you can only read and write from the same depth texture in a pass. You cannot test against one depth buffer but write to another buffer.
        /// </summary>
        /// <param name="tex"></param>
        void ReadTextureFragmentDepth(TextureHandle tex);

        /// <summary>
        /// Indicate a texture will be witten with the current frament depth by the ROPs (but not for depth reading (i.e. z-test == always)).
        /// NOTE: You can only write depth to a single texture in a pass. If you want to write depth to more than one texture
        /// you will need to register the second texture as WriteTextureFragment and manually calculate and write the depth value in the shader.
        /// Note you can only read and write from the same depth texture in a pass. You cannot test against one depth buffer but write to another buffer.
        /// </summary>
        /// <param name="tex"></param>
        void WriteTextureFragmentDepth(TextureHandle tex);

        /// <summary>
        /// A shorthand of ReadTextureFragment(handle); WriteTextureFragment(handle); in that exact order. See documentation of ReadTextureFragment/WriteTextureFragment for more details.
        /// Note this is the common use case for a pass that does "traditional" z-buffer based rendering where it is both read (to do the comparison against the current nearest) and written (do register the newest nearest depth)
        /// </summary>
        /// <param name="input">The Texture resource to act on.</param>
        public void ReadWriteTextureFragmentDepth(in TextureHandle input)
        {
            ReadTextureFragmentDepth(input);
            WriteTextureFragmentDepth(input);
        }

        /// <summary>
        /// Specify the render function to use for this pass.
        /// A call to this is mandatory for the pass to be valid.
        /// </summary>
        /// <typeparam name="PassData">The Type of the class that provides data to the Render Pass.</typeparam>
        /// <param name="renderFunc">Render function for the pass.</param>
        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, RasterGraphContext> renderFunc)
            where PassData : class, new();
    }
}
