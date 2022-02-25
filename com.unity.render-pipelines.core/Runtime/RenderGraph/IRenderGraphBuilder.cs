using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    public interface IBaseRenderGraphBuilder : IDisposable
    {
        [Flags]
        enum AccessFlags
        {
            None = 0, ///<summary>The pass does not access the resource at all. Calling Use* functions with none has no effect.</summary>
            Read = 1 << 0, ///<summary>This pass will read data the resource. Data in the resource should never be written unless one of the write flags is also present. Writing to a read-only resource may lead to undefined results, significant performance penaties, and GPU crashes.</summary>
            Write = 1 << 1, ///<summary>This pass will at least write some data to the resource. Data in the resource should never be read unless one of the read flags is also present. Reading from a write-only resource may lead to undefined results, significant performance penaties, and GPU crashes.</summary>
            Discard = 1 << 2, ///<summary>Previous data in the resource is not preserved. The resource will contain undefined data at the beginning of the pass.</summary>
            WriteAll = Write | Discard, ///<summary>All data in the resource will be written by this pass. Data in the resource should never be read.</summary>
            AllowGrab = 1 << 3, ///<summary>Allow auto "Grabbing" a framebuffer to a texture at the beginning of the pass. Normally UseTexture(Read) in combination with UseFrameBuffer(x) is not allowed as the texture reads would not nececarily show
                               /// modifications to the frame buffer. By passing this flag you indicate to the Graph it's ok to grab the frame buffer once at the beginning of the
                               /// pass and then use that as the  input texture. Depending on usage flags this may cause a new temporary buffer+buffer copy to be generated. This is mainly
                               /// useful in complex cases were you are unsure the handles are coming from and they may sometimes refer to the same resource. Say you want to do
                               /// distortion on the frame buffer but then also blend the distortion results over it. UseTexture(myBuff, Read  | Grab) USeFrameBuffer(myBuff, ReadWrite) to achieve this
                               /// if you have similar code  UseTexture(myBuff1, Read  | Grab) USeFrameBuffer(myBuff2, ReadWrite) where depending on the active passes myBuff1 may be different from myBuff@
                               /// the graph will always take the optimal path, not copying to a temp copy if they are in fact different, and auto-allocating a temp copy if they are.</summary>
            GrabRead = Read | AllowGrab,
            ReadWrite = Read | Write
        }

        /// <summary>
        /// Resource handles are generally implicitly versioned. This means the system will automatically take the last version in the graph up to the recorded point.
        /// Sometimes it's useful to explicitly refer to an older version of the resource. To get such an explicit versioned version of the handle you can use this function.
        /// It will return a fixed version version of the handle that can later be used to refer to this version.
        /// </summary>
        /// <param name="input">Resource to get a versioned handle for.</param>
        /// <returns>A versioned handle.</returns>
        public TextureHandle VersionedHandle(in TextureHandle input);
        public ComputeBufferHandle VersionedHandle(in ComputeBufferHandle input);

        /// <summary>
        /// This pass will read the input texture. This registers a dependency that all previous writes to the texture have to be complete before this pass can run.
        /// </summary>
        /// <param name="input">The Texture resource to read from during the pass.</param>
        public void UseTexture(in TextureHandle input, AccessFlags flags);

        /// <summary>
        /// This pass will read the input buffer. This registers a dependency that all previous writes to the buffer have to be complete before this pass can run.
        /// </summary>
        /// <param name="input">The Compute Buffer resource to read from during the pass.</param>
        public void UseComputeBuffer(in ComputeBufferHandle input, AccessFlags flags);

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
        /// This pass will read from this renderer list. RendererLists are always read-only in the graph so have no access flags.
        /// </summary>
        /// <param name="input">The Renderer List resource to use during the pass.</param>
        public void UseRendererList(in RendererListHandle input);

        /// <summary>
        /// Enable asynchronous compute for this pass.
        /// </summary>
        /// <param name="value">Set to true to enable asynchronous compute.</param>
        public void EnableAsyncCompute(bool value);

        /// <summary>
        /// Allow or not pass culling
        /// By default all passes can be culled out if the render graph detects it's not actually used.
        /// In some cases, a pass may not write or read any texture but rather do something with side effects (like setting a global texture parameter for example).
        /// This function can be used to tell the system that it should not cull this pass.
        /// </summary>
        /// <param name="value">True to allow pass culling.</param>
        public void AllowPassCulling(bool value);
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
        /// Use the texture as an rendertarget/renderpass attachment.
        /// Writing:
        /// Indicate this pass will write a texture on the current fragment position and sample index through MRT writes.
        /// The graph will automatically bind the texture as an MRT output on the indicated index slot.
        /// Write in shader as  float4 out : SV_Target{index} = value; This texture always needs to be written as an
        /// render target (SV_Targetx) writing using other methods (like `operator[] =` ) may not work even if
        /// using the current fragment+sampleIdx pos. When using operator[] please use the WriteTexture function instead.
        /// Reading:
        /// Indicates this pass will read a texture on the current fragment position. If the texture is msaa then the sample index to read
        /// may be chosen by the shader. This informs the graph that any shaders in pass will only read from this
        /// texture at the current fragment position using the LOAD_FRAMEBUFFER_INPUT(idx)/LOAD_FRAMEBUFFER_INPUT_MS(idx,sampleIdx) macros.
        /// I.e this texture is bound as an render pass attachment on compatible hardware (or as a regular texture
        ///  as a fall back).
        /// The index passed to LOAD_FRAMEBUFFER_INPUT needs to match the index passed to ReadTextureFragment for this texture.
        /// </summary>
        void UseTextureFragment(TextureHandle tex, int index, AccessFlags flags);

        /// <summary>
        /// Use the texture as a depth buffer for the Z-Buffer hardware.  Note you can only test-against and write-to a single depth texture in a pass.
        /// If you want to write depth to more than one texture you will need to register the second texture as WriteTextureFragment and manually calculate
        /// and write the depth value in the shader.
        /// Calling UseTextureFragmentDepth twice on the same builder is an error.
        /// Write:
        /// Indicate a texture will be witten with the current frament depth by the ROPs (but not for depth reading (i.e. z-test == always)).
        /// Read:
        /// Indicate a texture will be used as an input for the depth testing unit.
        /// </summary>
        /// <param name="tex"></param>
        void UseTextureFragmentDepth(TextureHandle tex, AccessFlags flags);

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
