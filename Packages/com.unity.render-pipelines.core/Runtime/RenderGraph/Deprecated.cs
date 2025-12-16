using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting.APIUpdating;
// Typedef for the in-engine RendererList API (to avoid conflicts with the experimental version)
using CoreRendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// This class specifies the context given to every render pass. This context type passes a generic
    /// command buffer that can be used to schedule all commands. This will eventually be deprecated
    /// in favor of more specific contexts that have more specific command buffer types.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    [Obsolete("RenderGraphContext is deprecated, use RasterGraphContext/ComputeGraphContext/UnsafeGraphContext instead.", true)]
    public struct RenderGraphContext : IDerivedRendergraphContext
    {
        private InternalRenderGraphContext wrappedContext;

        /// <inheritdoc />
        public void FromInternalContext(InternalRenderGraphContext context)
        {
            wrappedContext = context;
        }

        /// <inheritdoc />
        public readonly TextureUVOrigin GetTextureUVOrigin(in TextureHandle textureHandle) { return TextureUVOrigin.BottomLeft;  }

        ///<summary>Scriptable Render Context used for rendering.</summary>
        public ScriptableRenderContext renderContext { get => wrappedContext.renderContext; }
        ///<summary>Command Buffer used for rendering.</summary>
        public CommandBuffer cmd { get => wrappedContext.cmd; }
        ///<summary>Render Graph pool used for temporary data.</summary>
        public RenderGraphObjectPool renderGraphPool { get => wrappedContext.renderGraphPool; }
        ///<summary>Render Graph default resources.</summary>
        public RenderGraphDefaultResources defaultResources { get => wrappedContext.defaultResources; }
    }

    public partial class RenderGraph
    {
        /// <summary>
        /// Import the final backbuffer to render graph.
        /// This function can only be used when nativeRenderPassesEnabled is false.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="rt">Backbuffer render target identifier.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        [Obsolete("This overload is deprecated, use ImportBackbuffer(RenderTargetIdentifier, RenderTargetInfo, ImportResourceParams) instead.", true)]
        public TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
        {
            return new TextureHandle();
        }

        /// <summary>
        /// Create a new Render Graph Shared Texture resource.
        /// This texture will be persistent across render graph executions.
        /// </summary>
        /// <remarks>
        /// This API should not be used with URP NRP Render Graph. This API cannot be called when Render Graph is active, please call it outside of RecordRenderGraph().
        /// </remarks>
        /// <param name="desc">Creation descriptor of the texture.</param>
        /// <param name="explicitRelease">Set to true if you want to manage the lifetime of the resource yourself. Otherwise the resource will be released automatically if unused for a time.</param>
        /// <returns>A new TextureHandle.</returns>
        [Obsolete("CreateSharedTexture() and shared texture workflow are deprecated, use ImportTexture() workflow instead.", true)]
        public TextureHandle CreateSharedTexture(in TextureDesc desc, bool explicitRelease = false)
        {
            return new TextureHandle();
        }

        /// <summary>
        /// Refresh a shared texture with a new descriptor.
        /// </summary>
        /// <remarks>
        /// This API should not be used with URP NRP Render Graph.
        /// </remarks>
        /// <param name="handle">Shared texture that needs to be updated.</param>
        /// <param name="desc">New Descriptor for the texture.</param>
        [Obsolete("RefreshSharedTextureDesc() and shared texture workflow are deprecated, use ImportTexture() workflow instead.", true)]
        public void RefreshSharedTextureDesc(TextureHandle handle, in TextureDesc desc) {}

        /// <summary>
        /// Release a Render Graph shared texture resource.
        /// </summary>
        /// <remarks>
        /// This API should not be used with URP NRP Render Graph. This API cannot be called when Render Graph is active, please call it outside of RecordRenderGraph().
        /// </remarks>
        /// <param name="texture">The handle to the texture that needs to be release.</param>
        [Obsolete("ReleaseSharedTexture() and shared texture workflow are deprecated, use ImportTexture() workflow instead.", true)]
        public void ReleaseSharedTexture(TextureHandle texture) {}

        /// <summary>
        /// Import an external Graphics Buffer to the Render Graph.
        /// Any pass writing to an imported graphics buffer will be considered having side effects and can't be automatically culled.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="graphicsBuffer">External Graphics Buffer that needs to be imported.</param>
        /// <param name="forceRelease">The imported graphics buffer will be released after usage.</param>
        /// <returns>A new GraphicsBufferHandle.</returns>
        [Obsolete("ImportBuffer with forceRelease parameter is deprecated. Use ImportBuffer without it instead. #from(6000.3)", true)]
        public BufferHandle ImportBuffer(GraphicsBuffer graphicsBuffer, bool forceRelease = false)
        {
            return new BufferHandle();
        }

        /// <summary>
        /// Add a new Render Pass to the Render Graph.
        /// </summary>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="sampler">Profiling sampler used around the pass.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a RenderGraphBuilder used to setup the new Render Pass.</returns>
        [Obsolete("AddRenderPass() is deprecated, use AddRasterRenderPass/AddComputePass/AddUnsafePass() instead.", true)]
        public RenderGraphBuilder AddRenderPass<PassData>(string passName, out PassData passData, ProfilingSampler sampler
#if !CORE_PACKAGE_DOCTOOLS
            ,[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            passData = null;
            return new RenderGraphBuilder(null, m_Resources, this);
        }

        /// <summary>
        /// Add a new Render Pass to the Render Graph.
        /// </summary>
        /// <remarks>
        /// This API should not be used with URP NRP Render Graph. In addition, it cannot be called when Render Graph records a pass, please call it within SetRenderFunc() or outside of AddUnsafePass()/AddComputePass()/AddRasterRenderPass().
        /// </remarks>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a RenderGraphBuilder used to setup the new Render Pass.</returns>
        [Obsolete("AddRenderPass() is deprecated, use AddRasterRenderPass/AddComputePass/AddUnsafePass() instead.", true)]
        public RenderGraphBuilder AddRenderPass<PassData>(string passName, out PassData passData
#if !CORE_PACKAGE_DOCTOOLS
            ,[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            return AddRenderPass(passName, out passData, GetDefaultProfilingSampler(passName), file, line);
        }
    }

    /// <summary>
    /// Use this struct to set up a new Render Pass.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    [Obsolete("RenderGraphBuilder is deprecated, use IComputeRenderGraphBuilder/IRasterRenderGraphBuilder/IUnsafeRenderGraphBuilder instead.", true)]
    public struct RenderGraphBuilder : IDisposable
    {
        RenderGraphPass m_RenderPass;
        RenderGraphResourceRegistry m_Resources;
        RenderGraph m_RenderGraph;
        bool m_Disposed;

        #region Public Interface
        /// <summary>
        /// Specify that the pass will use a Texture resource as a color render target.
        /// This has the same effect as WriteTexture and also automatically sets the Texture to use as a render target.
        /// </summary>
        /// <param name="input">The Texture resource to use as a color render target.</param>
        /// <param name="index">Index for multiple render target usage.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle UseColorBuffer(in TextureHandle input, int index)
        {
            CheckResource(input.handle, false);
            m_Resources.IncrementWriteCount(input.handle);
            m_RenderPass.SetColorBuffer(input, index);
            return input;
        }

        /// <summary>
        /// Specify that the pass will use a Texture resource as a depth buffer.
        /// </summary>
        /// <param name="input">The Texture resource to use as a depth buffer during the pass.</param>
        /// <param name="flags">Specify the access level for the depth buffer. This allows you to say whether you will read from or write to the depth buffer, or do both.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle UseDepthBuffer(in TextureHandle input, DepthAccess flags)
        {
            CheckResource(input.handle, false);

            if ((flags & DepthAccess.Write) != 0)
                m_Resources.IncrementWriteCount(input.handle);
            if ((flags & DepthAccess.Read) != 0)
            {
                if (!m_Resources.IsRenderGraphResourceImported(input.handle) && m_Resources.TextureNeedsFallback(input))
                    WriteTexture(input);
            }

            m_RenderPass.SetDepthBuffer(input, flags);
            return input;
        }

        /// <summary>
        /// Specify a Texture resource to read from during the pass.
        /// </summary>
        /// <param name="input">The Texture resource to read from during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle ReadTexture(in TextureHandle input)
        {
            CheckResource(input.handle);

            if (!m_Resources.IsRenderGraphResourceImported(input.handle) && m_Resources.TextureNeedsFallback(input))
            {
                var textureResource = m_Resources.GetTextureResource(input.handle);

                // Ensure we get a fallback to black
                textureResource.desc.clearBuffer = true;
                textureResource.desc.clearColor = Color.black;

                // If texture is read from but never written to, return a fallback black texture to have valid reads
                // Return one from the preallocated default textures if possible
                if (m_RenderGraph.GetImportedFallback(textureResource.desc, out var fallback))
                    return fallback;

                // If not, simulate a write to the texture so that it gets allocated
                WriteTexture(input);
            }

            m_RenderPass.AddResourceRead(input.handle);
            return input;
        }

        /// <summary>
        /// Specify a Texture resource to write to during the pass.
        /// </summary>
        /// <param name="input">The Texture resource to write to during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle WriteTexture(in TextureHandle input)
        {
            CheckResource(input.handle);
            m_Resources.IncrementWriteCount(input.handle);
            m_RenderPass.AddResourceWrite(input.handle);
            return input;
        }

        /// <summary>
        /// Specify a Texture resource to read and write to during the pass.
        /// </summary>
        /// <param name="input">The Texture resource to read and write to during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public TextureHandle ReadWriteTexture(in TextureHandle input)
        {
            CheckResource(input.handle);
            m_Resources.IncrementWriteCount(input.handle);
            m_RenderPass.AddResourceWrite(input.handle);
            m_RenderPass.AddResourceRead(input.handle);
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
            var result = m_Resources.CreateTexture(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(result.handle);
            return result;
        }

        /// <summary>
        /// Create a new Render Graph Texture resource using the descriptor from another texture.
        /// This texture will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <returns>A new transient TextureHandle.</returns>
        public TextureHandle CreateTransientTexture(in TextureHandle texture)
        {
            ref readonly var desc = ref m_Resources.GetTextureResourceDesc(texture.handle);
            var result = m_Resources.CreateTexture(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(result.handle);
            return result;
        }

        /// <summary>
        /// Specify a RayTracingAccelerationStructure resource to build during the pass.
        /// </summary>
        /// <param name="input">The RayTracingAccelerationStructure resource to build during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public RayTracingAccelerationStructureHandle WriteRayTracingAccelerationStructure(in RayTracingAccelerationStructureHandle input)
        {
            CheckResource(input.handle);
            m_Resources.IncrementWriteCount(input.handle);
            m_RenderPass.AddResourceWrite(input.handle);
            return input;
        }

        /// <summary>
        /// Specify a RayTracingAccelerationStructure resource to use during the pass.
        /// </summary>
        /// <param name="input">The RayTracingAccelerationStructure resource to use during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public RayTracingAccelerationStructureHandle ReadRayTracingAccelerationStructure(in RayTracingAccelerationStructureHandle input)
        {
            CheckResource(input.handle);
            m_RenderPass.AddResourceRead(input.handle);
            return input;
        }

        /// <summary>
        /// Specify a Renderer List resource to use during the pass.
        /// </summary>
        /// <param name="input">The Renderer List resource to use during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public RendererListHandle UseRendererList(in RendererListHandle input)
        {
            if(input.IsValid())
                m_RenderPass.UseRendererList(input);
            return input;
        }

        /// <summary>
        /// Specify a Graphics Buffer resource to read from during the pass.
        /// </summary>
        /// <param name="input">The Graphics Buffer resource to read from during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public BufferHandle ReadBuffer(in BufferHandle input)
        {
            CheckResource(input.handle);
            m_RenderPass.AddResourceRead(input.handle);
            return input;
        }

        /// <summary>
        /// Specify a Graphics Buffer resource to write to during the pass.
        /// </summary>
        /// <param name="input">The Graphics Buffer resource to write to during the pass.</param>
        /// <returns>An updated resource handle to the input resource.</returns>
        public BufferHandle WriteBuffer(in BufferHandle input)
        {
            CheckResource(input.handle);
            m_RenderPass.AddResourceWrite(input.handle);
            m_Resources.IncrementWriteCount(input.handle);
            return input;
        }

        /// <summary>
        /// Create a new Render Graph Graphics Buffer resource.
        /// This Graphics Buffer will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="desc">Graphics Buffer descriptor.</param>
        /// <returns>A new transient GraphicsBufferHandle.</returns>
        public BufferHandle CreateTransientBuffer(in BufferDesc desc)
        {
            var result = m_Resources.CreateBuffer(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(result.handle);
            return result;
        }

        /// <summary>
        /// Create a new Render Graph Graphics Buffer resource using the descriptor from another Graphics Buffer.
        /// This Graphics Buffer will only be available for the current pass and will be assumed to be both written and read so users don't need to add explicit read/write declarations.
        /// </summary>
        /// <param name="graphicsbuffer">Graphics Buffer from which the descriptor should be used.</param>
        /// <returns>A new transient GraphicsBufferHandle.</returns>
        public BufferHandle CreateTransientBuffer(in BufferHandle graphicsbuffer)
        {
            ref readonly var desc = ref m_Resources.GetBufferResourceDesc(graphicsbuffer.handle);
            var result = m_Resources.CreateBuffer(desc, m_RenderPass.index);
            m_RenderPass.AddTransientResource(result.handle);
            return result;
        }

        /// <summary>
        /// Specify the render function to use for this pass.
        /// A call to this is mandatory for the pass to be valid.
        /// </summary>
        /// <typeparam name="PassData">The Type of the class that provides data to the Render Pass.</typeparam>
        /// <param name="renderFunc">Render function for the pass.</param>
        public void SetRenderFunc<PassData>(BaseRenderFunc<PassData, RenderGraphContext> renderFunc)
            where PassData : class, new()
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
        /// Allow or not pass culling
        /// By default all passes can be culled out if the render graph detects it's not actually used.
        /// In some cases, a pass may not write or read any texture but rather do something with side effects (like setting a global texture parameter for example).
        /// This function can be used to tell the system that it should not cull this pass.
        /// </summary>
        /// <param name="value">True to allow pass culling.</param>
        public void AllowPassCulling(bool value)
        {
            m_RenderPass.AllowPassCulling(value);
        }

        /// <summary>
        /// Enable foveated rendering for this pass.
        /// </summary>
        /// <param name="value">True to enable foveated rendering.</param>
        public void EnableFoveatedRasterization(bool value)
        {
            m_RenderPass.EnableFoveatedRasterization(value);
        }

        /// <summary>
        /// Dispose the RenderGraphBuilder instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Allow or not pass culling based on renderer list results
        /// By default all passes can be culled out if the render graph detects they are using a renderer list that is empty (does not draw any geometry)
        /// In some cases, a pass may not write or read any texture but rather do something with side effects (like setting a global texture parameter for example).
        /// This function can be used to tell the system that it should not cull this pass.
        /// </summary>
        /// <param name="value">True to allow pass culling.</param>
        public void AllowRendererListCulling(bool value)
        {
            m_RenderPass.AllowRendererListCulling(value);
        }

        /// <summary>
        /// Used to indicate that a pass depends on an external renderer list (that is not directly used in this pass).
        /// </summary>
        /// <param name="input">The renderer list handle this pass depends on.</param>
        /// <returns>A <see cref="RendererListHandle"/></returns>
        public RendererListHandle DependsOn(in RendererListHandle input)
        {
            m_RenderPass.UseRendererList(input);
            return input;
        }

        #endregion

        #region Internal Interface
        internal RenderGraphBuilder(RenderGraphPass renderPass, RenderGraphResourceRegistry resources, RenderGraph renderGraph)
        {
            m_RenderPass = renderPass;
            m_Resources = resources;
            m_RenderGraph = renderGraph;
            m_Disposed = false;
        }

        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            m_RenderGraph.RenderGraphState = RenderGraphState.RecordingGraph;
            m_Disposed = true;
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckResource(in ResourceHandle res, bool checkTransientReadWrite = true)
        {
            if(RenderGraph.enableValidityChecks)
            {
                if (res.IsValid())
                {
                    int transientIndex = m_Resources.GetRenderGraphResourceTransientIndex(res);
                    // We have dontCheckTransientReadWrite here because users may want to use UseColorBuffer/UseDepthBuffer API to benefit from render target auto binding. In this case we don't want to raise the error.
                    if (transientIndex == m_RenderPass.index && checkTransientReadWrite)
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
            }
        }

        internal void GenerateDebugData(bool value)
        {
            m_RenderPass.GenerateDebugData(value);
        }

        #endregion
    }
}
