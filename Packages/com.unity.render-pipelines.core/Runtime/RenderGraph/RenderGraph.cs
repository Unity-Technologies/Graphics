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
    /// Sets the read and write access for the depth buffer.
    /// </summary>
    [Flags][MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public enum DepthAccess
    {
        ///<summary>Read Access.</summary>
        Read = 1 << 0,
        ///<summary>Write Access.</summary>
        Write = 1 << 1,
        ///<summary>Read and Write Access.</summary>
        ReadWrite = Read | Write,
    }

    /// <summary>
    /// Express the operations the rendergraph pass will do on a resource.
    /// </summary>
    [Flags][MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
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

        ///<summary> Shortcut for Read | Write</summary>
        ReadWrite = Read | Write
    }

    /// <summary>
    /// Expresses additional pass properties that can be used to perform optimizations on some platforms.
    /// </summary>
    [Flags]
    public enum ExtendedFeatureFlags
    {
        ///<summary>Default state with no extended features enabled.</summary>
        None = 0,
        ///<summary>On Meta XR, this flag can be set for the pass that performs the most 3D rendering to achieve better performance.</summary>
        TileProperties = 1 << 0,
        ///<summary>On XR, this flag can be set for passes that are compatible with Multiview Render Regions</summary>
        MultiviewRenderRegionsCompatible = 1 << 1,
        ///<summary>On Meta XR, this flag can be set to use MSAA shader resolve in the last subpass of a render pass. </summary>
        MultisampledShaderResolve = 1 << 2,
    }

    [Flags]
    internal enum RenderGraphState
    {
        /// <summary>
        /// Render Graph is not doing anything.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// Render Graph is recording the graph.
        /// </summary>
        RecordingGraph = 1 << 0,

        /// <summary>
        /// Render Graph is recording a low level pass.
        /// </summary>
        RecordingPass = 1 << 1,

        /// <summary>
        /// Render Graph is executing the graph.
        /// </summary>
        Executing = 1 << 2,

        /// <summary>
        /// Utility flag to check if the graph is active.
        /// </summary>
        Active = RecordingGraph | RecordingPass | Executing
    }

    /// <summary>
    /// The strategy that the render pipeline should use to determine the UV origin of RenderTextures who have an Unknown TextureUVOrigin when rendering.
    /// </summary>
    public enum RenderTextureUVOriginStrategy
    {
        /// <summary>RenderTextures are always treated as bottom left orientation.</summary>
        BottomLeft,
        /// <summary>RenderTextures may inherit the backbuffer attachment orientation if they are only used via attachment reads.</summary>
        PropagateAttachmentOrientation
    }

    /// <summary>
    /// An object representing the internal context of a rendergraph pass execution.
    /// This object is public for technical reasons only and should not be used.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public class InternalRenderGraphContext
    {
        internal ScriptableRenderContext renderContext;
        internal CommandBuffer cmd;
        internal RenderGraphObjectPool renderGraphPool;
        internal RenderGraphDefaultResources defaultResources;
        internal RenderGraphPass executingPass;
        internal NativeRenderPassCompiler.CompilerContextData compilerContext;
        internal bool contextlessTesting;
        internal bool forceResourceCreation;
    }

    // InternalRenderGraphContext is public (but all members are internal)
    // only because the C# standard says that all interface member function implementations must be public.
    // So below in for example the RasterGraphContext we can't implement the (internal) interface as
    // internal void FromInternalContext(InternalRenderGraphContext context) { ... }
    // So we have to make FromInternalContext public so InternalRenderGraphContext also becomes public.
    // This seems an oversight in c# where Interfaces used as Generic constraints could very well be useful
    // with internal only functions.

    /// <summary>
    /// Interface implemented by the different render graph contexts provided at execution timeline (via SetRenderFunc())
    /// </summary>
    internal interface IDerivedRendergraphContext
    {
        /// <summary>
        /// This function is only public for techical resons of the c# language and should not be called outside the package.
        /// </summary>
        /// <param name="context">The context to convert</param>
        public void FromInternalContext(InternalRenderGraphContext context);

        /// <summary>
        /// Retrieves the TextureUVOrigin of the Render Graph texture from its handle.
        /// </summary>
        /// <remarks>
        /// This function can only be called when using the Native Render Pass Compiler (enabled by default).
        /// </remarks>
        /// <param name="textureHandle">The texture handle to query.</param>
        /// <returns>The TextureUVOrigin of the texture.</returns>
        public TextureUVOrigin GetTextureUVOrigin(in TextureHandle textureHandle);
    }

    /// <summary>
    /// This class declares the context object passed to the execute function of a raster render pass.
    /// <see cref="RenderGraph.AddRasterRenderPass"/>
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct RasterGraphContext : IDerivedRendergraphContext
    {
        private InternalRenderGraphContext wrappedContext;

        ///<summary>Command Buffer used for rendering.</summary>
        public RasterCommandBuffer cmd;

        ///<summary>Render Graph default resources.</summary>
        public RenderGraphDefaultResources defaultResources { get => wrappedContext.defaultResources; }

        ///<summary>Render Graph pool used for temporary data.</summary>
        public RenderGraphObjectPool renderGraphPool { get => wrappedContext.renderGraphPool; }

        static internal RasterCommandBuffer rastercmd = new RasterCommandBuffer(null, null, false);

        /// <inheritdoc />
        public void FromInternalContext(InternalRenderGraphContext context)
        {
            wrappedContext = context;
            rastercmd.m_WrappedCommandBuffer = wrappedContext.cmd;
            rastercmd.m_ExecutingPass = context.executingPass;
            cmd = rastercmd;
        }

        /// <inheritdoc />
        public readonly TextureUVOrigin GetTextureUVOrigin(in TextureHandle textureHandle)
        {
            if (!SystemInfo.graphicsUVStartsAtTop)
                return TextureUVOrigin.BottomLeft;

            if (wrappedContext.compilerContext != null)
            {
                return wrappedContext.compilerContext.GetTextureUVOrigin(textureHandle);
            }
            return TextureUVOrigin.BottomLeft;
        }
    }

    /// <summary>
    /// This class declares the context object passed to the execute function of a compute render pass.
    /// <see cref="RenderGraph.AddComputePass"/>
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public class ComputeGraphContext : IDerivedRendergraphContext
    {
        private InternalRenderGraphContext wrappedContext;

        ///<summary>Command Buffer used for rendering.</summary>
        public ComputeCommandBuffer cmd;

        ///<summary>Render Graph default resources.</summary>
        public RenderGraphDefaultResources defaultResources { get => wrappedContext.defaultResources; }

        ///<summary>Render Graph pool used for temporary data.</summary>
        public RenderGraphObjectPool renderGraphPool { get => wrappedContext.renderGraphPool; }

        static internal ComputeCommandBuffer computecmd = new ComputeCommandBuffer(null, null, false);

        /// <inheritdoc />
        public void FromInternalContext(InternalRenderGraphContext context)
        {
            wrappedContext = context;
            computecmd.m_WrappedCommandBuffer = wrappedContext.cmd;
            computecmd.m_ExecutingPass = context.executingPass;
            cmd = computecmd;
        }

        /// <inheritdoc />
        public TextureUVOrigin GetTextureUVOrigin(in TextureHandle textureHandle)
        {
            if (!SystemInfo.graphicsUVStartsAtTop)
                return TextureUVOrigin.BottomLeft;

            if (wrappedContext.compilerContext != null)
            {
                return wrappedContext.compilerContext.GetTextureUVOrigin(textureHandle);
            }
            return TextureUVOrigin.BottomLeft;
        }
    }

    /// <summary>
    /// This class declares the context object passed to the execute function of an unsafe render pass.
    /// <see cref="RenderGraph.AddUnsafePass"/>
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public class UnsafeGraphContext : IDerivedRendergraphContext
    {
        private InternalRenderGraphContext wrappedContext;

        ///<summary>Unsafe Command Buffer used for rendering.</summary>
        public UnsafeCommandBuffer cmd;

        ///<summary>Render Graph default resources.</summary>
        public RenderGraphDefaultResources defaultResources { get => wrappedContext.defaultResources; }

        ///<summary>Render Graph pool used for temporary data.</summary>
        public RenderGraphObjectPool renderGraphPool { get => wrappedContext.renderGraphPool; }

        internal static UnsafeCommandBuffer unsCmd = new UnsafeCommandBuffer(null, null, false);
        /// <inheritdoc />
        public void FromInternalContext(InternalRenderGraphContext context)
        {
            wrappedContext = context;
            unsCmd.m_WrappedCommandBuffer = wrappedContext.cmd;
            unsCmd.m_ExecutingPass = context.executingPass;
            cmd = unsCmd;
        }

        /// <inheritdoc />
        public TextureUVOrigin GetTextureUVOrigin(in TextureHandle textureHandle)
        {
            if (!SystemInfo.graphicsUVStartsAtTop)
                return TextureUVOrigin.BottomLeft;

            if (wrappedContext.compilerContext != null)
            {
                return wrappedContext.compilerContext.GetTextureUVOrigin(textureHandle);
            }
            return TextureUVOrigin.BottomLeft;
        }
    }

    /// <summary>
    /// This struct contains properties which control the execution of the Render Graph.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct RenderGraphParameters
    {
        ///<summary>Identifier for this render graph execution.</summary>
        [Obsolete("Not used anymore. The debugging tools use the name of the object identified by executionId. #from(6000.3)")]
        public string executionName;
        ///<summary>Identifier for this render graph execution (i.e. EntityId of the Camera rendering). Used for debugging tools.</summary>
        public EntityId executionId;
        ///<summary>Whether the execution should generate debug data and be visible in Render Graph Viewer.</summary>
        public bool generateDebugData;
        ///<summary>Index of the current frame being rendered.</summary>
        public int currentFrameIndex;
        ///<summary> Controls whether to enable Renderer List culling or not.</summary>
        [Obsolete("Not supported anymore. Syncing with culling system brings performance regressions in most cases. #from(6000.5)")]
        public bool rendererListCulling;
        ///<summary>Scriptable Render Context used by the render pipeline.</summary>
        public ScriptableRenderContext scriptableRenderContext;
        ///<summary>Command Buffer used to execute graphic commands.</summary>
        public CommandBuffer commandBuffer;
        ///<summary>When running tests indicate the context is intentionally invalid and all calls on it should just do nothing.
        ///This allows you to run tests that rely on code execution the way to the pass render functions
        ///This also changes some behaviours with exception handling and error logging so the test framework can act on exceptions to validate behaviour better.</summary>
        internal bool invalidContextForTesting;
        ///<summary>The strategy that the rendergraph should use to determine the texture uv origin if Unknown of RenderTextures when rendering.</summary>
        public RenderTextureUVOriginStrategy renderTextureUVOriginStrategy;
    }

    /// <summary>
    /// The Render Pass rendering delegate to use with typed contexts.
    /// </summary>
    /// <typeparam name="PassData">The type of the class used to provide data to the Render Pass.</typeparam>
    /// <typeparam name="ContextType">The type of the context that will be passed to the render function.</typeparam>
    /// <param name="data">Render Pass specific data.</param>
    /// <param name="renderGraphContext">Global Render Graph context.</param>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public delegate void BaseRenderFunc<PassData, ContextType>(PassData data, ContextType renderGraphContext) where PassData : class, new();

    /// <summary>
    /// This class is the main entry point of the Render Graph system.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public partial class RenderGraph
    {
        ///<summary>Maximum number of MRTs supported by Render Graph.</summary>
        public static readonly int kMaxMRTCount = 8;

        /// <summary>
        /// Enable the use of the render pass API instead of the traditional SetRenderTarget workflow for AddRasterRenderPass() API. Enabled by default since 6000.3. No alternative since 6000.5.
        /// </summary>
        /// <remarks>
        /// When enabled, the render graph try to use render passes and supasses instead of relying on SetRendertarget. It
        /// will try to aggressively optimize the number of BeginRenderPass+EndRenderPass calls as well as calls to NextSubPass.
        /// This with the aim to maximize the time spent "on chip" on tile based renderers.
        ///
        /// The Graph will automatically determine when to break render passes as well as the load and store actions to apply to these render passes.
        /// To do this, the graph will analyze the use of textures. E.g. when a texture is used twice in a row as a active render target, the two
        /// render graph passes will be merged in a single render pass with two surpasses. On the other hand if a render target is sampled as a texture in
        /// a later pass this render target will be stored (and possibly resolved) and the render pass will be broken up.
        ///
        /// When setting this setting to true some existing render graph API is no longer valid as it can't express detailed frame information needed to emit
        /// native render pases. In particular:
        /// - The ImportBackbuffer overload without a RenderTargetInfo argument.
        /// - Any AddRenderPass overloads. The more specific AddRasterRenderPass/AddComputePass/AddUnsafePass functions should be used to register passes.
        ///
        /// In addition to this, additional validation will be done on the correctness of arguments of existing API that was not previously done. This could lead
        /// to new errors when using existing render graph code with nativeRenderPassesEnabled.
        ///
        /// Note: that CommandBuffer.BeginRenderPass/EndRenderPass calls are different by design from SetRenderTarget so this could also have
        /// effects outside of render graph (e.g. for code relying on the currently active render target as this will not be updated when using render passes).
        /// </remarks>
        [Obsolete("RenderGraph always enables native render pass support. #from(6000.5)")]
        public bool nativeRenderPassesEnabled { get; set; } = true;

        internal/*for tests*/ RenderGraphResourceRegistry m_Resources;
        internal/*for tests*/ RenderGraphObjectPool m_RenderGraphPool = new RenderGraphObjectPool();
        RenderGraphBuilders m_builderInstance = new RenderGraphBuilders();
        internal/*for tests*/ List<RenderGraphPass> m_RenderPasses = new List<RenderGraphPass>(64);
        List<RendererListHandle> m_RendererLists = new List<RendererListHandle>(32);
        RenderGraphDebugParams m_DebugParameters = new RenderGraphDebugParams();
        RenderGraphDefaultResources m_DefaultResources = new RenderGraphDefaultResources();
        Dictionary<int, ProfilingSampler> m_DefaultProfilingSamplers = new Dictionary<int, ProfilingSampler>();
        InternalRenderGraphContext m_RenderGraphContext = new InternalRenderGraphContext();
        CommandBuffer m_PreviousCommandBuffer;
        RenderGraphCompilationCache m_CompilationCache;

        EntityId m_CurrentExecutionId;
        bool m_CurrentExecutionCanGenerateDebugData;
        int m_ExecutionCount;
        int m_CurrentFrameIndex;
        bool m_ExecutionExceptionWasRaised;
        bool m_EnableCompilationCaching;
        internal/*for tests*/ static bool? s_EnableCompilationCachingForTests;
        RenderGraphState m_RenderGraphState;
        RenderTextureUVOriginStrategy m_renderTextureUVOriginStrategy;

        // Global container of registered render graphs, associated with the list of executions that have been registered for them.
        // When a RenderGraph is created, an entry is added to this dictionary. When that RenderGraph renders something,
        // and a debug session is active, an entry is added to the list of executions for that RenderGraph using the executionId
        // as the key. So when you render multiple times with the same executionId, only one DebugExecutionItem is created.
        static Dictionary<RenderGraph, List<DebugExecutionItem>> s_RegisteredExecutions = new ();

        #region Public Interface
        /// <summary>Name of the Render Graph.</summary>
        public string name { get; private set; } = "RenderGraph";

        internal RenderGraphState RenderGraphState
        {
            get { return m_RenderGraphState; }
            set { m_RenderGraphState = value; }
        }

        /// <summary>The strategy the Render Graph will take for the uv origin of RenderTextures in the graph.</summary>
        public RenderTextureUVOriginStrategy renderTextureUVOriginStrategy
        {
            get { return m_renderTextureUVOriginStrategy; }
            internal set { m_renderTextureUVOriginStrategy = value; }
        }

        /// <summary>If true, the Render Graph Viewer is active.</summary>
        public static bool isRenderGraphViewerActive => RenderGraphDebugSession.hasActiveDebugSession;

        /// <summary>If true, the Render Graph will run its various validity checks while processing (not considered in release mode).</summary>
        internal static bool enableValidityChecks { get; private set; }

        /// <summary>
        /// Set of default resources usable in a pass rendering code.
        /// </summary>
        public RenderGraphDefaultResources defaultResources
        {
            get
            {
                return m_DefaultResources;
            }
        }

        /// <summary>
        /// Render Graph constructor.
        /// </summary>
        /// <param name="name">Optional name used to identify the render graph instance.</param>
        public RenderGraph(string name = "RenderGraph")
        {
            this.name = name;
            if (GraphicsSettings.TryGetRenderPipelineSettings<RenderGraphGlobalSettings>(out var renderGraphGlobalSettings))
            {
                m_EnableCompilationCaching = renderGraphGlobalSettings.enableCompilationCaching;
                enableValidityChecks = renderGraphGlobalSettings.enableValidityChecks;
            }
            else // No SRP pipeline is present/active, it can happen with unit tests
            {
                enableValidityChecks = true;
            }

            if (s_EnableCompilationCachingForTests.HasValue)
                m_EnableCompilationCaching = s_EnableCompilationCachingForTests.Value;

            if (m_EnableCompilationCaching)
                m_CompilationCache = new RenderGraphCompilationCache();

            m_Resources = new RenderGraphResourceRegistry(m_DebugParameters);
            RegisterGraph();

            m_RenderGraphState = RenderGraphState.Idle;

            RenderGraph.RenderGraphExceptionMessages.enableCaller = true;

#if !UNITY_EDITOR && DEVELOPMENT_BUILD
            if (RenderGraphDebugSession.currentDebugSession == null)
                RenderGraphDebugSession.Create<RenderGraphPlayerRemoteDebugSession>();
#endif
        }

        // Internal, only for testing
        // Useful when we need to clean when calling
        // internal functions in tests even if Render Graph is active
        // This API shouldn't be called when the render graph is active!
        internal void CleanupResourcesAndGraph()
        {
            // Usually done at the end of Execute step
            // Also doing it here in case RG stopped before it
            ClearCurrentCompiledGraph();

            m_Resources.Cleanup();
            m_DefaultResources.Cleanup();
            m_RenderGraphPool.Cleanup();
            nativeCompiler?.Cleanup();
            m_CompilationCache?.Clear();

            DelegateHashCodeUtils.ClearCache();
        }

        /// <summary>
        /// Free up all resources used internally by the Render Graph instance, and unregister it so it won't be visible in the Render Graph Viewer.
        /// </summary>
        public void Cleanup()
        {
            CheckNotUsedWhenActive();

            // Dispose of the compiled graphs left over in the cache
            m_CompilationCache?.Cleanup();

            CleanupResourcesAndGraph();
            UnregisterGraph();
        }

        internal RenderGraphDebugParams debugParams => m_DebugParameters;

        internal List<DebugUI.Widget> GetWidgetList()
        {
            return m_DebugParameters.GetWidgetList(name);
        }

        internal bool areAnySettingsActive => m_DebugParameters.AreAnySettingsActive;

        /// <summary>
        /// Register the render graph to the debug window.
        /// </summary>
        /// <remarks>
        /// This API cannot be called when Render Graph is active, please call it outside of RecordRenderGraph().
        /// </remarks>
        /// <param name="panel">Optional debug panel to which the render graph debug parameters will be registered.</param>
        public void RegisterDebug(DebugUI.Panel panel = null)
        {
            CheckNotUsedWhenActive();

            m_DebugParameters.RegisterDebug(name, panel);
        }

        /// <summary>
        /// Unregister render graph from the debug window.
        /// </summary>
        /// <remarks>
        /// This API cannot be called when Render Graph is active, please call it outside of RecordRenderGraph().
        /// </remarks>
        public void UnRegisterDebug()
        {
            CheckNotUsedWhenActive();

            m_DebugParameters.UnRegisterDebug(this.name);
        }

        /// <summary>
        /// Get the list of all registered render graphs.
        /// </summary>
        /// <returns>The list of all registered render graphs.</returns>
        public static List<RenderGraph> GetRegisteredRenderGraphs()
        {
            return new List<RenderGraph>(s_RegisteredExecutions.Keys);
        }

        internal static Dictionary<RenderGraph, List<DebugExecutionItem>> GetRegisteredExecutions() => s_RegisteredExecutions;

        /// <summary>
        /// End frame processing. Purge resources that have been used since last frame and resets internal states.
        /// This need to be called once per frame.
        /// </summary>
        /// <remarks>
        /// This API cannot be called when Render Graph is active, please call it outside of RecordRenderGraph().
        /// </remarks>
        public void EndFrame()
        {
            CheckNotUsedWhenActive();

            m_Resources.PurgeUnusedGraphicsResources();
        }

        /// <summary>
        /// Import an external texture to the Render Graph.
        /// Any pass writing to an imported texture will be considered having side effects and can't be automatically culled.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        public TextureHandle ImportTexture(RTHandle rt)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.ImportTexture(rt);
        }

        /// <summary>
        /// Import an external Variable Rate Shading (VRS) textures to the RenderGraph.
        /// Any pass writing to an imported texture will be considered having side effects and can't be automatically culled.
        /// </summary>
        /// <param name="rt">External shading rate image RTHandle that needs to be imported.</param>
        /// <returns>New TextureHandle that represents the imported shading rate images in the context of this rendergraph.</returns>
        public TextureHandle ImportShadingRateImageTexture(RTHandle rt)
        {
            if (ShadingRateInfo.supportsPerImageTile)
                return m_Resources.ImportTexture(rt);

            return TextureHandle.nullHandle;
        }

        /// <summary>
        /// Import an external texture to the Render Graph.
        /// Any pass writing to an imported texture will be considered having side effects and can't be automatically culled.
        ///
        /// Note: RTHandles that wrap RenderTargetIdentifier will fail to import using this overload as render graph can't derive the render texture's properties.
        /// In that case the overload taking a RenderTargetInfo argument should be used instead.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <param name="importParams">Info describing the clear behavior of imported textures. Clearing textures using importParams may be more efficient than manually clearing the texture using `cmd.Clear` on some hardware.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        public TextureHandle ImportTexture(RTHandle rt, ImportResourceParams importParams)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.ImportTexture(rt, importParams);
        }

        /// <summary>
        /// Import an external texture to the Render Graph. This overload should be used for RTHandles  wrapping a RenderTargetIdentifier.
        /// If the RTHandle is wrapping a RenderTargetIdentifer, Rendergraph can't derive the render texture's properties so the user has to provide this info to the graph through RenderTargetInfo.
        ///
        /// Any pass writing to an imported texture will be considered having side effects and can't be automatically culled.
        ///
        /// Note: To avoid inconsistencies between the passed in RenderTargetInfo and render texture this overload can only be used when the RTHandle is wrapping a RenderTargetIdentifier.
        /// If this is not the case, the overload of ImportTexture without a RenderTargetInfo argument should be used instead.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <param name="info">The properties of the passed in RTHandle.</param>
        /// <param name="importParams">Info describing the clear behavior of imported textures. Clearing textures using importParams may be more efficient than manually clearing the texture using `cmd.Clear` on some hardware.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        public TextureHandle ImportTexture(RTHandle rt, RenderTargetInfo info, ImportResourceParams importParams = new ImportResourceParams())
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.ImportTexture(rt, info, importParams);
        }

        /// <summary>
        /// Import an external texture to the Render Graph and set the handle as builtin handle. This can only happen from within the graph module
        /// so it is internal.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <param name="isBuiltin">The handle is a builtin handle managed by RenderGraph internally.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        internal TextureHandle ImportTexture(RTHandle rt, bool isBuiltin)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.ImportTexture(rt, isBuiltin);
        }

        /// <summary>
        /// Import the final backbuffer to render graph. The rendergraph can't derive the properties of a RenderTargetIdentifier as it is an opaque handle so the user has to pass them in through the info argument.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="rt">Backbuffer render target identifier.</param>
        /// <param name="info">The properties of the passed in RTHandle.</param>
        /// <param name="importParams">Info describing the clear behavior of imported textures. Clearing textures using importParams may be more efficient than manually clearing the texture using `cmd.Clear` on some hardware.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        public TextureHandle ImportBackbuffer(RenderTargetIdentifier rt, RenderTargetInfo info, ImportResourceParams importParams = new ImportResourceParams())
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.ImportBackbuffer(rt, info, importParams);
        }

        /// <summary>
        /// Create a new Render Graph Texture resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="desc">Texture descriptor.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle CreateTexture(in TextureDesc desc)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateTexture(desc);
        }

        /// <summary>
        /// Create a new Render Graph Texture resource using the descriptor from another texture.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle CreateTexture(TextureHandle texture)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateTexture(in m_Resources.GetTextureResourceDesc(texture.handle));
        }

        /// <summary>
        /// Create a new Render Graph Texture resource using the descriptor from another texture.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <param name="name">The destination texture name.</param>
        /// <param name="clear">Texture needs to be cleared on first use.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle CreateTexture(TextureHandle texture, string name, bool clear = false)
        {
            CheckNotUsedWhenExecuting();

            var destinationDesc = GetTextureDesc(texture);
            destinationDesc.name = name;
            destinationDesc.clearBuffer = clear;

            return m_Resources.CreateTexture(destinationDesc);
        }

        /// <summary>
        /// Create a new Render Graph Texture if the passed handle is invalid and use said handle as output.
        /// If the passed handle is valid, no texture is created.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="desc">Desc used to create the texture.</param>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        public void CreateTextureIfInvalid(in TextureDesc desc, ref TextureHandle texture)
        {
            CheckNotUsedWhenExecuting();

            if (!texture.IsValid())
                texture = m_Resources.CreateTexture(desc);
        }

        /// <summary>
        /// Gets the descriptor of the specified Texture resource.
        /// </summary>
        /// <param name="texture">Texture resource from which the descriptor is requested.</param>
        /// <returns>The input texture descriptor.</returns>
        public TextureDesc GetTextureDesc(in TextureHandle texture)
        {
            return m_Resources.GetTextureResourceDesc(texture.handle);
        }

        /// <summary>
        /// Gets the descriptor of the specified Texture resource.
        /// </summary>
        /// <param name="texture">Texture resource from which the descriptor is requested.</param>
        /// <returns>The input texture descriptor.</returns>
        public RenderTargetInfo GetRenderTargetInfo(TextureHandle texture)
        {
            RenderTargetInfo info;
            m_Resources.GetRenderTargetInfo(texture.handle, out info);
            return info;
        }

        /// <summary>
        /// Creates a new Renderer List Render Graph resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="desc">Renderer List descriptor.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateRendererList(in CoreRendererListDesc desc)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateRendererList(desc);
        }

        /// <summary>
        /// Creates a new Renderer List Render Graph resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="desc">Renderer List descriptor.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateRendererList(in RendererListParams desc)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateRendererList(desc);
        }

        /// <summary>
        /// Creates a new Shadow  Renderer List Render Graph resource.
        /// </summary>
        /// <param name="shadowDrawingSettings">DrawSettings that describe the shadow drawcall.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateShadowRendererList(ref ShadowDrawingSettings shadowDrawingSettings)
        {
            return m_Resources.CreateShadowRendererList(m_RenderGraphContext.renderContext, ref shadowDrawingSettings);
        }

        /// <summary>
        /// Creates a new Gizmo Renderer List Render Graph resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="camera">The camera that is used for rendering the Gizmo.</param>
        /// <param name="gizmoSubset">GizmoSubset that specifies whether gizmos render before or after postprocessing for a camera render. </param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateGizmoRendererList(in Camera camera, in GizmoSubset gizmoSubset)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateGizmoRendererList(m_RenderGraphContext.renderContext, camera, gizmoSubset);
        }

        /// <summary>
        /// Creates a new UIOverlay Renderer List Render Graph resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="camera">The camera that is used for rendering the full UIOverlay.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateUIOverlayRendererList(in Camera camera)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateUIOverlayRendererList(m_RenderGraphContext.renderContext, camera, UISubset.All);
        }

        /// <summary>
        /// Creates a new UIOverlay Renderer List Render Graph resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="camera">The camera that is used for rendering some subset of the UIOverlay.</param>
        /// <param name="uiSubset">Enum flag that specifies which subset to render.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateUIOverlayRendererList(in Camera camera, in UISubset uiSubset)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateUIOverlayRendererList(m_RenderGraphContext.renderContext, camera, uiSubset);
        }

        /// <summary>
        /// Creates a new WireOverlay Renderer List Render Graph resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="camera">The camera that is used for rendering the WireOverlay.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateWireOverlayRendererList(in Camera camera)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateWireOverlayRendererList(m_RenderGraphContext.renderContext, camera);
        }

        /// <summary>
        /// Creates a new Skybox Renderer List Render Graph resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="camera">The camera that is used for rendering the Skybox.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateSkyboxRendererList(in Camera camera)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateSkyboxRendererList(m_RenderGraphContext.renderContext, camera);
        }

        /// <summary>
        /// Creates a new Skybox Renderer List Render Graph resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="camera">The camera that is used for rendering the Skybox.</param>
        /// <param name="projectionMatrix">The projection matrix used during XR rendering of the skybox.</param>
        /// <param name="viewMatrix">The view matrix used during XR rendering of the skybox.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateSkyboxRendererList(in Camera camera, Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateSkyboxRendererList(m_RenderGraphContext.renderContext, camera, projectionMatrix, viewMatrix);
        }

        /// <summary>
        /// Creates a new Skybox Renderer List Render Graph resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="camera">The camera that is used for rendering the Skybox.</param>
        /// <param name="projectionMatrixL">The left eye projection matrix used during Legacy single pass XR rendering of the skybox.</param>
        /// <param name="viewMatrixL">The left eye view matrix used during Legacy single pass XR rendering of the skybox.</param>
        /// <param name="projectionMatrixR">The right eye projection matrix used during Legacy single pass XR rendering of the skybox.</param>
        /// <param name="viewMatrixR">The right eye view matrix used during Legacy single pass XR rendering of the skybox.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateSkyboxRendererList(in Camera camera, Matrix4x4 projectionMatrixL, Matrix4x4 viewMatrixL, Matrix4x4 projectionMatrixR, Matrix4x4 viewMatrixR)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateSkyboxRendererList(m_RenderGraphContext.renderContext, camera, projectionMatrixL, viewMatrixL, projectionMatrixR, viewMatrixR);
        }

        /// <summary>
        /// Import an external Graphics Buffer to the Render Graph.
        /// Any pass writing to an imported graphics buffer will be considered having side effects and can't be automatically culled.
        /// </summary>
        /// <param name="graphicsBuffer">External Graphics Buffer that needs to be imported.</param>
        /// <returns>A new GraphicsBufferHandle.</returns>
        public BufferHandle ImportBuffer(GraphicsBuffer graphicsBuffer)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.ImportBuffer(graphicsBuffer);
        }

        /// <summary>
        /// Create a new Render Graph Graphics Buffer resource.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="desc">Graphics Buffer descriptor.</param>
        /// <returns>A new GraphicsBufferHandle.</returns>
        public BufferHandle CreateBuffer(in BufferDesc desc)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateBuffer(desc);
        }

        /// <summary>
        /// Create a new Render Graph Graphics Buffer resource using the descriptor from another graphics buffer.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="graphicsBuffer">Graphics Buffer from which the descriptor should be used.</param>
        /// <returns>A new GraphicsBufferHandle.</returns>
        public BufferHandle CreateBuffer(in BufferHandle graphicsBuffer)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.CreateBuffer(in m_Resources.GetBufferResourceDesc(graphicsBuffer.handle));
        }

        /// <summary>
        /// Gets the descriptor of the specified Graphics Buffer resource.
        /// </summary>
        /// <param name="graphicsBuffer">Graphics Buffer resource from which the descriptor is requested.</param>
        /// <returns>The input graphics buffer descriptor.</returns>
        public BufferDesc GetBufferDesc(in BufferHandle graphicsBuffer)
        {
            return m_Resources.GetBufferResourceDesc(graphicsBuffer.handle);
        }

        /// <summary>
        /// Import an external RayTracingAccelerationStructure to the Render Graph.
        /// Any pass writing to (building) an imported RayTracingAccelerationStructure will be considered having side effects and can't be automatically culled.
        /// </summary>
        /// <remarks>
        /// This API cannot be called during the Render Graph execution, please call it outside of SetRenderFunc().
        /// </remarks>
        /// <param name="accelStruct">External RayTracingAccelerationStructure that needs to be imported.</param>
		/// <param name="name">Optional name for identifying the RayTracingAccelerationStructure in the Render Graph.</param>
        /// <returns>A new RayTracingAccelerationStructureHandle.</returns>
        public RayTracingAccelerationStructureHandle ImportRayTracingAccelerationStructure(in RayTracingAccelerationStructure accelStruct, string name = null)
        {
            CheckNotUsedWhenExecuting();

            return m_Resources.ImportRayTracingAccelerationStructure(accelStruct, name);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckNotUsedWhenExecuting()
        {
            if (enableValidityChecks && m_RenderGraphState == RenderGraphState.Executing)
                throw new InvalidOperationException(RenderGraphExceptionMessages.GetExceptionMessage(RenderGraphState.Executing));
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckNotUsedWhenRecordingGraph()
        {
            if (enableValidityChecks && m_RenderGraphState == RenderGraphState.RecordingGraph)
                throw new InvalidOperationException(RenderGraphExceptionMessages.GetExceptionMessage(RenderGraphState.RecordingGraph));
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckNotUsedWhenRecordPassOrExecute()
        {
            if (enableValidityChecks && (m_RenderGraphState == RenderGraphState.RecordingPass || m_RenderGraphState == RenderGraphState.Executing))
                throw new InvalidOperationException(RenderGraphExceptionMessages.GetExceptionMessage(RenderGraphState.RecordingPass | RenderGraphState.Executing));
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckNotUsedWhenRecordingPass()
        {
            if (enableValidityChecks && m_RenderGraphState == RenderGraphState.RecordingPass)
                throw new InvalidOperationException(RenderGraphExceptionMessages.GetExceptionMessage(RenderGraphState.RecordingPass));
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckNotUsedWhenActive()
        {
            if (enableValidityChecks && (m_RenderGraphState & RenderGraphState.Active) != RenderGraphState.Idle)
                throw new InvalidOperationException(RenderGraphExceptionMessages.GetExceptionMessage(RenderGraphState.Active));
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void CheckNotUsedWhenIdle()
        {
            if (enableValidityChecks && m_RenderGraphState == RenderGraphState.Idle)
                throw new InvalidOperationException(RenderGraphExceptionMessages.GetExceptionMessage(RenderGraphState.Active));
        }

        /// <summary>
        /// Add a new Raster Render Pass to the Render Graph. Raster passes can execute rasterization workloads but cannot do other GPU work like copies or compute.
        /// </summary>
        /// <remarks>
        /// This API cannot be called when Render Graph records a pass, please call it within SetRenderFunc() or outside of AddUnsafePass()/AddComputePass()/AddRasterRenderPass().
        /// </remarks>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a IRasterRenderGraphBuilder used to setup the new Rasterization Render Pass.</returns>
        public IRasterRenderGraphBuilder AddRasterRenderPass<PassData>(string passName, out PassData passData
#if !CORE_PACKAGE_DOCTOOLS
            , [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            return AddRasterRenderPass(passName, out passData, GetDefaultProfilingSampler(passName), file, line);
        }

        /// <summary>
        /// Add a new Raster Render Pass to the Render Graph. Raster passes can execute rasterization workloads but cannot do other GPU work like copies or compute.
        /// </summary>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="sampler">Profiling sampler used around the pass.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a IRasterRenderGraphBuilder used to setup the new Rasterization Render Pass.</returns>
        public IRasterRenderGraphBuilder AddRasterRenderPass<PassData>(string passName, out PassData passData, ProfilingSampler sampler
#if !CORE_PACKAGE_DOCTOOLS
            ,[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            CheckNotUsedWhenRecordingPass();

            m_RenderGraphState = RenderGraphState.RecordingPass;

            var renderPass = m_RenderGraphPool.Get<RasterRenderGraphPass<PassData>>();
            renderPass.Initialize(m_RenderPasses.Count, m_RenderGraphPool.Get<PassData>(), passName, RenderGraphPassType.Raster, sampler);

            AddPassDebugMetadata(renderPass, file, line);

            passData = renderPass.data;

            m_RenderPasses.Add(renderPass);

            m_builderInstance.Setup(renderPass, m_Resources, this);

            return m_builderInstance;
        }

        /// <summary>
        /// Add a new Compute Render Pass to the Render Graph. Raster passes can execute rasterization workloads but cannot do other GPU work like copies or compute.
        /// </summary>
        /// <remarks>
        /// This API cannot be called when Render Graph records a pass, please call it within SetRenderFunc() or outside of AddUnsafePass()/AddComputePass()/AddRasterRenderPass().
        /// </remarks>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a IRasterRenderGraphBuilder used to setup the new Rasterization Render Pass.</returns>
        public IComputeRenderGraphBuilder AddComputePass<PassData>(string passName, out PassData passData
#if !CORE_PACKAGE_DOCTOOLS
        , [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            return AddComputePass(passName, out passData, GetDefaultProfilingSampler(passName), file, line);
        }

        /// <summary>
        /// Add a new Compute Render Pass to the Render Graph. Compute passes can execute compute workloads but cannot do rasterization.
        /// </summary>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="sampler">Profiling sampler used around the pass.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a IComputeRenderGraphBuilder used to setup the new Compute Render Pass.</returns>
        public IComputeRenderGraphBuilder AddComputePass<PassData>(string passName, out PassData passData, ProfilingSampler sampler
#if !CORE_PACKAGE_DOCTOOLS
            ,[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            CheckNotUsedWhenRecordingPass();

            m_RenderGraphState = RenderGraphState.RecordingPass;

            var renderPass = m_RenderGraphPool.Get<ComputeRenderGraphPass<PassData>>();
            renderPass.Initialize(m_RenderPasses.Count, m_RenderGraphPool.Get<PassData>(), passName, RenderGraphPassType.Compute, sampler);

            AddPassDebugMetadata(renderPass, file, line);

            passData = renderPass.data;

            m_RenderPasses.Add(renderPass);

            m_builderInstance.Setup(renderPass, m_Resources, this);

            return m_builderInstance;
        }

        /// <summary>
        /// Add a new Unsafe Render Pass to the Render Graph. Unsafe passes can do certain operations compute/raster render passes cannot do and have
        /// access to the full command buffer API. The unsafe API should be used sparingly as it has the following downsides:
        /// - Limited automatic validation of the commands and resource dependencies. The user is responsible to ensure that all dependencies are correctly declared.
        /// - All native render passes will be serialized out.
        /// - In the future the render graph compiler may generate a sub-optimal command stream for unsafe passes.
        /// When using a unsafe pass the graph will also not automatically set up graphics state like rendertargets. The pass should do this itself
        /// using cmd.SetRenderTarget and related commands.
        /// </summary>
        /// <remarks>
        /// This API cannot be called when Render Graph records a pass, please call it within SetRenderFunc() or outside of AddUnsafePass()/AddComputePass()/AddRasterRenderPass().
        /// </remarks>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a IUnsafeRenderGraphBuilder used to setup the new Unsafe Render Pass.</returns>
        public IUnsafeRenderGraphBuilder AddUnsafePass<PassData>(string passName, out PassData passData
#if !CORE_PACKAGE_DOCTOOLS
            , [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            return AddUnsafePass(passName, out passData, GetDefaultProfilingSampler(passName), file, line);
        }

        /// <summary>
        /// Add a new unsafe Render Pass to the Render Graph. Unsafe passes can do certain operations compute/raster render passes cannot do and have
        /// access to the full command buffer API. The unsafe API should be used sparingly as it has the following downsides:
        /// - Limited automatic validation of the commands and resource dependencies. The user is responsible to ensure that all dependencies are correctly declared.
        /// - All native render passes will be serialized out.
        /// - In the future the render graph compiler may generate a sub-optimal command stream for unsafe passes.
        /// When using an unsafe pass the graph will also not automatically set up graphics state like rendertargets. The pass should do this itself
        /// using cmd.SetRenderTarget and related commands.
        /// </summary>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="sampler">Profiling sampler used around the pass.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a IUnsafeRenderGraphBuilder used to setup the new unsafe Render Pass.</returns>
        public IUnsafeRenderGraphBuilder AddUnsafePass<PassData>(string passName, out PassData passData, ProfilingSampler sampler
#if !CORE_PACKAGE_DOCTOOLS
            , [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            CheckNotUsedWhenRecordingPass();

            m_RenderGraphState = RenderGraphState.RecordingPass;

            var renderPass = m_RenderGraphPool.Get<UnsafeRenderGraphPass<PassData>>();
            renderPass.Initialize(m_RenderPasses.Count, m_RenderGraphPool.Get<PassData>(), passName, RenderGraphPassType.Unsafe, sampler);
            renderPass.AllowGlobalState(true);

            AddPassDebugMetadata(renderPass, file, line);

            passData = renderPass.data;

            m_RenderPasses.Add(renderPass);

            m_builderInstance.Setup(renderPass, m_Resources, this);

            return m_builderInstance;
        }

        /// <summary>
        /// Starts the recording of the render graph.
        /// This must be called before adding any pass to the render graph.
        /// </summary>
        /// <remarks>
        /// This API cannot be called when Render Graph is active, please call it outside of RecordRenderGraph().
        /// </remarks>
        /// <param name="parameters">Parameters necessary for the render graph execution.</param>
        /// <example>
        /// <para>Begin recording the Render Graph.</para>
        /// <code>
        /// renderGraph.BeginRecording(parameters)
        /// // Add your render graph passes here.
        /// renderGraph.EndRecordingAndExecute()
        /// </code>
        /// </example>
        public void BeginRecording(in RenderGraphParameters parameters)
        {
            CheckNotUsedWhenActive();

            m_ExecutionExceptionWasRaised = false;
            m_RenderGraphState = RenderGraphState.RecordingGraph;

            m_CurrentFrameIndex = parameters.currentFrameIndex;
            m_CurrentExecutionId = parameters.executionId;

            // Ignore preview cameras and render requests for debug data generation. They would cause the same camera to
            // be rendered twice with different render graphs, confusing users and causing the RG Viewer to constantly update.
            m_CurrentExecutionCanGenerateDebugData = parameters.generateDebugData && parameters.executionId != EntityId.None;

            m_renderTextureUVOriginStrategy = parameters.renderTextureUVOriginStrategy;

            m_Resources.BeginRenderGraph(m_ExecutionCount++);

            m_DefaultResources.InitializeForRendering(this);

            m_RenderGraphContext.cmd = parameters.commandBuffer;
            m_RenderGraphContext.renderContext = parameters.scriptableRenderContext;
            m_RenderGraphContext.contextlessTesting = parameters.invalidContextForTesting;
            m_RenderGraphContext.renderGraphPool = m_RenderGraphPool;
            m_RenderGraphContext.defaultResources = m_DefaultResources;

            // With the actual implementation of the Frame Debugger, we cannot re-use resources during the same frame
            // or it breaks the rendering of the pass preview, since the FD copies the texture after the execution of the RG.
            m_RenderGraphContext.forceResourceCreation =
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                FrameDebugger.enabled;
#else
            false;
#endif
        }

        /// <summary>
        /// Ends the recording and executes the render graph.
        /// This must be called once all passes have been added to the render graph.
        /// </summary>
        public void EndRecordingAndExecute()
        {
            CheckNotUsedWhenRecordPassOrExecute();

            Execute();

            ClearCompiledGraph();

            m_Resources.EndExecute();

            InvalidateContext();

            m_RenderGraphState = RenderGraphState.Idle;
        }

        /// <summary>
        /// Catches and logs exceptions that could happen during the graph recording or execution.
        /// </summary>
        /// <param name="e">The exception thrown by the graph.</param>
        /// <returns>True if contexless testing is enabled, false otherwise.</returns>
        public bool ResetGraphAndLogException(Exception e)
        {
            m_RenderGraphState = RenderGraphState.Idle;

            if (!m_RenderGraphContext.contextlessTesting)
            {
                // If we're not testing log the exception and swallow it.
                // TODO: Do we really want to swallow exceptions here? Not a very c# thing to do.
                Debug.LogError(RenderGraphExceptionMessages.k_RenderGraphExecutionError);
                if (!m_ExecutionExceptionWasRaised) // Already logged. TODO: There is probably a better way in C# to handle that.
                {
                    Debug.LogException(e);
                }

                m_ExecutionExceptionWasRaised = true;
            }

            CommandBuffer.ThrowOnSetRenderTarget = false;

            CleanupResourcesAndGraph();

            // If there has been an error, we have to flush the command being built along the RG data structure,
            // because otherwise the command might try to use resources we just deleted.
            m_RenderGraphContext.cmd.Clear();
            return m_RenderGraphContext.contextlessTesting;
        }

        /// <summary>
        /// Execute the Render Graph in its current state.
        /// </summary>
        internal void Execute()
        {
            m_ExecutionExceptionWasRaised = false;
            m_RenderGraphState = RenderGraphState.Executing;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (m_RenderGraphContext.cmd == null)
                    throw new InvalidOperationException("RenderGraph.BeginRecording was not called before executing the render graph.");

            ClearCacheIfNewActiveDebugSession();
#endif

            int graphHash = m_EnableCompilationCaching ? ComputeGraphHash() : 0;

            CompileNativeRenderGraph(graphHash);

            // Must be set after compilation when the compiler has been initialized
            m_RenderGraphContext.compilerContext = nativeCompiler?.contextData;

            m_Resources.BeginExecute(m_CurrentFrameIndex);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Feeding Render Graph Viewer before resource deallocation at pass execution
            GenerateDebugData(graphHash);
#endif
            ExecuteNativeRenderGraph();

            // Clear the shader bindings for all global textures to make sure bindings don't leak outside the graph
            ClearGlobalBindings();
        }

        class ProfilingScopePassData
        {
            public ProfilingSampler sampler;
        }

        const string k_BeginProfilingSamplerPassName = "BeginProfile";
        const string k_EndProfilingSamplerPassName = "EndProfile";

        /// <summary>
        /// Begin a profiling scope.
        /// </summary>
        /// <param name="sampler">Sampler used for profiling.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        public void BeginProfilingSampler(ProfilingSampler sampler,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (sampler == null)
                return;

            using (var builder = AddUnsafePass<ProfilingScopePassData>(k_BeginProfilingSamplerPassName, out var passData, (ProfilingSampler)null, file, line))
            {
                passData.sampler = sampler;
                builder.AllowPassCulling(false);
                builder.GenerateDebugData(false);
                builder.SetRenderFunc(static (ProfilingScopePassData data, UnsafeGraphContext ctx) =>
                {
                    data.sampler.Begin(CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd));
                });
            }
        }

        /// <summary>
        /// End a profiling scope.
        /// </summary>
        /// <param name="sampler">Sampler used for profiling.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        public void EndProfilingSampler(ProfilingSampler sampler,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (sampler == null)
                return;

            using (var builder = AddUnsafePass<ProfilingScopePassData>(k_EndProfilingSamplerPassName, out var passData, (ProfilingSampler)null, file, line))
            {
                passData.sampler = sampler;
                builder.AllowPassCulling(false);
                builder.GenerateDebugData(false);
                builder.SetRenderFunc(static (ProfilingScopePassData data, UnsafeGraphContext ctx) =>
                {
                    data.sampler.End(CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd));
                });
            }
        }

        #endregion

        #region Internal Interface

        // Internal for testing purpose only
        internal void ClearCurrentCompiledGraph()
        {
            ClearCompiledGraph();
        }

        void ClearCompiledGraph()
        {
            ClearRenderPasses();
            m_Resources.Clear(m_ExecutionExceptionWasRaised);
            m_RendererLists.Clear();
            registeredGlobals.Clear();
        }

        void InvalidateContext()
        {
            m_RenderGraphContext.cmd = null;
            m_RenderGraphContext.renderGraphPool = null;
            m_RenderGraphContext.defaultResources = null;
            m_RenderGraphContext.compilerContext = null;
        }

        internal delegate void OnGraphRegisteredDelegate(string graphName);
        internal static event OnGraphRegisteredDelegate onGraphRegistered;
        internal static event OnGraphRegisteredDelegate onGraphUnregistered;
        internal delegate void OnExecutionRegisteredDelegate(string graphName, EntityId executionId, string executionName);
        internal static event OnExecutionRegisteredDelegate onExecutionRegistered;
        #endregion

        #region Private Interface

        // Internal for testing purpose only.
        internal int ComputeGraphHash()
        {
            using (new ProfilingScope(ProfilingSampler.Get(RenderGraphProfileId.ComputeHashRenderGraph)))
            {
                var hash128 = HashFNV1A32.Create();
                for (int i = 0; i < m_RenderPasses.Count; ++i)
                    m_RenderPasses[i].ComputeHash(ref hash128, m_Resources);

                return hash128.value;
            }
        }

        internal bool GetImportedFallback(TextureDesc desc, out TextureHandle fallback)
        {
            fallback = TextureHandle.nullHandle;

            // We don't have any fallback texture with MSAA
            if (!desc.bindTextureMS)
            {
                if (desc.depthBufferBits != DepthBits.None)
                {
                    fallback = defaultResources.whiteTexture;
                }
                else if (desc.clearColor == Color.black || desc.clearColor == default)
                {
                    if (desc.dimension == TextureXR.dimension)
                        fallback = defaultResources.blackTextureXR;
                    else if (desc.dimension == TextureDimension.Tex3D)
                        fallback = defaultResources.blackTexture3DXR;
                    else if (desc.dimension == TextureDimension.Tex2D)
                        fallback = defaultResources.blackTexture;
                }
                else if (desc.clearColor == Color.white)
                {
                    if (desc.dimension == TextureXR.dimension)
                        fallback = defaultResources.whiteTextureXR;
                    else if (desc.dimension == TextureDimension.Tex2D)
                        fallback = defaultResources.whiteTexture;
                }
            }

            return fallback.IsValid();
        }

        void ClearRenderPasses()
        {
            foreach (var pass in m_RenderPasses)
                pass.Release(m_RenderGraphPool);

            m_RenderPasses.Clear();
        }

        ProfilingSampler GetDefaultProfilingSampler(string name)
        {
            // In non-dev builds, ProfilingSampler.Get returns null, so we'd always end up executing this.
            // To avoid that we also ifdef the code out here.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            int hash = name.GetHashCode();
            if (!m_DefaultProfilingSamplers.TryGetValue(hash, out var sampler))
            {
                sampler = new ProfilingSampler(name);
                m_DefaultProfilingSamplers.Add(hash, sampler);
            }

            return sampler;
#else
            return null;
#endif
        }

        // Register the graph in the RenderGraph Viewer.
        // Registered once on graph creation, indicates to the viewer that this render graph exists.
        void RegisterGraph()
        {
            s_RegisteredExecutions.Add(this, new List<DebugExecutionItem>());
            onGraphRegistered?.Invoke(this.name);
        }

        // Unregister the graph from the render graph viewer.
        // Should only be unregistered when it is destroyed, not before.
        // Should not be called when an error happens because the existence of the graph doesn't depend on its state
        // or whether it's been cleaned.
        void UnregisterGraph()
        {
            s_RegisteredExecutions.Remove(this);
            onGraphUnregistered?.Invoke(name);
        }

        // Note: obj.name allocates so make sure you only call this when debug tools / options are active
        static string GetExecutionNameAllocates(EntityId entityId)
        {
            var obj = Resources.EntityIdToObject(entityId);
            return obj != null ? obj.name : $"RenderGraphExecution ({entityId})";
        }

        static bool s_DebugSessionWasActive;

        void ClearCacheIfNewActiveDebugSession()
        {
            if (RenderGraphDebugSession.hasActiveDebugSession && !s_DebugSessionWasActive)
            {
                // Invalidate cache whenever a debug session becomes active. This is because while the DebugSession is
                // inactive, certain debug data (store/load/pass break audits) stored inside the compilation cache is
                // not getting generated. Therefore we clear compilation cache to regenerate this debug data.
                // This needs to be done before the compilation step.
                m_CompilationCache?.Clear();
            }
            s_DebugSessionWasActive = RenderGraphDebugSession.hasActiveDebugSession;
        }

        void GenerateDebugData(int graphHash)
        {
            if (!RenderGraphDebugSession.hasActiveDebugSession || !m_CurrentExecutionCanGenerateDebugData || m_ExecutionExceptionWasRaised)
                return;

            var registeredExecutions = s_RegisteredExecutions[this];

            // The reverse loop prunes deleted cameras and checks if the current camera needs to be registered.
            bool alreadyRegistered = false;
            using var deletedExecutionIdsDisposable = ListPool<EntityId>.Get(out var deletedExecutionIds);
            for (int i = registeredExecutions.Count - 1; i >= 0; i--)
            {
                DebugExecutionItem executionItem = registeredExecutions[i];
                if (Resources.EntityIdToObject(executionItem.id) == null)
                {
                    registeredExecutions.RemoveAt(i);
                    deletedExecutionIds.Add(executionItem.id);
                    continue;
                }
                if (executionItem.id == m_CurrentExecutionId)
                    alreadyRegistered = true;
            }

            var currentExecutionName = GetExecutionNameAllocates(m_CurrentExecutionId);
            if (!alreadyRegistered)
            {
                registeredExecutions.Add(new DebugExecutionItem(m_CurrentExecutionId, currentExecutionName));
            }

            if (deletedExecutionIds.Count > 0)
                RenderGraphDebugSession.DeleteExecutionIds(name, deletedExecutionIds); // Clear stale debug data entries for deleted cameras

            if (!alreadyRegistered)
            {
                onExecutionRegistered?.Invoke(name, m_CurrentExecutionId, currentExecutionName);
            }

            // When compilation caching is disabled, we don't hash the graph. In order to avoid UI needing to update every
            // frame, we still want to use the hash to know if debug data should be updated, so compute the hash now.
            if (!m_EnableCompilationCaching)
                graphHash = ComputeGraphHash();

            var debugData = RenderGraphDebugSession.GetDebugData(name, m_CurrentExecutionId);
            bool cameraWasRenamed = debugData.executionName != currentExecutionName;
            if (debugData.valid && debugData.graphHash == graphHash && !cameraWasRenamed)
                return; // No need to update

            debugData.Clear();
            debugData.executionName = currentExecutionName;
            debugData.graphHash = graphHash;

            nativeCompiler.GenerateNativeCompilerDebugData(ref debugData);

            debugData.valid = true;
            RenderGraphDebugSession.SetDebugData(name, m_CurrentExecutionId, debugData);
        }

        #endregion

        Dictionary<int, TextureHandle> registeredGlobals = new Dictionary<int, TextureHandle>();

        internal void SetGlobal(in TextureHandle h, int globalPropertyId)
        {
            if (!h.IsValid())
                throw new ArgumentException("Attempting to register an invalid texture handle as a global");

            registeredGlobals[globalPropertyId] = h;
        }

        internal bool IsGlobal(int globalPropertyId)
        {
            return registeredGlobals.ContainsKey(globalPropertyId);
        }

        internal Dictionary<int, TextureHandle>.ValueCollection AllGlobals()
        {
            return registeredGlobals.Values;
        }

        internal TextureHandle GetGlobal(int globalPropertyId)
        {
            TextureHandle h;
            registeredGlobals.TryGetValue(globalPropertyId, out h);
            return h;
        }

        /// <summary>
        /// Clears the shader bindings associated with the registered globals in the graph
        ///
        /// This prevents later rendering logic from accidentally relying on stale shader bindings that were set
        /// earlier during graph execution.
        /// </summary>
        internal void ClearGlobalBindings()
        {
            // Set all the global texture shader bindings to the default black texture.
            // This doesn't technically "clear" the shader bindings, but it's the closest we can do.
            foreach (var globalTex in registeredGlobals)
            {
                m_RenderGraphContext.cmd.SetGlobalTexture(globalTex.Key, defaultResources.blackTexture);
            }
        }
    }

    /// <summary>
    /// Render Graph Scoped Profiling markers
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct RenderGraphProfilingScope : IDisposable
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        ProfilingSampler m_Sampler;
        RenderGraph m_RenderGraph;
        bool m_Disposed;
#endif

        /// <summary>
        /// Profiling Scope constructor
        /// </summary>
        /// <param name="renderGraph">Render Graph used for this scope.</param>
        /// <param name="sampler">Profiling Sampler to be used for this scope.</param>
        public RenderGraphProfilingScope(RenderGraph renderGraph, ProfilingSampler sampler)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_RenderGraph = renderGraph;
            m_Sampler = sampler;
            m_Disposed = false;
            renderGraph.BeginProfilingSampler(sampler);
#endif
        }

        /// <summary>
        ///  Dispose pattern implementation
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        // Protected implementation of Dispose pattern.
        void Dispose(bool disposing)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_Disposed)
                return;

            // As this is a struct, it could have been initialized using an empty constructor so we
            // need to make sure `cmd` isn't null to avoid a crash. Switching to a class would fix
            // this but will generate garbage on every frame (and this struct is used quite a lot).
            if (disposing)
            {
                m_RenderGraph.EndProfilingSampler(m_Sampler);
            }

            m_Disposed = true;
#endif
        }
    }
}
