using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Common base interface for the different render graph builders. These functions are supported on all builders.
    /// </summary>
    public interface IBaseRenderGraphBuilder : IDisposable
    {

        /// <summary>
        /// Express the operations the rendergraph pass will do on a resource.
        /// </summary>
        [Flags]
        public enum AccessFlags
        {
            ///<summary>The pass does not access the resource at all. Calling Use* functions with none has no effect.</summary>
            None = 0,
            ///<summary>This pass will read data the resource. Data in the resource should never be written unless one of the write flags is also present. Writing to a read-only resource may lead to undefined results, significant performance penaties, and GPU crashes.</summary>
            Read = 1 << 0,

            ///<summary>This pass will at least write some data to the resource. Data in the resource should never be read unless one of the read flags is also present. Reading from a write-only resource may lead to undefined results, significant performance penaties, and GPU crashes.</summary>
            Write = 1 << 1,

            ///<summary>Previous data in the resource is not preserved. The resource will contain undefined data at the beginning of the pass.</summary>
            Discard = 1 << 2,

            ///<summary>All data in the resource will be written by this pass. Data in the resource should never be read.</summary>
            WriteAll = Write | Discard,

            /// <summary>Allow auto "Grabbing" a framebuffer to a texture at the beginning of the pass. Normally UseTexture(Read) in combination with UseFrameBuffer(x) is not allowed as the texture reads would not nececarily show
            /// modifications to the frame buffer. By passing this flag you indicate to the Graph it's ok to grab the frame buffer once at the beginning of the
            /// pass and then use that as the  input texture. Depending on usage flags this may cause a new temporary buffer+buffer copy to be generated. This is mainly
            /// useful in complex cases were you are unsure the handles are coming from and they may sometimes refer to the same resource. Say you want to do
            /// distortion on the frame buffer but then also blend the distortion results over it. UseTexture(myBuff, Read  | Grab) USeFrameBuffer(myBuff, ReadWrite) to achieve this
            /// if you have similar code  UseTexture(myBuff1, Read  | Grab) USeFrameBuffer(myBuff2, ReadWrite) where depending on the active passes myBuff1 may be different from myBuff@
            /// the graph will always take the optimal path, not copying to a temp copy if they are in fact different, and auto-allocating a temp copy if they are.</summary>
            AllowGrab = 1 << 3,

            ///<summary> Shortcut for Read | AllowGrab</summary>
            GrabRead = Read | AllowGrab,

            ///<summary> Shortcut for Read | Write</summary>
            ReadWrite = Read | Write
        }

        /// <summary>
        /// Declare that this pass uses the input texture.
        /// </summary>
        /// <param name="input">The texture resource to use during the pass.</param>
        /// <param name="flags">A combination of flags indicating how the resource will be used during the pass. Default value is set to AccessFlag.Read </param>
        /// <returns>A explicitly versioned handle representing the latest version of the passed in texture.
        /// Note that except for special cases where you want to refer to a specific version return value is generally discarded.</returns>
        public TextureHandle UseTexture(in TextureHandle input, AccessFlags flags = AccessFlags.Read);

        /// <summary>
        /// Declare that this pass uses the texture assigned to the global texture slot. The actual texture referenced is indirectly specified here it depends
        /// on the value previous passes that were added to the graph set for the global texture slot. If no previous pass set a texture to the global slot an
        /// exception will be raised.
        /// </summary>
        /// <param name="glboalTextureSlotId">The global texture slot read by shaders in this pass. Use Shader.PropertyToID to generate these ids.</param>
        /// <param name="flags">A combination of flags indicating how the resource will be used during the pass. Default value is set to AccessFlag.Read </param>
        public void UseGlobalTexture(int glboalTextureSlotId, AccessFlags flags = AccessFlags.Read);

        /// <summary>
        /// Indicate that this pass will reference all textures in global texture slots known to the graph. The default setting is true.
        /// It is highly recommended if you know which globals you pass will access to set this value to false and do UseTexture(glboalTextureSlotId) instead
        /// to ensure the graph can maximally optimize resource use and lifetimes.
        ///
        /// Note that in some cases it is difficult/impossible to know which globals a pass will access. This is for example true if your pass renders objects
        /// in the scene (e.g. using CommandBuffer.DrawRendererList) that might be using arbitrary shaders which in turn may access arbitrary global textures.
        /// In this situation you will either have to ensure *all* shaders are well behaved and do not access spurious globals our make sure your rendererlists
        /// filters allow only shaders that are known to be well behaved to pass.
        /// </summary>
        /// <param name="enable">If true the pass from which this is called will reference all global textures.</param>
        public void UseAllGlobalTextures(bool enable);

        /// <summary>
        /// Make this pass set a global texture slot *at the end* of this pass. During this pass the global texture will still have it's
        /// old value. Only after this pass the global texture slot will take on the new value specified.
        /// Generally this pass will also do a UseTexture(write) on this texture handle to indicate it is generating this texture, but this is not really a requirement
        /// you can have a pass that simply sets up a new value in a global texture slot but doesn't write it.
        /// Although counter-intuitive at first, this call doesn't actually have a dependency on the passed in texture handle. It's only when a subsequent pass has
        /// a dependency on the global texture slot that subsequent pass will get a dependency on the currently set global texture for that slot. This means
        /// globals slots can be set witout overhead if you're unsure if a resource will be used or not, the graph will still maintain the correct lifetimes.
        /// </summary>
        /// <param name="input">The texture value to set in the global texture slot. This can be an null handle to clear the global texture slot.</param>
        /// <param name="globalPropertyID">The global texture slot to set the value for. Use Shader.PropertyToID to generate the id.</param>
        public void PostSetGlobalTexture(in TextureHandle input, int globalPropertyID);

        /// <summary>
        /// Declare that this pass uses the input compute buffer.
        /// </summary>
        /// <param name="input">The compute buffer resource to use during the pass.</param>
        /// <param name="flags">A combination of flags indicating how the resource will be used during the pass. Default value is set to AccessFlag.Read </param>
        /// <returns>A explicitly versioned handle representing the latest version of the passed in texture.
        /// Note that except for special cases where you want to refer to a specific version return value is generally discarded.</returns>
        public BufferHandle UseBuffer(in BufferHandle input, AccessFlags flags = AccessFlags.Read);

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
        /// Create a new Render Graph Graphics Buffer resource.
        /// This Graphics Buffer will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="desc">Compute Buffer descriptor.</param>
        /// <returns>A new transient BufferHandle.</returns>
        public BufferHandle CreateTransientBuffer(in BufferDesc desc);

        /// <summary>
        /// Create a new Render Graph Graphics Buffer resource using the descriptor from another Graphics Buffer.
        /// This Graphics Buffer will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="computebuffer">Graphics Buffer from which the descriptor should be used.</param>
        /// <returns>A new transient BufferHandle.</returns>
        public BufferHandle CreateTransientBuffer(in BufferHandle computebuffer);

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

        /// <summary>
        /// Allow commands in the command buffer to modify global state. This will introduce a render graph sync-point in the frame and cause all passes after this pass to never be
        /// reordered before this pass. This may nave negative impact on performance and memory use if not used carefully so it is recommended to only allow this in specific use cases.
        /// This will also set AllowPassCulling to false.
        /// </summary>
        /// <param name="value">True to allow global state modification.</param>
        public void AllowGlobalStateModification(bool value);
    }

    /// <summary>
    /// A builder for a compute render pass
    /// <see cref="RenderGraph.AddComputePass"/>
    /// </summary>
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

    /// <summary>
    /// A builder for a low-level render pass.
    /// <see cref="RenderGraph.AddLowLevelPass"/>
    /// </summary>
    public interface ILowLevelRenderGraphBuilder : IBaseRenderGraphBuilder
    {

        /// <summary>
        /// Specify the render function to use for this pass.
        /// A call to this is mandatory for the pass to be valid.
        /// </summary>
        /// <typeparam name="PassData">The Type of the class that provides data to the Render Pass.</typeparam>
        /// <param name="renderFunc">Render function for the pass.</param>
        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, LowLevelGraphContext> renderFunc)
            where PassData : class, new();
    }

    /// <summary>
    /// A builder for a raster render pass
    /// <see cref="RenderGraph.AddRasterRenderPass"/>
    /// </summary>
    public interface IRasterRenderGraphBuilder : IBaseRenderGraphBuilder
    {
        /// <summary>
        /// Use the texture as an rendertarget attachment.
        ///
        /// Writing:
        /// Indicate this pass will write a texture through rendertarget rasterization writes.
        /// The graph will automatically bind the texture as an MRT output on the indicated index slot.
        /// Write in shader as  float4 out : SV_Target{index} = value; This texture always needs to be written as an
        /// render target (SV_Targetx) writing using other methods (like `operator[] =` ) may not work even if
        /// using the current fragment+sampleIdx pos. When using operator[] please use the UseTexture function instead.
        /// Reading:
        /// Indicates this pass will read a texture on the current fragment position but not unnecessarily modify it. Although not explicitly visible in shader code
        /// Reading may happen depending on the rasterization state, e.g. Blending (read and write) or Z-Testing (read only) may read the buffer.
        ///
        /// Note: The rendergraph does not know what content will be rendered in the bound texture. By default it assumes only partial data
        /// is written (e.g. a small rectangle is drawn on the screen) so it will preserve the existing rendertarget content (e.g. behind/around the triangle)
        /// if you know you will write the full screen the AccessFlags.WriteAll should be used instead as it will give better performance.
        /// </summary>
        /// <param name="tex">Texture to use during this pass.</param>
        /// <param name="index">Index the shader will use to access this texture.</param>
        /// <param name="flags">How this pass will acess the texture. Default value is set to AccessFlag.Write </param>
        /// <returns>A explicitly versioned handle representing the latest version of the passed in texture.
        /// Note that except for special cases where you want to refer to a specific version return value is generally discarded.</returns>
        TextureHandle UseTextureFragment(TextureHandle tex, int index, AccessFlags flags = AccessFlags.Write);

        /// <summary>
        /// Use the texture as an input attachment.
        ///
        /// This informs the graph that any shaders in pass will only read from this texture at the current fragment position using the
        /// LOAD_FRAMEBUFFER_INPUT(idx)/LOAD_FRAMEBUFFER_INPUT_MS(idx,sampleIdx) macros. The index passed to LOAD_FRAMEBUFFER_INPUT needs
        /// to match the index passed to UseTextureFragmentInput for this texture.
        ///
        /// </summary>
        /// <param name="tex">Texture to use during this pass.</param>
        /// <param name="index">Index the shader will use to access this texture.</param>
        /// <param name="flags">How this pass will acess the texture. Default value is set to AccessFlag.Read. Writing is currently not supported on any platform. </param>
        /// <returns>A explicitly versioned handle representing the latest version of the passed in texture.
        /// Note that except for special cases where you want to refer to a specific version return value is generally discarded.</returns>
        TextureHandle UseTextureFragmentInput(TextureHandle tex, int index, AccessFlags flags = AccessFlags.Read);

        /// <summary>
        /// Use the texture as a depth buffer for the Z-Buffer hardware.  Note you can only test-against and write-to a single depth texture in a pass.
        /// If you want to write depth to more than one texture you will need to register the second texture as UseTextureFragment and manually calculate
        /// and write the depth value in the shader.
        /// Calling UseTextureFragmentDepth twice on the same builder is an error.
        /// Write:
        /// Indicate a texture will be written with the current fragment depth by the ROPs (but not for depth reading (i.e. z-test == always)).
        /// Read:
        /// Indicate a texture will be used as an input for the depth testing unit.
        /// </summary>
        /// <param name="tex">Texture to use during this pass.</param>
        /// <param name="flags">How this pass will acess the texture. Default value is set to AccessFlag.Write </param>
        /// <returns>A explicitly versioned handle representing the latest version of the passed in texture.
        /// Note that except for special cases where you want to refer to a specific version return value is generally discarded.</returns>
        TextureHandle UseTextureFragmentDepth(TextureHandle tex, AccessFlags flags = AccessFlags.Write);

        /// <summary>
        /// Use the texture as an random access attachment. This is called "Unordered Access View" in DX12 and "Storage Image" in Vulkan.
        ///
        /// This informs the graph that any shaders in the pass will access the texture as a random access attachment through RWTexture2d&lt;T&gt;, RWTexture3d&lt;T&gt;,... 
        /// The texture can then be read/written by regular HLSL commands (including atomics, etc.).
        ///
        /// As in other parts of the Unity graphics APIs random access textures share the index-based slots with render targets and input attachments. See CommandBuffer.SetRandomWriteTarget for details.
        /// </summary>
        /// <param name="tex">Texture to use during this pass.</param>
        /// <param name="index">Index the shader will use to access this texture. This is set in the shader through the `register(ux)`  keyword.</param>
        /// <param name="flags">How this pass will acess the texture. Default value is set to AccessFlag.ReadWrite.</param>
        /// <returns>A explicitly versioned handle representing the latest version of the passed in texture.
        /// Note that except for special cases where you want to refer to a specific version return value is generally discarded.</returns>
        TextureHandle UseTextureRandomAccess(TextureHandle tex, int index, AccessFlags flags = AccessFlags.ReadWrite);

        /// <summary>
        /// Use the buffer as an random access attachment. This is called "Unordered Access View" in DX12 and "Storage Buffer" in Vulkan.
        ///
        /// This informs the graph that any shaders in the pass will access the buffer as a random access attachment through RWStructuredBuffer, RWByteAddressBuffer,... 
        /// The buffer can then be read/written by regular HLSL commands (including atomics, etc.).
        ///
        /// As in other parts of the Unity graphics APIs random access buffers share the index-based slots with render targets and input attachments. See CommandBuffer.SetRandomWriteTarget for details.
        /// </summary>
        /// <param name="tex">Buffer to use during this pass.</param>
        /// <param name="index">Index the shader will use to access this texture. This is set in the shader through the `register(ux)`  keyword.</param>
        /// <param name="flags">How this pass will acess the buffer. Default value is set to AccessFlag.Read.</param>
        /// <returns>A explicitly versioned handle representing the latest version of the passed in buffer.
        /// Note that except for special cases where you want to refer to a specific version return value is generally discarded.</returns>
        BufferHandle UseBufferRandomAccess(BufferHandle tex, int index, AccessFlags flags = AccessFlags.Read);

        /// <summary>
        /// Use the buffer as an random access attachment. This is called "Unordered Access View" in DX12 and "Storage Buffer" in Vulkan.
        ///
        /// This informs the graph that any shaders in the pass will access the buffer as a random access attachment through RWStructuredBuffer, RWByteAddressBuffer,... 
        /// The buffer can then be read/written by regular HLSL commands (including atomics, etc.).
        ///
        /// As in other parts of the Unity graphics APIs random access buffers share the index-based slots with render targets and input attachments. See CommandBuffer.SetRandomWriteTarget for details.
        /// </summary>
        /// <param name="tex">Buffer to use during this pass.</param>
        /// <param name="index">Index the shader will use to access this texture. This is set in the shader through the `register(ux)`  keyword.</param>
        /// <param name="preserveCounterValue">Whether to leave the append/consume counter value unchanged. The default is to preserve the value.</param>
        /// <param name="flags">How this pass will acess the buffer. Default value is set to AccessFlag.Read.</param>
        /// <returns>A explicitly versioned handle representing the latest version of the passed in buffer.
        /// Note that except for special cases where you want to refer to a specific version return value is generally discarded.</returns>
        BufferHandle UseBufferRandomAccess(BufferHandle tex, int index, bool preserveCounterValue, AccessFlags flags = AccessFlags.Read);

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
