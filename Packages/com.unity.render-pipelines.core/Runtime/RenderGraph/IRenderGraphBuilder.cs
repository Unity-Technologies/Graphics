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

        /// <summary>
        /// Generates debugging data for this pass, intended for visualization in the RenderGraph Viewer.
        /// </summary>
        /// <param name="value">True to enable debug data generation for this pass.</param>
        public void GenerateDebugData(bool value);
    }

    /// <summary>
    /// An intermediary interface for builders that can set render attachments and random access attachments (UAV).
    /// </summary>
    public interface IRenderAttachmentRenderGraphBuilder : IBaseRenderGraphBuilder
    {
        /// <summary>
        /// Binds the texture as a color render target (MRT attachment) for this pass.
        /// </summary>
        /// <param name="tex">The texture to bind as a render target for this pass.</param>
        /// <param name="index">The MRT slot the shader writes to. This maps to `SV_Target` in the shader, for example, a value of `1` maps to `SV_Target1`.</param>
        /// <param name="flags">The access mode for the texture. The default value is `AccessFlags.Write`.</param>
        /// <remarks>
        /// Potential access flags:
        /// - Write: This pass outputs to the texture using render target rasterization. The render graph binds
        ///   the texture to the specified MRT slot (index). In HLSL, write to the slot using
        ///   `float4 outColor : SV_Target{index} = value;`.
        ///   To use random-access writes, use `SetRandomAccessAttachment` (UAV).
        ///   Don't write to this target with UAV-style indexed access (`operator[]`). 
        /// - Read: This pass might read the current contents of the render target implicitly, depending
        ///   on rasterization state (for example, blending operations that read before writing).
        ///   
        /// The render graph can't determine how much of the target you overwrite. By default, it
        /// assumes partial updates and preserves existing content. If you fully overwrite the target
        /// (for example, in a fullscreen pass), use `AccessFlags.WriteAll` for better performance.
        /// </remarks>
        void SetRenderAttachment(TextureHandle tex, int index, AccessFlags flags = AccessFlags.Write)
        {
            SetRenderAttachment(tex, index, flags, 0, -1);
        }

        /// <summary>
        /// Binds the texture as a color render target (MRT attachment) for this pass.
        /// </summary>
        /// <param name="tex">The texture to bind as a render target for this pass.</param>
        /// <param name="index">The MRT slot the shader writes to (corresponds to `SV_Target{index}`).</param>
        /// <param name="flags">How this pass accesses the texture. Defaults to `AccessFlags.Write`.</param>
        /// <param name="mipLevel">The mip level to bind.</param>
        /// <param name="depthSlice">The array slice to bind. Use -1 to bind all slices.</param>
        /// <remarks>
        /// Potential access flags:
        /// - Write: This pass outputs to the texture using render target rasterization. The render graph binds
        ///   the texture to the specified MRT slot (index). In HLSL, write to the slot using
        ///   `float4 outColor : SV_Target{index} = value;`.
        ///   To use random-access writes, use `SetRandomAccessAttachment` (UAV).
        ///   Don't write to this target with UAV-style indexed access (`operator[]`). 
        /// - Read: This pass might read the current contents of the render target implicitly, depending
        ///   on rasterization state (for example, blending operations that read before writing).
        ///   
        /// The render graph can't determine how much of the target you overwrite. By default, it
        /// assumes partial updates and preserves existing content. If you fully overwrite the target
        /// (for example, in a fullscreen pass), use `AccessFlags.WriteAll` for better performance.
        ///
        /// Using the same texture handle with different depth slices at different render target indices is not supported.
        /// </remarks>
        void SetRenderAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice);

        /// <summary>
        /// Binds the texture as the depth buffer for this pass.
        /// </summary>
        /// <param name="tex">The texture to use as the depth buffer for this pass.</param>
        /// <param name="flags">Access mode for the texture in this pass. Defaults to `AccessFlag.ReadWrite`.
        /// </param>
        /// <remarks>
        /// Potential access flags:
        /// - Write: The pass writes fragment depth to the bound depth buffer (required if `ZWrite` is enabled in shader code).
        /// - Read: The pass reads from the bound depth buffer for depth testing (required if `ZTest` is set to an operation other than `Disabled`, `Never`, or `Always` in shader code).
        /// 
        /// Only one depth buffer can be tested against or written to in a single pass.
        /// To output depth to multiple textures, register the additional texture as a color
        /// attachment using `SetRenderAttachment()`, then compute and write the depth value in the shader.
        /// If you call `SetRenderAttachmentDepth()` more than once on the same builder, it results in an error.
        /// </remarks>
        void SetRenderAttachmentDepth(TextureHandle tex, AccessFlags flags = AccessFlags.ReadWrite)
        {
            SetRenderAttachmentDepth(tex, flags, 0, -1);
        }

        /// <summary>
        /// Binds the texture as the depth buffer for this pass.
        /// </summary>
        /// <param name="tex">The texture to use as the depth buffer for this pass.</param>
        /// <param name="flags">How this pass will access the depth texture (for example, `AccessFlags.Read`, `AccessFlags.Write`, `AccessFlags.ReadWrite`).</param>
        /// <param name="mipLevel">The mip level to bind.</param>
        /// <param name="depthSlice">The array slice to bind. Use -1 to bind all slices.</param>
        /// <remarks>
        /// Potential access flags:
        /// - Write: The pass writes fragment depth to the bound depth buffer (required if `ZWrite` is enabled in shader code).
	/// - Read: The pass reads from the bound depth buffer for depth testing (required if `ZTest` is set to an operation other than `Disabled`, `Never`, or `Always` in shader code).
        /// 
        /// Only one depth texture can be read from or written to in a single pass.
        /// To output depth to multiple textures, register the additional texture as a color
        /// attachment using `SetRenderAttachment()`, then compute and write the depth value in the shader.
        /// If you call `SetRenderAttachmentDepth()` more than once on the same builder, it results in an error.
        /// 
        /// Using the same texture handle with different depth slices at different render target indices is not supported.
        /// </remarks>
        void SetRenderAttachmentDepth(TextureHandle tex, AccessFlags flags, int mipLevel, int depthSlice);

        /// <summary>
        /// Binds the texture as a random-access attachment for this pass
        /// (DX12: Unordered Access View, Vulkan: Storage Image).
        /// </summary>
        /// <param name="tex">The texture to expose as a UAV in this pass.</param>
        /// <param name="index">The binding slot the shader uses to access this UAV (HLSL: `register(u[index])`).</param>
        /// <param name="flags">Access mode for this texture in the pass. Defaults to `AccessFlags.ReadWrite`.</param>
        /// <returns>
        /// This declares that shaders in the pass will access the texture via
        /// `RWTexture2D`, `RWTexture3D`, etc., enabling read/write operations,
        /// atomics, and other UAV-style operations using standard HLSL.
        ///
        /// The value passed to the `tex` parameter. The return value is deprecated.
        /// </returns>
        /// <remarks>
        /// Random-access (UAV) textures share index-based binding slots with
        /// render targets and input attachments. Refer to `CommandBuffer.SetRandomWriteTarget`
        /// for platform-specific details and constraints.
        /// </remarks>
        TextureHandle SetRandomAccessAttachment(TextureHandle tex, int index, AccessFlags flags = AccessFlags.ReadWrite);

        /// <summary>
        /// Binds the buffer as a random-access attachment for this pass
        /// (DX12: Unordered Access View, Vulkan: Storage Buffer).
        /// </summary>
        /// <param name="tex">The buffer to expose as a UAV in this pass.</param>
        /// <param name="index">The binding slot the shader uses to access this UAV (HLSL: `register(u[index])`).</param>
        /// <param name="flags">Access mode for this buffer in the pass. Defaults to `AccessFlags.Read`.</param>
        /// <returns>
        /// The value passed to the `buffer` parameter. The return value is deprecated.
        /// </returns>
        /// <remarks>
        /// This declares that shaders in the pass will access the buffer via
        /// `RWStructuredBuffer`, `RWByteAddressBuffer`, etc., enabling read/write,
        /// atomics, and other UAV-style operations using standard HLSL.
        /// 
        /// Random-access (UAV) buffers share index-based binding slots with
        /// render targets and input attachments. Refer to `CommandBuffer.SetRandomWriteTarget`
        /// for platform-specific details and constraints.
        /// </remarks>
        BufferHandle UseBufferRandomAccess(BufferHandle tex, int index, AccessFlags flags = AccessFlags.Read);

        /// <summary>
        /// Binds the buffer as a random-access attachment for this pass
        /// (DX12: Unordered Access View, Vulkan: Storage Buffer).
        /// </summary>
        /// <param name="tex">The buffer to expose as a UAV in this pass.</param>
        /// <param name="index">The binding slot the shader uses to access this UAV (HLSL: `register(u[index])`).</param>
        /// <param name="preserveCounterValue">Whether to keep the current append/consume counter unchanged for this UAV buffer. Defaults to preserving the existing counter value.</param>
        /// <param name="flags">Access mode for this buffer in the pass. Defaults to `AccessFlags.Read`.</param>
        /// <returns>
        /// The value passed to the `buffer` parameter. The return value is deprecated.
        /// </returns>
        /// <remarks>
        /// This declares that shaders in the pass will access the buffer via
        /// `RWStructuredBuffer`, `RWByteAddressBuffer`, etc., enabling read/write,
        /// atomics, and other UAV-style operations using standard HLSL.
        /// 
        /// Random-access (UAV) buffers share index-based binding slots with
        /// render targets and input attachments. Refer to `CommandBuffer.SetRandomWriteTarget`
        /// for platform-specific details and constraints.
        /// </remarks>
        BufferHandle UseBufferRandomAccess(BufferHandle tex, int index, bool preserveCounterValue, AccessFlags flags = AccessFlags.Read);
    }

    /// <summary>
    /// A builder for a compute render pass.
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
    public interface IUnsafeRenderGraphBuilder : IRenderAttachmentRenderGraphBuilder
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
    /// A builder for a raster render pass.
    /// <see cref="RenderGraph.AddRasterRenderPass"/>
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public interface IRasterRenderGraphBuilder : IRenderAttachmentRenderGraphBuilder
    {
        /// <summary>
        /// Binds the texture as an input attachment for this pass.
        /// 
        /// Shaders might read this texture at the current fragment using
        /// `LOAD_FRAMEBUFFER_INPUT(idx)` or `LOAD_FRAMEBUFFER_INPUT_MS(idx, sampleIdx)`.
        /// The `idx` used in the shader must match the `index` provided to `SetInputAttachment`.
        /// </summary>
        /// <remarks>
        /// Platform support varies. Input attachments, especially with MSAA, might be unsupported on
        /// some targets. Use `RenderGraphUtils.IsFramebufferFetchSupportedOnCurrentPlatform` at
        /// runtime to check compatibility.
        /// </remarks>
        /// <param name="tex">The texture to expose as an input attachment.</param>
        /// <param name="index">The binding index used by the shader macros (`idx`).</param>
        /// <param name="flags">The access mode for this texture. Defaults to `AccessFlags.Read`. Writing is currently not supported on any platform.</param>
        void SetInputAttachment(TextureHandle tex, int index, AccessFlags flags = AccessFlags.Read)
        {
            SetInputAttachment(tex, index, flags, 0, -1);
        }

        /// <summary>
        /// Binds the texture as an input attachment for this pass.
        /// 
        /// Shaders might read this texture at the current fragment using
        /// `LOAD_FRAMEBUFFER_INPUT(idx)` or `LOAD_FRAMEBUFFER_INPUT_MS(idx, sampleIdx)`.
        /// The `idx` used in the shader must match the `index` provided in `SetInputAttachment`.
        /// </summary>
        /// <remarks>
        /// Platform support varies. Input attachments, especially with MSAA, might be unsupported on
        /// some targets. Use `RenderGraphUtils.IsFramebufferFetchSupportedOnCurrentPlatform` at
        /// runtime to check compatibility.
        /// </remarks>
        /// <param name="tex">The texture to expose as an input attachment.</param>
        /// <param name="index">The binding index used by the shader macros (`idx`).</param>
        /// <param name="flags">The access mode for the texture. Defaults to `AccessFlags.Read`. Writing is currently not supported on any platform.</param>
        /// <param name="mipLevel">The mip level to bind.</param>
        /// <param name="depthSlice">The array slice to bind. Use -1 to bind all slices.</param>
        void SetInputAttachment(TextureHandle tex, int index, AccessFlags flags, int mipLevel, int depthSlice);

        /// <summary>
        /// Enables Variable Rate Shading (VRS) on the current rasterization pass. Rasterization will use the texture to determine the rate of fragment shader invocation.
        /// </summary>
        /// <param name="tex">Shading rate image (SRI) Texture to use during this pass.</param>
        void SetShadingRateImageAttachment(in TextureHandle tex);

        /// <summary>
        /// Set shading rate fragment size.
        /// </summary>
        /// <param name="shadingRateFragmentSize">Shading rate fragment size to set.</param>
        void SetShadingRateFragmentSize(ShadingRateFragmentSize shadingRateFragmentSize);

        /// <summary>
        /// Set shading rate combiner.
        /// </summary>
        /// <param name="stage">Shading rate combiner stage to apply combiner to.</param>
        /// <param name="combiner">Shading rate combiner to set.</param>
        void SetShadingRateCombiner(ShadingRateCombinerStage stage, ShadingRateCombiner combiner);

        /// <summary>
        /// Enables the configuration of extended pass properties that may allow platform-specific optimizations.
        /// </summary>
        /// <param name="extendedFeatureFlags">Specifies additional pass properties that may enable optimizations on certain platforms.</param>
        public void SetExtendedFeatureFlags(ExtendedFeatureFlags extendedFeatureFlags);

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
