using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Common base interface for the different render graph builders. These functions are supported on all builders.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public interface IBaseRenderGraphBuilder : IDisposable
    {
        /// <summary>
        /// Declare that this pass uses the input texture.
        /// </summary>
        /// <param name="input">The texture resource to use during the pass.</param>
        /// <param name="flags">A combination of flags indicating how the resource will be used during the pass. Default value is set to AccessFlag.Read </param>
        public void UseTexture(in TextureHandle input, AccessFlags flags = AccessFlags.Read);

        /// <summary>
        /// Declare that this pass uses the texture assigned to the global texture slot. The actual texture referenced is indirectly specified here it depends
        /// on the value previous passes that were added to the graph set for the global texture slot. If no previous pass set a texture to the global slot an
        /// exception will be raised.
        /// </summary>
        /// <param name="propertyId">The global texture slot read by shaders in this pass. Use Shader.PropertyToID to generate these ids.</param>
        /// <param name="flags">A combination of flags indicating how the resource will be used during the pass. Default value is set to AccessFlag.Read </param>
        public void UseGlobalTexture(int propertyId, AccessFlags flags = AccessFlags.Read);

        /// <summary>
        /// Indicate that this pass will reference all textures in global texture slots known to the graph. The default setting is false.
        /// It is highly recommended if you know which globals you pass will access to use UseTexture(glboalTextureSlotId) with individual texture slots instead of
        /// UseAllGlobalTextures(true) to ensure the graph can maximally optimize resource use and lifetimes.
        ///
        /// This function should only be used in cases where it is difficult/impossible to know which globals a pass will access. This is for example true if your pass
        /// renders objects in the scene (e.g. using CommandBuffer.DrawRendererList) that might be using arbitrary shaders which in turn may access arbitrary global textures.
        /// To avoid having to do a UseAllGlobalTextures(true) in this situation, you will either have to ensure *all* shaders are well behaved and do not access spurious
        /// globals our make sure your renderer list filters allow only shaders that are known to be well behaved to pass.
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
        /// globals slots can be set without overhead if you're unsure if a resource will be used or not, the graph will still maintain the correct lifetimes.
        ///
        /// NOTE: When the `RENDER_GRAPH_CLEAR_GLOBALS` define is set, all shader bindings set through this function will be cleared once graph execution completes.
        /// </summary>
        /// <param name="input">The texture value to set in the global texture slot. This can be an null handle to clear the global texture slot.</param>
        /// <param name="propertyId">The global texture slot to set the value for. Use Shader.PropertyToID to generate the id.</param>
        public void SetGlobalTextureAfterPass(in TextureHandle input, int propertyId);

        /// <summary>
        /// Declare that this pass uses the input compute buffer.
        /// </summary>
        /// <param name="input">The compute buffer resource to use during the pass.</param>
        /// <param name="flags">A combination of flags indicating how the resource will be used during the pass. Default value is set to AccessFlag.Read </param>
        /// <returns>The value passed to 'input'. You should not use the returned value it will be removed in the future.</returns>
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
        /// Allow or not pass culling.
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

        /// <summary>
        /// Enable foveated rendering for this pass.
        /// </summary>
        /// <param name="value">True to enable foveated rendering.</param>
        public void EnableFoveatedRasterization(bool value);
    }

    /// <summary>
    /// A builder for a compute render pass
    /// <see cref="RenderGraph.AddComputePass"/>
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
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
    /// A builder for an unsafe render pass.
    /// <see cref="RenderGraph.AddUnsafePass"/>
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public interface IUnsafeRenderGraphBuilder : IBaseRenderGraphBuilder
    {
        /// <summary>
        /// Specify the render function to use for this pass.
        /// A call to this is mandatory for the pass to be valid.
        /// </summary>
        /// <typeparam name="PassData">The Type of the class that provides data to the Render Pass.</typeparam>
        /// <param name="renderFunc">Render function for the pass.</param>
        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, UnsafeGraphContext> renderFunc)
            where PassData : class, new();
    }

    /// <summary>
    /// A builder for a raster render pass
    /// <see cref="RenderGraph.AddRasterRenderPass"/>
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
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
        /// <param name="flags">How this pass will access the texture. Default value is set to AccessFlag.Write </param>
        void SetRenderAttachment(TextureHandle tex, int index, AccessFlags flags = AccessFlags.Write)
        {
            SetRenderAttachment(tex, index, flags, 0, -1);
        }

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
        /// <param name="flags">How this pass will access the texture. </param>
        /// <param name="mipLevel">Selects which mip map to used.</param>
        /// <param name="depthSlice">Used to index into a texture array. Use -1 to use bind all slices.</param>
        void SetRenderAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice);

        /// <summary>
        /// Use the texture as an input attachment.
        ///
        /// This informs the graph that any shaders in pass will only read from this texture at the current fragment position using the
        /// LOAD_FRAMEBUFFER_INPUT(idx)/LOAD_FRAMEBUFFER_INPUT_MS(idx,sampleIdx) macros. The index passed to LOAD_FRAMEBUFFER_INPUT needs
        /// to match the index passed to SetInputAttachment for this texture.
        ///
        /// </summary>
        /// <param name="tex">Texture to use during this pass.</param>
        /// <param name="index">Index the shader will use to access this texture.</param>
        /// <param name="flags">How this pass will access the texture. Default value is set to AccessFlag.Read. Writing is currently not supported on any platform. </param>
        void SetInputAttachment(TextureHandle tex, int index, AccessFlags flags = AccessFlags.Read)
        {
            SetInputAttachment(tex, index, flags, 0, -1);
        }

        /// <summary>
        /// Use the texture as an input attachment.
        ///
        /// This informs the graph that any shaders in pass will only read from this texture at the current fragment position using the
        /// LOAD_FRAMEBUFFER_INPUT(idx)/LOAD_FRAMEBUFFER_INPUT_MS(idx,sampleIdx) macros. The index passed to LOAD_FRAMEBUFFER_INPUT needs
        /// to match the index passed to SetInputAttachment for this texture.
        ///
        /// </summary>
        /// <param name="tex">Texture to use during this pass.</param>
        /// <param name="index">Index the shader will use to access this texture.</param>
        /// <param name="flags">How this pass will access the texture. Writing is currently not supported on any platform. </param>
        /// <param name="mipLevel">Selects which mip map to used.</param>
        /// <param name="depthSlice">Used to index into a texture array. Use -1 to use bind all slices.</param>
        void SetInputAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice);

        /// <summary>
        /// Use the texture as a depth buffer for the Z-Buffer hardware.  Note you can only test-against and write-to a single depth texture in a pass.
        /// If you want to write depth to more than one texture you will need to register the second texture as SetRenderAttachment and manually calculate
        /// and write the depth value in the shader.
        /// Calling SetRenderAttachmentDepth twice on the same builder is an error.
        /// Write:
        /// Indicate a texture will be written with the current fragment depth by the ROPs (but not for depth reading (i.e. z-test == always)).
        /// Read:
        /// Indicate a texture will be used as an input for the depth testing unit.
        /// </summary>
        /// <param name="tex">Texture to use during this pass.</param>
        /// <param name="flags">How this pass will access the texture. Default value is set to AccessFlag.Write </param>
        void SetRenderAttachmentDepth(TextureHandle tex, AccessFlags flags = AccessFlags.Write)
        {
            SetRenderAttachmentDepth(tex, flags, 0, -1);
        }

        /// <summary>
        /// Use the texture as a depth buffer for the Z-Buffer hardware.  Note you can only test-against and write-to a single depth texture in a pass.
        /// If you want to write depth to more than one texture you will need to register the second texture as SetRenderAttachment and manually calculate
        /// and write the depth value in the shader.
        /// Calling SetRenderAttachmentDepth twice on the same builder is an error.
        /// Write:
        /// Indicate a texture will be written with the current fragment depth by the ROPs (but not for depth reading (i.e. z-test == always)).
        /// Read:
        /// Indicate a texture will be used as an input for the depth testing unit.
        /// </summary>
        /// <param name="tex">Texture to use during this pass.</param>
        /// <param name="flags">How this pass will access the texture.</param>
        /// <param name="mipLevel">Selects which mip map to used.</param>
        /// <param name="depthSlice">Used to index into a texture array. Use -1 to use bind all slices.</param>
        void SetRenderAttachmentDepth(TextureHandle tex, AccessFlags flags, int mipLevel, int depthSlice);

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
        /// <param name="flags">How this pass will access the texture. Default value is set to AccessFlag.ReadWrite.</param>
        /// <returns>The value passed to 'input'. You should not use the returned value it will be removed in the future.</returns>
        TextureHandle SetRandomAccessAttachment(TextureHandle tex, int index, AccessFlags flags = AccessFlags.ReadWrite);

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
        /// <param name="flags">How this pass will access the buffer. Default value is set to AccessFlag.Read.</param>
        /// <returns>The value passed to 'input'. You should not use the returned value it will be removed in the future.</returns>
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
        /// <param name="flags">How this pass will access the buffer. Default value is set to AccessFlag.Read.</param>
        /// <returns>The value passed to 'input'. You should not use the returned value it will be removed in the future.</returns>
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
