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
        internal bool contextlessTesting;
    }

    // This whole thing is  a bit of a mess InternalRenderGraphContext is public (but all members are internal)
    // just because the C# standard says that all interface member function implementations should be public.
    // So below in for example the RasterGraphContext we can't implement the (internal) interface as
    // internal void FromInternalContext(InternalRenderGraphContext context) { ... }
    // So we have to make FromInternalContext public so InternalRenderGraphContext also becomes public.
    // This seems an oversight in c# where Interfaces used as Generic constraints could very well be useful
    // with internal only functions.

    internal interface IDerivedRendergraphContext
    {
        /// <summary>
        /// This function is only public for techical resons of the c# language and should not be called outside the package.
        /// </summary>
        /// <param name="context">The context to convert</param>
        public void FromInternalContext(InternalRenderGraphContext context);
    }

    /// <summary>
    /// This class specifies the context given to every render pass. This context type passes a generic
    /// command buffer that can be used to schedule all commands. This will eventually be deprecated
    /// in favor of more specific contexts that have more specific command buffer types.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct RenderGraphContext : IDerivedRendergraphContext
    {
        private InternalRenderGraphContext wrappedContext;

        /// <inheritdoc />
        public void FromInternalContext(InternalRenderGraphContext context)
        {
            wrappedContext = context;
        }

        ///<summary>Scriptable Render Context used for rendering.</summary>
        public ScriptableRenderContext renderContext { get => wrappedContext.renderContext; }
        ///<summary>Command Buffer used for rendering.</summary>
        public CommandBuffer cmd { get => wrappedContext.cmd; }
        ///<summary>Render Graph pool used for temporary data.</summary>
        public RenderGraphObjectPool renderGraphPool { get => wrappedContext.renderGraphPool; }
        ///<summary>Render Graph default resources.</summary>
        public RenderGraphDefaultResources defaultResources { get => wrappedContext.defaultResources; }
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
    }

    /// <summary>
    /// This struct contains properties which control the execution of the Render Graph.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct RenderGraphParameters
    {
        ///<summary>Identifier for this render graph execution.</summary>
        public string executionName;
        ///<summary>Index of the current frame being rendered.</summary>
        public int currentFrameIndex;
        ///<summary> Controls whether to enable Renderer List culling or not.</summary>
        public bool rendererListCulling;
        ///<summary>Scriptable Render Context used by the render pipeline.</summary>
        public ScriptableRenderContext scriptableRenderContext;
        ///<summary>Command Buffer used to execute graphic commands.</summary>
        public CommandBuffer commandBuffer;
        ///<summary>When running tests indicate the context is intentionally invalid and all calls on it should just do nothing.
        ///This allows you to run tests that rely on code execution the way to the pass render functions
        ///This also changes some behaviours with exception handling and error logging so the test framework can act on exceptions to validate behaviour better.</summary>
        internal bool invalidContextForTesting;
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

        internal struct CompiledResourceInfo
        {
            public List<int> producers;
            public List<int> consumers;
            public int refCount;
            public bool imported;

            public void Reset()
            {
                if (producers == null)
                    producers = new List<int>();
                if (consumers == null)
                    consumers = new List<int>();

                producers.Clear();
                consumers.Clear();
                refCount = 0;
                imported = false;
            }
        }

        [DebuggerDisplay("RenderPass: {name} (Index:{index} Async:{enableAsyncCompute})")]
        internal struct CompiledPassInfo
        {
            public string name;
            public int index;

            public List<int>[] resourceCreateList;
            public List<int>[] resourceReleaseList;
            public GraphicsFence fence;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // This members are only here to ease debugging.
            public List<string>[] debugResourceReads;
            public List<string>[] debugResourceWrites;
#endif

            public int refCount;
            public int syncToPassIndex; // Index of the pass that needs to be waited for.
            public int syncFromPassIndex; // Smaller pass index that waits for this pass.

            public bool enableAsyncCompute;
            public bool allowPassCulling;
            public bool needGraphicsFence;
            public bool culled;
            public bool culledByRendererList;
            public bool hasSideEffect;
            public bool enableFoveatedRasterization;
            public bool hasShadingRateImage;
            public bool hasShadingRateStates;

            public void Reset(RenderGraphPass pass, int index)
            {
                name = pass.name;
                this.index = index;

                enableAsyncCompute = pass.enableAsyncCompute;
                allowPassCulling = pass.allowPassCulling;
                enableFoveatedRasterization = pass.enableFoveatedRasterization;
                hasShadingRateImage = pass.hasShadingRateImage && !pass.enableFoveatedRasterization;
                hasShadingRateStates = pass.hasShadingRateStates && !pass.enableFoveatedRasterization;

                if (resourceCreateList == null)
                {
                    resourceCreateList = new List<int>[(int)RenderGraphResourceType.Count];
                    resourceReleaseList = new List<int>[(int)RenderGraphResourceType.Count];
                    for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                    {
                        resourceCreateList[i] = new List<int>();
                        resourceReleaseList[i] = new List<int>();
                    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    debugResourceReads = new List<string>[(int)RenderGraphResourceType.Count];
                    debugResourceWrites = new List<string>[(int)RenderGraphResourceType.Count];
                    for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                    {
                        debugResourceReads[i] = new List<string>();
                        debugResourceWrites[i] = new List<string>();
                    }
#endif
                }

                for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                {
                    resourceCreateList[i].Clear();
                    resourceReleaseList[i].Clear();
                }

                refCount = 0;
                culled = false;
                culledByRendererList = false;
                hasSideEffect = false;
                syncToPassIndex = -1;
                syncFromPassIndex = -1;
                needGraphicsFence = false;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                {
                    debugResourceReads[i].Clear();
                    debugResourceWrites[i].Clear();
                }
#endif
            }
        }

        /// <summary>
        /// Enable the use of the render pass API by the graph instead of traditional SetRenderTarget. This is an advanced
        /// feature and users have to be aware of the specific impact it has on rendergraph/graphics APIs below.
        ///
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
        /// </summary>
        public bool nativeRenderPassesEnabled
        {
            get; set;
        }

        internal/*for tests*/ RenderGraphResourceRegistry m_Resources;
        RenderGraphObjectPool m_RenderGraphPool = new RenderGraphObjectPool();
        RenderGraphBuilders m_builderInstance = new RenderGraphBuilders();
        internal/*for tests*/ List<RenderGraphPass> m_RenderPasses = new List<RenderGraphPass>(64);
        List<RendererListHandle> m_RendererLists = new List<RendererListHandle>(32);
        RenderGraphDebugParams m_DebugParameters = new RenderGraphDebugParams();
        RenderGraphLogger m_FrameInformationLogger = new RenderGraphLogger();
        RenderGraphDefaultResources m_DefaultResources = new RenderGraphDefaultResources();
        Dictionary<int, ProfilingSampler> m_DefaultProfilingSamplers = new Dictionary<int, ProfilingSampler>();
        InternalRenderGraphContext m_RenderGraphContext = new InternalRenderGraphContext();
        CommandBuffer m_PreviousCommandBuffer;
        List<int>[] m_ImmediateModeResourceList = new List<int>[(int)RenderGraphResourceType.Count];
        RenderGraphCompilationCache m_CompilationCache;

        RenderTargetIdentifier[][] m_TempMRTArrays = null;

        internal interface ICompiledGraph
        {
            public void Clear();
        }

        // Compiled Render Graph info.
        internal class CompiledGraph : ICompiledGraph
        {
            // This is a 1:1 mapping on the resource handle indexes, this means the first element with index 0 will represent the "null" handle
            public DynamicArray<CompiledResourceInfo>[] compiledResourcesInfos = new DynamicArray<CompiledResourceInfo>[(int)RenderGraphResourceType.Count];
            public DynamicArray<CompiledPassInfo> compiledPassInfos = new DynamicArray<CompiledPassInfo>();
            public int lastExecutionFrame;

            public CompiledGraph()
            {
                for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                {
                    compiledResourcesInfos[i] = new DynamicArray<CompiledResourceInfo>();
                }
            }

            public void Clear()
            {
                for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                    compiledResourcesInfos[i].Clear();
                compiledPassInfos.Clear();
            }

            void InitResourceInfosData(DynamicArray<CompiledResourceInfo> resourceInfos, int count)
            {
                resourceInfos.Resize(count);
                for (int i = 0; i < resourceInfos.size; ++i)
                    resourceInfos[i].Reset();
            }

            public void InitializeCompilationData(List<RenderGraphPass> passes, RenderGraphResourceRegistry resources)
            {
                InitResourceInfosData(compiledResourcesInfos[(int)RenderGraphResourceType.Texture], resources.GetTextureResourceCount());
                InitResourceInfosData(compiledResourcesInfos[(int)RenderGraphResourceType.Buffer], resources.GetBufferResourceCount());
                InitResourceInfosData(compiledResourcesInfos[(int)RenderGraphResourceType.AccelerationStructure], resources.GetRayTracingAccelerationStructureResourceCount());

                compiledPassInfos.Resize(passes.Count);
                for (int i = 0; i < compiledPassInfos.size; ++i)
                    compiledPassInfos[i].Reset(passes[i], i);
            }
        }

        Stack<int> m_CullingStack = new Stack<int>();

        string m_CurrentExecutionName;
        int m_ExecutionCount;
        int m_CurrentFrameIndex;
        int m_CurrentImmediatePassIndex;
        bool m_ExecutionExceptionWasRaised;
        bool m_HasRenderGraphBegun;
        bool m_RendererListCulling;
        bool m_EnableCompilationCaching;
        CompiledGraph m_DefaultCompiledGraph = new();
        CompiledGraph m_CurrentCompiledGraph;
        string m_CaptureDebugDataForExecution; // Null unless debug data has been requested

        Dictionary<string, DebugData> m_DebugData = new Dictionary<string, DebugData>();

        // Global list of living render graphs
        static List<RenderGraph> s_RegisteredGraphs = new List<RenderGraph>();

        #region Public Interface
        /// <summary>Name of the Render Graph.</summary>
        public string name { get; private set; } = "RenderGraph";

        /// <summary>Request debug data be captured for the provided execution on the next frame.</summary>
        internal void RequestCaptureDebugData(string executionName)
        {
            m_CaptureDebugDataForExecution = executionName;
        }

        /// <summary>If true, the Render Graph Viewer is active.</summary>
        public static bool isRenderGraphViewerActive { get; internal set; }

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
        /// <param name="name">Optional name used to identify the render graph instnace.</param>
        public RenderGraph(string name = "RenderGraph")
        {
            this.name = name;
            if (GraphicsSettings.TryGetRenderPipelineSettings<RenderGraphGlobalSettings>(out var renderGraphGlobalSettings))
            {
                m_EnableCompilationCaching = renderGraphGlobalSettings.enableCompilationCaching;
                if (m_EnableCompilationCaching)
                    m_CompilationCache = new RenderGraphCompilationCache();

                enableValidityChecks = renderGraphGlobalSettings.enableValidityChecks;
            }
            else // No SRP pipeline is present/active, it can happen with unit tests
            {
                enableValidityChecks = true;
            }

            m_TempMRTArrays = new RenderTargetIdentifier[kMaxMRTCount][];
            for (int i = 0; i < kMaxMRTCount; ++i)
                m_TempMRTArrays[i] = new RenderTargetIdentifier[i + 1];

            m_Resources = new RenderGraphResourceRegistry(m_DebugParameters, m_FrameInformationLogger);
            s_RegisteredGraphs.Add(this);
            onGraphRegistered?.Invoke(this);
        }

        /// <summary>
        /// Cleanup the Render Graph.
        /// </summary>
        public void Cleanup()
        {
            // Usually done at the end of Execute step
            // Also doing it here in case RG stopped before it
            ClearCurrentCompiledGraph();

            m_Resources.Cleanup();
            m_DefaultResources.Cleanup();
            m_RenderGraphPool.Cleanup();

            s_RegisteredGraphs.Remove(this);
            onGraphUnregistered?.Invoke(this);

            nativeCompiler?.Cleanup();

            m_CompilationCache?.Clear();
            
            DelegateHashCodeUtils.ClearCache();
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
        /// <param name="panel">Optional debug panel to which the render graph debug parameters will be registered.</param>
        public void RegisterDebug(DebugUI.Panel panel = null)
        {
            m_DebugParameters.RegisterDebug(name, panel);
        }

        /// <summary>
        /// Unregister render graph from the debug window.
        /// </summary>
        public void UnRegisterDebug()
        {
            m_DebugParameters.UnRegisterDebug(this.name);
        }

        /// <summary>
        /// Get the list of all registered render graphs.
        /// </summary>
        /// <returns>The list of all registered render graphs.</returns>
        public static List<RenderGraph> GetRegisteredRenderGraphs()
        {
            return s_RegisteredGraphs;
        }

        /// <summary>
        /// Returns the last rendered frame debug data. Can be null if requireDebugData is set to false.
        /// </summary>
        /// <returns>The last rendered frame debug data</returns>
        internal DebugData GetDebugData(string executionName)
        {
            if (m_DebugData.TryGetValue(executionName, out var debugData))
                return debugData;

            return null;
        }

        /// <summary>
        /// End frame processing. Purge resources that have been used since last frame and resets internal states.
        /// This need to be called once per frame.
        /// </summary>
        public void EndFrame()
        {
            m_Resources.PurgeUnusedGraphicsResources();

            if (m_DebugParameters.logFrameInformation)
            {
                Debug.Log(m_FrameInformationLogger.GetAllLogs());
                m_DebugParameters.logFrameInformation = false;
            }
            if (m_DebugParameters.logResources)
            {
                m_Resources.FlushLogs();
                m_DebugParameters.logResources = false;
            }
        }

        /// <summary>
        /// Import an external texture to the Render Graph.
        /// Any pass writing to an imported texture will be considered having side effects and can't be automatically culled.
        /// </summary>
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        public TextureHandle ImportTexture(RTHandle rt)
        {
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
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <param name="importParams">Info describing the clear behavior of imported textures. Clearing textures using importParams may be more efficient than manually clearing the texture using `cmd.Clear` on some hardware.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        public TextureHandle ImportTexture(RTHandle rt, ImportResourceParams importParams )
        {
            return m_Resources.ImportTexture(rt, importParams);
        }


        /// <summary>
        /// Import an external texture to the Render Graph. This overload should be used for RTHandles  wrapping a RenderTargetIdentifier.
        /// If the RTHandle is wrapping a RenderTargetIdentifer, Rendergrpah can't derive the render texture's properties so the user has to provide this info to the graph through RenderTargetInfo.
        ///
        /// Any pass writing to an imported texture will be considered having side effects and can't be automatically culled.
        ///
        /// Note: To avoid inconsistencies between the passed in RenderTargetInfo and render texture this overload can only be used when the RTHandle is wrapping a RenderTargetIdentifier.
        /// If this is not the case, the overload of ImportTexture without a RenderTargetInfo argument should be used instead.
        /// </summary>
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <param name="info">The properties of the passed in RTHandle.</param>
        /// <param name="importParams">Info describing the clear behavior of imported textures. Clearing textures using importParams may be more efficient than manually clearing the texture using `cmd.Clear` on some hardware.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        public TextureHandle ImportTexture(RTHandle rt, RenderTargetInfo info, ImportResourceParams importParams = new ImportResourceParams() )
        {
            return m_Resources.ImportTexture(rt, info, importParams);
        }

        /// <summary>
        /// Import an external texture to the Render Graph and set the handle as builtin handle. This can only happen from within the graph module
        /// so it is internal.
        /// </summary>
        internal TextureHandle ImportTexture(RTHandle rt, bool isBuiltin)
        {
            return m_Resources.ImportTexture(rt, isBuiltin);
        }

        /// <summary>
        /// Import the final backbuffer to render graph. The rendergraph can't derive the properties of a RenderTargetIdentifier as it is an opaque handle so the user has to pass them in through the info argument.
        /// </summary>
        /// <param name="rt">Backbuffer render target identifier.</param>
        /// <param name="info">The properties of the passed in RTHandle.</param>
        /// <param name="importParams">Info describing the clear behavior of imported textures. Clearing textures using importParams may be more efficient than manually clearing the texture using `cmd.Clear` on some hardware.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        public TextureHandle ImportBackbuffer(RenderTargetIdentifier rt, RenderTargetInfo info, ImportResourceParams importParams = new ImportResourceParams())
        {
            return m_Resources.ImportBackbuffer(rt, info, importParams);
        }

        /// <summary>
        /// Import the final backbuffer to render graph.
        /// This function can only be used when nativeRenderPassesEnabled is false.
        /// </summary>
        /// <param name="rt">Backbuffer render target identifier.</param>
        /// <returns>A new TextureHandle that represents the imported texture in the context of this rendergraph.</returns>
        public TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
        {
            RenderTargetInfo dummy = new RenderTargetInfo();
            dummy.width = dummy.height = dummy.volumeDepth = dummy.msaaSamples = 1;
            dummy.format = GraphicsFormat.R8G8B8A8_SRGB;
            return m_Resources.ImportBackbuffer(rt, dummy, new ImportResourceParams());
        }

        /// <summary>
        /// Create a new Render Graph Texture resource.
        /// </summary>
        /// <param name="desc">Texture descriptor.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle CreateTexture(in TextureDesc desc)
        {
            return m_Resources.CreateTexture(desc);
        }

        /// <summary>
        /// Create a new Render Graph Shared Texture resource.
        /// This texture will be persistent across render graph executions.
        /// </summary>
        /// <param name="desc">Creation descriptor of the texture.</param>
        /// <param name="explicitRelease">Set to true if you want to manage the lifetime of the resource yourself. Otherwise the resource will be released automatically if unused for a time.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle CreateSharedTexture(in TextureDesc desc, bool explicitRelease = false)
        {
            if (m_HasRenderGraphBegun)
                throw new InvalidOperationException("A shared texture can only be created outside of render graph execution.");

            return m_Resources.CreateSharedTexture(desc, explicitRelease);
        }

        /// <summary>
        /// Refresh a shared texture with a new descriptor.
        /// </summary>
        /// <param name="handle">Shared texture that needs to be updated.</param>
        /// <param name="desc">New Descriptor for the texture.</param>
        public void RefreshSharedTextureDesc(TextureHandle handle, in TextureDesc desc)
        {
            m_Resources.RefreshSharedTextureDesc(handle, desc);
        }

        /// <summary>
        /// Release a Render Graph shared texture resource.
        /// </summary>
        /// <param name="texture">The handle to the texture that needs to be release.</param>
        public void ReleaseSharedTexture(TextureHandle texture)
        {
            if (m_HasRenderGraphBegun)
                throw new InvalidOperationException("A shared texture can only be release outside of render graph execution.");

            m_Resources.ReleaseSharedTexture(texture);
        }

        /// <summary>
        /// Create a new Render Graph Texture resource using the descriptor from another texture.
        /// </summary>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle CreateTexture(TextureHandle texture)
        {
            return m_Resources.CreateTexture(m_Resources.GetTextureResourceDesc(texture.handle));
        }

        /// <summary>
        /// Create a new Render Graph Texture if the passed handle is invalid and use said handle as output.
        /// If the passed handle is valid, no texture is created.
        /// </summary>
        /// <param name="desc">Desc used to create the texture.</param>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        public void CreateTextureIfInvalid(in TextureDesc desc, ref TextureHandle texture)
        {
            if (!texture.IsValid())
                texture = m_Resources.CreateTexture(desc);
        }

        /// <summary>
        /// Gets the descriptor of the specified Texture resource.
        /// </summary>
        /// <param name="texture">Texture resource from which the descriptor is requested.</param>
        /// <returns>The input texture descriptor.</returns>
        public TextureDesc GetTextureDesc(TextureHandle texture)
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
        /// <param name="desc">Renderer List descriptor.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateRendererList(in CoreRendererListDesc desc)
        {
            return m_Resources.CreateRendererList(desc);
        }

        /// <summary>
        /// Creates a new Renderer List Render Graph resource.
        /// </summary>
        /// <param name="desc">Renderer List descriptor.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateRendererList(in RendererListParams desc)
        {
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
        /// <param name="camera">The camera that is used for rendering the Gizmo.</param>
        /// <param name="gizmoSubset">GizmoSubset that specifies whether gizmos render before or after postprocessing for a camera render. </param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateGizmoRendererList(in Camera camera, in GizmoSubset gizmoSubset)
        {
            return m_Resources.CreateGizmoRendererList(m_RenderGraphContext.renderContext, camera, gizmoSubset);
        }

        /// <summary>
        /// Creates a new UIOverlay Renderer List Render Graph resource.
        /// </summary>
        /// <param name="camera">The camera that is used for rendering the full UIOverlay.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateUIOverlayRendererList(in Camera camera)
        {
            return m_Resources.CreateUIOverlayRendererList(m_RenderGraphContext.renderContext, camera, UISubset.All);
        }

        /// <summary>
        /// Creates a new UIOverlay Renderer List Render Graph resource.
        /// </summary>
        /// <param name="camera">The camera that is used for rendering some subset of the UIOverlay.</param>
        /// <param name="uiSubset">Enum flag that specifies which subset to render.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateUIOverlayRendererList(in Camera camera, in UISubset uiSubset)
        {
            return m_Resources.CreateUIOverlayRendererList(m_RenderGraphContext.renderContext, camera, uiSubset);
        }

        /// <summary>
        /// Creates a new WireOverlay Renderer List Render Graph resource.
        /// </summary>
        /// <param name="camera">The camera that is used for rendering the WireOverlay.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateWireOverlayRendererList(in Camera camera)
        {
            return m_Resources.CreateWireOverlayRendererList(m_RenderGraphContext.renderContext, camera);
        }

        /// <summary>
        /// Creates a new Skybox Renderer List Render Graph resource.
        /// </summary>
        /// <param name="camera">The camera that is used for rendering the Skybox.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateSkyboxRendererList(in Camera camera)
        {
            return m_Resources.CreateSkyboxRendererList(m_RenderGraphContext.renderContext, camera);
        }

        /// <summary>
        /// Creates a new Skybox Renderer List Render Graph resource.
        /// </summary>
        /// <param name="camera">The camera that is used for rendering the Skybox.</param>
        /// <param name="projectionMatrix">The projection matrix used during XR rendering of the skybox.</param>
        /// <param name="viewMatrix">The view matrix used during XR rendering of the skybox.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateSkyboxRendererList(in Camera camera, Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix)
        {
            return m_Resources.CreateSkyboxRendererList(m_RenderGraphContext.renderContext, camera, projectionMatrix, viewMatrix);
        }

        /// <summary>
        /// Creates a new Skybox Renderer List Render Graph resource.
        /// </summary>
        /// <param name="camera">The camera that is used for rendering the Skybox.</param>
        /// <param name="projectionMatrixL">The left eye projection matrix used during Legacy single pass XR rendering of the skybox.</param>
        /// <param name="viewMatrixL">The left eye view matrix used during Legacy single pass XR rendering of the skybox.</param>
        /// <param name="projectionMatrixR">The right eye projection matrix used during Legacy single pass XR rendering of the skybox.</param>
        /// <param name="viewMatrixR">The right eye view matrix used during Legacy single pass XR rendering of the skybox.</param>
        /// <returns>A new RendererListHandle.</returns>
        public RendererListHandle CreateSkyboxRendererList(in Camera camera, Matrix4x4 projectionMatrixL, Matrix4x4 viewMatrixL, Matrix4x4 projectionMatrixR, Matrix4x4 viewMatrixR)
        {
            return m_Resources.CreateSkyboxRendererList(m_RenderGraphContext.renderContext, camera, projectionMatrixL, viewMatrixL, projectionMatrixR, viewMatrixR);
        }

        /// <summary>
        /// Import an external Graphics Buffer to the Render Graph.
        /// Any pass writing to an imported graphics buffer will be considered having side effects and can't be automatically culled.
        /// </summary>
        /// <param name="graphicsBuffer">External Graphics Buffer that needs to be imported.</param>
        /// <param name="forceRelease">The imported graphics buffer will be released after usage.</param>
        /// <returns>A new GraphicsBufferHandle.</returns>
        public BufferHandle ImportBuffer(GraphicsBuffer graphicsBuffer, bool forceRelease = false)
        {
            return m_Resources.ImportBuffer(graphicsBuffer, forceRelease);
        }

        /// <summary>
        /// Create a new Render Graph Graphics Buffer resource.
        /// </summary>
        /// <param name="desc">Graphics Buffer descriptor.</param>
        /// <returns>A new GraphicsBufferHandle.</returns>
        public BufferHandle CreateBuffer(in BufferDesc desc)
        {
            return m_Resources.CreateBuffer(desc);
        }

        /// <summary>
        /// Create a new Render Graph Graphics Buffer resource using the descriptor from another graphics buffer.
        /// </summary>
        /// <param name="graphicsBuffer">Graphics Buffer from which the descriptor should be used.</param>
        /// <returns>A new GraphicsBufferHandle.</returns>
        public BufferHandle CreateBuffer(in BufferHandle graphicsBuffer)
        {
            return m_Resources.CreateBuffer(m_Resources.GetBufferResourceDesc(graphicsBuffer.handle));
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
        /// <param name="accelStruct">External RayTracingAccelerationStructure that needs to be imported.</param>
		/// <param name="name">Optional name for identifying the RayTracingAccelerationStructure in the Render Graph.</param>
        /// <returns>A new RayTracingAccelerationStructureHandle.</returns>
        public RayTracingAccelerationStructureHandle ImportRayTracingAccelerationStructure(in RayTracingAccelerationStructure accelStruct, string name = null)
        {
            return m_Resources.ImportRayTracingAccelerationStructure(accelStruct, name);
        }

        /// <summary>
        /// Add a new Raster Render Pass to the Render Graph. Raster passes can execute rasterization workloads but cannot do other GPU work like copies or compute.
        /// </summary>
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
        /// Add a new Render Pass to the Render Graph.
        /// </summary>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="sampler">Profiling sampler used around the pass.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a RenderGraphBuilder used to setup the new Render Pass.</returns>
        public RenderGraphBuilder AddRenderPass<PassData>(string passName, out PassData passData, ProfilingSampler sampler
#if !CORE_PACKAGE_DOCTOOLS
            ,[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            var renderPass = m_RenderGraphPool.Get<RenderGraphPass<PassData>>();
            renderPass.Initialize(m_RenderPasses.Count, m_RenderGraphPool.Get<PassData>(), passName, RenderGraphPassType.Legacy, sampler);
            renderPass.AllowGlobalState(true);// Old pass types allow global state by default as HDRP relies on it

            AddPassDebugMetadata(renderPass, file, line);

            passData = renderPass.data;

            m_RenderPasses.Add(renderPass);

            return new RenderGraphBuilder(renderPass, m_Resources, this);
        }

        /// <summary>
        /// Add a new Render Pass to the Render Graph.
        /// </summary>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="file">File name of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <param name="line">File line of the source file this function is called from. Used for debugging. This parameter is automatically generated by the compiler. Users do not need to pass it.</param>
        /// <returns>A new instance of a RenderGraphBuilder used to setup the new Render Pass.</returns>
        public RenderGraphBuilder AddRenderPass<PassData>(string passName, out PassData passData
#if !CORE_PACKAGE_DOCTOOLS
            ,[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0) where PassData : class, new()
#endif
        {
            return AddRenderPass(passName, out passData, GetDefaultProfilingSampler(passName), file, line);
        }

        /// <summary>
        /// Starts the recording of the render graph.
        /// This must be called before adding any pass to the render graph.
        /// </summary>
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
            m_CurrentFrameIndex = parameters.currentFrameIndex;
            m_CurrentExecutionName = parameters.executionName != null ? parameters.executionName : "RenderGraphExecution";
            m_HasRenderGraphBegun = true;
            // Cannot do renderer list culling with compilation caching because it happens after compilation is done so it can lead to discrepancies.
            m_RendererListCulling = parameters.rendererListCulling && !m_EnableCompilationCaching;

            m_Resources.BeginRenderGraph(m_ExecutionCount++);

            if (m_DebugParameters.enableLogging)
            {
                m_FrameInformationLogger.Initialize(m_CurrentExecutionName);
            }

            m_DefaultResources.InitializeForRendering(this);

            m_RenderGraphContext.cmd = parameters.commandBuffer;
            m_RenderGraphContext.renderContext = parameters.scriptableRenderContext;
            m_RenderGraphContext.contextlessTesting = parameters.invalidContextForTesting;
            m_RenderGraphContext.renderGraphPool = m_RenderGraphPool;
            m_RenderGraphContext.defaultResources = m_DefaultResources;

            if (m_DebugParameters.immediateMode)
            {
                UpdateCurrentCompiledGraph(graphHash: -1, forceNoCaching: true);

                LogFrameInformation();

                // Prepare the list of compiled pass info for immediate mode.
                // Conservative resize because we don't know how many passes there will be.
                // We might still need to grow the array later on anyway if it's not enough.
                m_CurrentCompiledGraph.compiledPassInfos.Resize(m_CurrentCompiledGraph.compiledPassInfos.capacity);
                m_CurrentImmediatePassIndex = 0;

                for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                {
                    if (m_ImmediateModeResourceList[i] == null)
                        m_ImmediateModeResourceList[i] = new List<int>();

                    m_ImmediateModeResourceList[i].Clear();
                }

                m_Resources.BeginExecute(m_CurrentFrameIndex);
            }
        }

        /// <summary>
        /// Ends the recording and executes the render graph.
        /// This must be called once all passes have been added to the render graph.
        /// </summary>
        public void EndRecordingAndExecute()
        {
            Execute();
        }

        /// <summary>
        /// Execute the Render Graph in its current state.
        /// </summary>
        internal void Execute()
        {
            m_ExecutionExceptionWasRaised = false;

            try
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (m_RenderGraphContext.cmd == null)
                    throw new InvalidOperationException("RenderGraph.BeginRecording was not called before executing the render graph.");
#endif
                if (!m_DebugParameters.immediateMode)
                {
                    LogFrameInformation();

                    int graphHash = 0;
                    if (m_EnableCompilationCaching)
                        graphHash = ComputeGraphHash();

                    if (nativeRenderPassesEnabled)
                        CompileNativeRenderGraph(graphHash);
                    else
                        CompileRenderGraph(graphHash);

                    m_Resources.BeginExecute(m_CurrentFrameIndex);

#if UNITY_EDITOR
                    // Feeding Render Graph Viewer before resource deallocation at pass execution
                    GenerateDebugData();
#endif

                    if (nativeRenderPassesEnabled)
                        ExecuteNativeRenderGraph();
                    else
                        ExecuteRenderGraph();

                    // Clear the shader bindings for all global textures to make sure bindings don't leak outside the graph
                    ClearGlobalBindings();
                }
            }
            catch (Exception e)
            {
                if (m_RenderGraphContext.contextlessTesting)
                {
                    // Throw it for the tests to handle
                    throw;
                }
                else
                {
                    // If we're not testing log the exception and swallow it.
                    // TODO: Do we really want to swallow exceptions here? Not a very c# thing to do.
                    Debug.LogError("Render Graph Execution error");
                    if (!m_ExecutionExceptionWasRaised) // Already logged. TODO: There is probably a better way in C# to handle that.
                        Debug.LogException(e);
                    m_ExecutionExceptionWasRaised = true;
                }
            }
            finally
            {
                if (m_DebugParameters.immediateMode)
                    ReleaseImmediateModeResources();

                ClearCompiledGraph(m_CurrentCompiledGraph, m_EnableCompilationCaching);

                m_Resources.EndExecute();

                InvalidateContext();

                m_HasRenderGraphBegun = false;
            }
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

            using (var builder = AddRenderPass<ProfilingScopePassData>(k_BeginProfilingSamplerPassName, out var passData, (ProfilingSampler)null, file, line))
            {
                passData.sampler = sampler;
                builder.AllowPassCulling(false);
                builder.GenerateDebugData(false);
                builder.SetRenderFunc((ProfilingScopePassData data, RenderGraphContext ctx) =>
                {
                    data.sampler.Begin(ctx.cmd);
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

            using (var builder = AddRenderPass<ProfilingScopePassData>(k_EndProfilingSamplerPassName, out var passData, (ProfilingSampler)null, file, line))
            {
                passData.sampler = sampler;
                builder.AllowPassCulling(false);
                builder.GenerateDebugData(false);
                builder.SetRenderFunc((ProfilingScopePassData data, RenderGraphContext ctx) =>
                {
                    data.sampler.End(ctx.cmd);
                });
            }
        }

        #endregion

        #region Internal Interface
        // Internal for testing purpose only
        internal DynamicArray<CompiledPassInfo> GetCompiledPassInfos() { return m_CurrentCompiledGraph.compiledPassInfos; }

        // Internal for testing purpose only
        internal void ClearCurrentCompiledGraph()
        {
            ClearCompiledGraph(m_CurrentCompiledGraph, false);
        }

        void ClearCompiledGraph(CompiledGraph compiledGraph, bool useCompilationCaching)
        {
            ClearRenderPasses();
            m_Resources.Clear(m_ExecutionExceptionWasRaised);
            m_RendererLists.Clear();
            registeredGlobals.Clear();

            // When using compilation caching, we need to keep alive the result of the compiled graph.
            if (!useCompilationCaching)
            {
                if (!nativeRenderPassesEnabled)
                    compiledGraph?.Clear();
            }
        }

        void InvalidateContext()
        {
            m_RenderGraphContext.cmd = null;
            m_RenderGraphContext.renderGraphPool = null;
            m_RenderGraphContext.defaultResources = null;
        }

        internal void OnPassAdded(RenderGraphPass pass)
        {
            if (m_DebugParameters.immediateMode)
            {
                ExecutePassImmediately(pass);
            }
        }

        internal delegate void OnGraphRegisteredDelegate(RenderGraph graph);
        internal static event OnGraphRegisteredDelegate onGraphRegistered;
        internal static event OnGraphRegisteredDelegate onGraphUnregistered;
        internal delegate void OnExecutionRegisteredDelegate(RenderGraph graph, string executionName);
        internal static event OnExecutionRegisteredDelegate onExecutionRegistered;
        internal static event OnExecutionRegisteredDelegate onExecutionUnregistered;
        internal static event Action onDebugDataCaptured;

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

        void CountReferences()
        {
            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            var compiledResourceInfo = m_CurrentCompiledGraph.compiledResourcesInfos;

            for (int passIndex = 0; passIndex < compiledPassInfo.size; ++passIndex)
            {
                RenderGraphPass pass = m_RenderPasses[passIndex];
                ref CompiledPassInfo passInfo = ref compiledPassInfo[passIndex];

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    var resourceRead = pass.resourceReadLists[type];
                    foreach (var resource in resourceRead)
                    {
                        ref CompiledResourceInfo info = ref compiledResourceInfo[type][resource.index];
                        info.imported = m_Resources.IsRenderGraphResourceImported(resource);
                        info.consumers.Add(passIndex);
                        info.refCount++;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        passInfo.debugResourceReads[type].Add(m_Resources.GetRenderGraphResourceName(resource));
#endif
                    }

                    var resourceWrite = pass.resourceWriteLists[type];
                    foreach (var resource in resourceWrite)
                    {
                        ref CompiledResourceInfo info = ref compiledResourceInfo[type][resource.index];
                        info.imported = m_Resources.IsRenderGraphResourceImported(resource);
                        info.producers.Add(passIndex);

                        // Writing to an imported texture is considered as a side effect because we don't know what users will do with it outside of render graph.
                        passInfo.hasSideEffect = info.imported;
                        passInfo.refCount++;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        passInfo.debugResourceWrites[type].Add(m_Resources.GetRenderGraphResourceName(resource));
#endif
                    }

                    foreach (var resource in pass.transientResourceList[type])
                    {
                        ref CompiledResourceInfo info = ref compiledResourceInfo[type][resource.index];
                        info.refCount++;
                        info.consumers.Add(passIndex);
                        info.producers.Add(passIndex);
                    }
                }
            }
        }

        void CullUnusedPasses()
        {
            if (m_DebugParameters.disablePassCulling)
            {
                if (m_DebugParameters.enableLogging)
                {
                    m_FrameInformationLogger.LogLine("- Pass Culling Disabled -\n");
                }
                return;
            }

            // This will cull all passes that produce resource that are never read.
            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                DynamicArray<CompiledResourceInfo> resourceUsageList = m_CurrentCompiledGraph.compiledResourcesInfos[type];

                // Gather resources that are never read.
                m_CullingStack.Clear();
                for (int i = 1; i < resourceUsageList.size; ++i) // 0 == null resource skip it
                {
                    if (resourceUsageList[i].refCount == 0)
                    {
                        m_CullingStack.Push(i);
                    }
                }

                while (m_CullingStack.Count != 0)
                {
                    var unusedResource = resourceUsageList[m_CullingStack.Pop()];
                    foreach (var producerIndex in unusedResource.producers)
                    {
                        ref var producerInfo = ref m_CurrentCompiledGraph.compiledPassInfos[producerIndex];
                        var producerPass = m_RenderPasses[producerIndex];
                        producerInfo.refCount--;
                        if (producerInfo.refCount == 0 && !producerInfo.hasSideEffect && producerInfo.allowPassCulling)
                        {
                            // Producer is not necessary anymore as it produces zero resources
                            // Cull it and decrement refCount of all the textures it reads.
                            producerInfo.culled = true;

                            foreach (var resource in producerPass.resourceReadLists[type])
                            {
                                ref CompiledResourceInfo resourceInfo = ref resourceUsageList[resource.index];
                                resourceInfo.refCount--;
                                // If a resource is not used anymore, add it to the stack to be processed in subsequent iteration.
                                if (resourceInfo.refCount == 0)
                                    m_CullingStack.Push(resource.index);
                            }
                        }
                    }
                }
            }

            LogCulledPasses();
        }

        void UpdatePassSynchronization(ref CompiledPassInfo currentPassInfo, ref CompiledPassInfo producerPassInfo, int currentPassIndex, int lastProducer, ref int intLastSyncIndex)
        {
            // Current pass needs to wait for pass index lastProducer
            currentPassInfo.syncToPassIndex = lastProducer;
            // Update latest pass waiting for the other pipe.
            intLastSyncIndex = lastProducer;

            // Producer will need a graphics fence that this pass will wait on.
            producerPassInfo.needGraphicsFence = true;
            // We update the producer pass with the index of the smallest pass waiting for it.
            // This will be used to "lock" resource from being reused until the pipe has been synchronized.
            if (producerPassInfo.syncFromPassIndex == -1)
                producerPassInfo.syncFromPassIndex = currentPassIndex;
        }

        void UpdateResourceSynchronization(ref int lastGraphicsPipeSync, ref int lastComputePipeSync, int currentPassIndex, in CompiledResourceInfo resource)
        {
            int lastProducer = GetLatestProducerIndex(currentPassIndex, resource);
            if (lastProducer != -1)
            {
                var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
                ref CompiledPassInfo currentPassInfo = ref compiledPassInfo[currentPassIndex];

                //If the passes are on different pipes, we need synchronization.
                if (m_CurrentCompiledGraph.compiledPassInfos[lastProducer].enableAsyncCompute != currentPassInfo.enableAsyncCompute)
                {
                    // Pass is on compute pipe, need sync with graphics pipe.
                    if (currentPassInfo.enableAsyncCompute)
                    {
                        if (lastProducer > lastGraphicsPipeSync)
                        {
                            UpdatePassSynchronization(ref currentPassInfo, ref compiledPassInfo[lastProducer], currentPassIndex, lastProducer, ref lastGraphicsPipeSync);
                        }
                    }
                    else
                    {
                        if (lastProducer > lastComputePipeSync)
                        {
                            UpdatePassSynchronization(ref currentPassInfo, ref compiledPassInfo[lastProducer], currentPassIndex, lastProducer, ref lastComputePipeSync);
                        }
                    }
                }
            }
        }

        int GetFirstValidConsumerIndex(int passIndex, in CompiledResourceInfo info)
        {
            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            // We want to know the lowest pass index after the current pass that reads from the resource.
            foreach (int consumer in info.consumers)
            {
                // consumers are by construction in increasing order.
                if (consumer > passIndex && !compiledPassInfo[consumer].culled)
                    return consumer;
            }

            return -1;
        }

        int FindTextureProducer(int consumerPass, in CompiledResourceInfo info, out int index)
        {
            // We check all producers before the consumerPass. The first one not culled will be the one allocating the resource
            // If they are all culled, we need to get the one right before the consumer, it will allocate or reuse the resource
            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            int previousPass = 0;
            for (index = 0; index < info.producers.Count; index++)
            {
                int currentPass = info.producers[index];
                // We found a valid producer - he will allocate the texture
                if (!compiledPassInfo[currentPass].culled)
                    return currentPass;
                // We reached consumer pass, return last producer even if it's culled
                if (currentPass >= consumerPass)
                    return previousPass;
                previousPass = currentPass;
            }

            return previousPass;
        }

        int GetLatestProducerIndex(int passIndex, in CompiledResourceInfo info)
        {
            // We want to know the highest pass index below the current pass that writes to the resource.
            int result = -1;
            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            foreach (var producer in info.producers)
            {
                var producerPassInfo = compiledPassInfo[producer];
                // producers are by construction in increasing order.
                // We also need to make sure we don't return a pass that was culled (can happen at this point because of renderer list culling).
                if (producer < passIndex && !(producerPassInfo.culled || producerPassInfo.culledByRendererList))
                    result = producer;
                else
                    return result;
            }

            return result;
        }

        int GetLatestValidReadIndex(in CompiledResourceInfo info)
        {
            if (info.consumers.Count == 0)
                return -1;

            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            var consumers = info.consumers;
            for (int i = consumers.Count - 1; i >= 0; --i)
            {
                if (!compiledPassInfo[consumers[i]].culled)
                    return consumers[i];
            }

            return -1;
        }

        int GetFirstValidWriteIndex(in CompiledResourceInfo info)
        {
            if (info.producers.Count == 0)
                return -1;

            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            var producers = info.producers;
            for (int i = 0; i < producers.Count; i++)
            {
                if (!compiledPassInfo[producers[i]].culled)
                    return producers[i];
            }

            return -1;
        }

        int GetLatestValidWriteIndex(in CompiledResourceInfo info)
        {
            if (info.producers.Count == 0)
                return -1;

            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            var producers = info.producers;
            for (int i = producers.Count - 1; i >= 0; --i)
            {
                if (!compiledPassInfo[producers[i]].culled)
                    return producers[i];
            }

            return -1;
        }

        void CreateRendererLists()
        {
            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            for (int passIndex = 0; passIndex < compiledPassInfo.size; ++passIndex)
            {
                ref CompiledPassInfo passInfo = ref compiledPassInfo[passIndex];

                if (passInfo.culled)
                    continue;

                // Gather all renderer lists
                m_RendererLists.AddRange(m_RenderPasses[passInfo.index].usedRendererListList);
            }

            // Anything related to renderer lists needs a real context to be able to use/test it
            Debug.Assert(m_RendererLists.Count == 0 || m_RenderGraphContext.contextlessTesting == false);

            // Creates all renderer lists
            m_Resources.CreateRendererLists(m_RendererLists, m_RenderGraphContext.renderContext, m_RendererListCulling);
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

        void AllocateCulledPassResources(ref CompiledPassInfo passInfo)
        {
            var passIndex = passInfo.index;
            var pass = m_RenderPasses[passIndex];

            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                var resourcesInfo = m_CurrentCompiledGraph.compiledResourcesInfos[type];
                foreach (var resourceHandle in pass.resourceWriteLists[type])
                {
                    ref var compiledResource = ref resourcesInfo[resourceHandle.index];

                    // Check if there is a valid consumer and no other valid producer
                    int consumerPass = GetFirstValidConsumerIndex(passIndex, compiledResource);
                    int producerPass = FindTextureProducer(consumerPass, compiledResource, out int index);
                    if (consumerPass != -1 && passIndex == producerPass)
                    {
                        if (type == (int)RenderGraphResourceType.Texture)
                        {
                            // Try to transform into an imported resource - for some textures, this will save an allocation
                            // We have a way to disable the fallback, because we can't fallback to RenderTexture and sometimes it's necessary (eg. SampleCopyChannel_xyzw2x)
                            var textureResource = m_Resources.GetTextureResource(resourceHandle);
                            if (!textureResource.desc.disableFallBackToImportedTexture && GetImportedFallback(textureResource.desc, out var fallback))
                            {
                                compiledResource.imported = true;
                                textureResource.imported = true;
                                textureResource.graphicsResource = m_Resources.GetTexture(fallback);
                                continue;
                            }

                            textureResource.desc.sizeMode = TextureSizeMode.Explicit;
                            textureResource.desc.width = 1;
                            textureResource.desc.height = 1;
                            textureResource.desc.clearBuffer = true;
                        }

                        // Delegate resource allocation to the consumer
                        compiledResource.producers[index - 1] = consumerPass;
                    }
                }
            }
        }

        void UpdateResourceAllocationAndSynchronization()
        {
            int lastGraphicsPipeSync = -1;
            int lastComputePipeSync = -1;

            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            var compiledResourceInfo = m_CurrentCompiledGraph.compiledResourcesInfos;

            // First go through all passes.
            // - Update the last pass read index for each resource.
            // - Add texture to creation list for passes that first write to a texture.
            // - Update synchronization points for all resources between compute and graphics pipes.
            for (int passIndex = 0; passIndex < compiledPassInfo.size; ++passIndex)
            {
                ref CompiledPassInfo passInfo = ref compiledPassInfo[passIndex];

                // If this pass is culled, we need to make sure that any texture read by a later pass is still allocated
                // We also try to find an imported fallback to save an allocation
                if (passInfo.culledByRendererList)
                    AllocateCulledPassResources(ref passInfo);

                if (passInfo.culled)
                    continue;

                var pass = m_RenderPasses[passInfo.index];

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    var resourcesInfo = compiledResourceInfo[type];
                    foreach (var resource in pass.resourceReadLists[type])
                    {
                        UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, resourcesInfo[resource.index]);
                    }

                    foreach (var resource in pass.resourceWriteLists[type])
                    {
                        UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, resourcesInfo[resource.index]);
                    }
                }
            }

            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                var resourceInfos = compiledResourceInfo[type];

                // Now push resources to the release list of the pass that reads it last.
                for (int i = 1; i < resourceInfos.size; ++i) // 0 == null resource skip it
                {
                    CompiledResourceInfo resourceInfo = resourceInfos[i];

                    bool sharedResource = m_Resources.IsRenderGraphResourceShared((RenderGraphResourceType)type, i);
                    bool forceRelease = m_Resources.IsRenderGraphResourceForceReleased((RenderGraphResourceType) type, i);

                    // Imported resource needs neither creation nor release.
                    if (resourceInfo.imported && !sharedResource && !forceRelease)
                        continue;

                    // Resource creation
                    int firstWriteIndex = GetFirstValidWriteIndex(resourceInfo);
                    // Index -1 can happen for imported resources (for example an imported dummy black texture will never be written to but does not need creation anyway)
                    // Or when the only pass that was writting to this resource was culled dynamically by renderer lists
                    if (firstWriteIndex != -1)
                        compiledPassInfo[firstWriteIndex].resourceCreateList[type].Add(i);

                    var latestValidReadIndex = GetLatestValidReadIndex(resourceInfo);
                    var latestValidWriteIndex = GetLatestValidWriteIndex(resourceInfo);

                    // Sometimes, a texture can be written by a pass after the last pass that reads it.
                    // In this case, we need to extend its lifetime to this pass otherwise the pass would get an invalid texture.
                    // This is exhibited in cases where a pass might produce more than one output and one of them isn't used.
                    // Ex: Transparent pass in HDRP that writes to the color buffer and motion vectors.
                    // If TAA/MotionBlur aren't used, the movecs are never read after the transparent pass and it would raise this error.
                    // Because of that, it's hard to make this an actual error.
                    // Commented out code to check such cases if needed.
                    //if (latestValidReadIndex != -1 && (latestValidWriteIndex > latestValidReadIndex))
                    //{
                    //    var name = m_Resources.GetRenderGraphResourceName((RenderGraphResourceType)type, i);
                    //    var lastPassReadName = m_CompiledPassInfos[latestValidReadIndex].pass.name;
                    //    var lastPassWriteName = m_CompiledPassInfos[latestValidWriteIndex].pass.name;
                    //    Debug.LogError($"Resource {name} is written again after the last pass that reads it.\nLast pass read: {lastPassReadName}\nLast pass write: {lastPassWriteName}");
                    //}

                    // For not imported resources, make sure we don't try to release them if they were never created (due to culling).
                    bool shouldRelease = !(firstWriteIndex == -1 && !resourceInfo.imported);
                    int lastReadPassIndex = shouldRelease ? Math.Max(latestValidWriteIndex, latestValidReadIndex) : -1;

                    // Texture release
                    if (lastReadPassIndex != -1)
                    {
                        // In case of async passes, we need to extend lifetime of resource to the first pass on the graphics pipeline that wait for async passes to be over.
                        // Otherwise, if we freed the resource right away during an async pass, another non async pass could reuse the resource even though the async pipe is not done.
                        if (compiledPassInfo[lastReadPassIndex].enableAsyncCompute)
                        {
                            int currentPassIndex = lastReadPassIndex;
                            int firstWaitingPassIndex = compiledPassInfo[currentPassIndex].syncFromPassIndex;
                            // Find the first async pass that is synchronized by the graphics pipeline (ie: passInfo.syncFromPassIndex != -1)
                            while (firstWaitingPassIndex == -1 && currentPassIndex++ < compiledPassInfo.size - 1)
                            {
                                if (compiledPassInfo[currentPassIndex].enableAsyncCompute)
                                    firstWaitingPassIndex = compiledPassInfo[currentPassIndex].syncFromPassIndex;
                            }

                            // Fail safe in case render graph is badly formed.
                            if (currentPassIndex == compiledPassInfo.size)
                            {
                                // This is not true with passes with side effect as they are writing to a resource that may not be read by the render graph this frame and to no other resource.
                                // In this case we extend the lifetime of resources to the end of the frame. It's not idea but it should not be the majority of cases.
                                if (compiledPassInfo[lastReadPassIndex].hasSideEffect)
                                {
                                    firstWaitingPassIndex = currentPassIndex;
                                }
                                else
                                {
                                    RenderGraphPass invalidPass = m_RenderPasses[lastReadPassIndex];

                                    var resName = "<unknown>";
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                                    resName = m_Resources.GetRenderGraphResourceName((RenderGraphResourceType)type, i);
#endif
                                    var msg = $"{(RenderGraphResourceType)type} resource '{resName}' in asynchronous pass '{invalidPass.name}' is missing synchronization on the graphics pipeline.";
                                    throw new InvalidOperationException(msg);
                                }
                            }

                            // Finally add the release command to the pass before the first pass that waits for the compute pipe.
                            var releasePassIndex = Math.Max(0, firstWaitingPassIndex - 1);

                            // Check to ensure that we do not release resources on a culled pass (causes a leak otherwise).
                            while (compiledPassInfo[releasePassIndex].culled)
                                releasePassIndex = Math.Max(0, releasePassIndex - 1);

                            ref CompiledPassInfo passInfo = ref compiledPassInfo[releasePassIndex];
                            passInfo.resourceReleaseList[type].Add(i);
                        }
                        else
                        {
                            ref CompiledPassInfo passInfo = ref compiledPassInfo[lastReadPassIndex];
                            passInfo.resourceReleaseList[type].Add(i);
                        }
                    }

                    if (sharedResource && (firstWriteIndex != -1 || lastReadPassIndex != -1)) // A shared resource is considered used if it's either read or written at any pass.
                    {
                        m_Resources.UpdateSharedResourceLastFrameIndex(type, i);
                    }
                }
            }
        }

        void UpdateAllSharedResourceLastFrameIndex()
        {
            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                var resourceInfos = m_CurrentCompiledGraph.compiledResourcesInfos[type];
                var sharedResourceCount = m_Resources.GetSharedResourceCount((RenderGraphResourceType)type);

                for (int i = 1; i <= sharedResourceCount; ++i) // 0 == null resource skip it
                {
                    CompiledResourceInfo resourceInfo = resourceInfos[i];
                    var latestValidReadIndex = GetLatestValidReadIndex(resourceInfo);
                    int firstWriteIndex = GetFirstValidWriteIndex(resourceInfo);

                    if ((firstWriteIndex != -1 || latestValidReadIndex != -1)) // A shared resource is considered used if it's either read or written at any pass.
                    {
                        m_Resources.UpdateSharedResourceLastFrameIndex(type, i);
                    }
                }
            }
        }

        bool AreRendererListsEmpty(List<RendererListHandle> rendererLists)
        {
            // Anything related to renderer lists needs a real context to be able to use/test it
            Debug.Assert(m_RenderGraphContext.contextlessTesting == false);

            foreach (RendererListHandle handle in rendererLists)
            {
                var rendererList = m_Resources.GetRendererList(handle);
                if (m_RenderGraphContext.renderContext.QueryRendererListStatus(rendererList) == RendererListStatus.kRendererListPopulated)
                {
                    return false;
                }
            }

            // If the list of RendererLists is empty, then the default behavior is to not cull, so return false.
            return rendererLists.Count > 0 ? true : false;
        }

        void TryCullPassAtIndex(int passIndex)
        {
            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            ref var compiledPass = ref compiledPassInfo[passIndex];
            var pass = m_RenderPasses[passIndex];
            if (!compiledPass.culled &&
                pass.allowPassCulling &&
                pass.allowRendererListCulling &&
                !compiledPass.hasSideEffect)
            {
                if (AreRendererListsEmpty(pass.usedRendererListList))
                {
                    //Debug.Log($"Culling pass <color=red> {pass.name} </color>");
                    compiledPass.culled = compiledPass.culledByRendererList = true;
                }
            }
        }

        void CullRendererLists()
        {
            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            for (int passIndex = 0; passIndex < compiledPassInfo.size; ++passIndex)
            {
                var compiledPass = compiledPassInfo[passIndex];

                if (!compiledPass.culled && !compiledPass.hasSideEffect)
                {
                    var pass = m_RenderPasses[passIndex];
                    if (pass.usedRendererListList.Count > 0)
                    {
                        TryCullPassAtIndex(passIndex);
                    }
                }
            }
        }

        bool UpdateCurrentCompiledGraph(int graphHash, bool forceNoCaching = false)
        {
            bool cached = false;
            if (m_EnableCompilationCaching && !forceNoCaching)
                cached = m_CompilationCache.GetCompilationCache(graphHash, m_ExecutionCount, out m_CurrentCompiledGraph);
            else
                m_CurrentCompiledGraph = m_DefaultCompiledGraph;

            return cached;
        }

        // Internal visibility for testing purpose only
        // Traverse the render graph:
        // - Determines when resources are created/released
        // - Determines async compute pass synchronization
        // - Cull unused render passes.
        internal void CompileRenderGraph(int graphHash)
        {
            using (new ProfilingScope(m_RenderGraphContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.CompileRenderGraph)))
            {
                bool compilationIsCached = UpdateCurrentCompiledGraph(graphHash);

                if (!compilationIsCached)
                {
                    m_CurrentCompiledGraph.Clear();
                    m_CurrentCompiledGraph.InitializeCompilationData(m_RenderPasses, m_Resources);
                    CountReferences();

                    // First cull all passes that produce unused output
                    CullUnusedPasses();
                }

                // Create the renderer lists of the remaining passes
                CreateRendererLists();

                if (!compilationIsCached)
                {
                    // Cull dynamically the graph passes based on the renderer list visibility
                    if (m_RendererListCulling)
                        CullRendererLists();

                    // After all culling passes, allocate the resources for this frame
                    UpdateResourceAllocationAndSynchronization();
                }
                else
                {
                    // We need to update all shared resource frame index usage otherwise they might not be in a valid state.
                    // Otherwise it's done in UpdateResourceAllocationAndSynchronization().
                    UpdateAllSharedResourceLastFrameIndex();
                }

                LogRendererListsCreation();
            }
        }

        ref CompiledPassInfo CompilePassImmediatly(RenderGraphPass pass)
        {
            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            // If we don't have enough pre allocated elements we double the size.
            // It's pretty aggressive but the immediate mode is only for debug purpose so it should be fine.
            if (m_CurrentImmediatePassIndex >= compiledPassInfo.size)
                compiledPassInfo.Resize(compiledPassInfo.size * 2);

            ref CompiledPassInfo passInfo = ref compiledPassInfo[m_CurrentImmediatePassIndex++];
            passInfo.Reset(pass, m_CurrentImmediatePassIndex - 1);
            // In immediate mode we don't have proper information to generate synchronization so we disable async compute.
            passInfo.enableAsyncCompute = false;

            // In immediate mode, we don't have any resource usage information so we'll just create resources whenever they are written to if not already alive.
            // We will release all resources at the end of the render graph execution.
            for (int iType = 0; iType < (int)RenderGraphResourceType.Count; ++iType)
            {
                foreach (var res in pass.transientResourceList[iType])
                {
                    passInfo.resourceCreateList[iType].Add(res.index);
                    passInfo.resourceReleaseList[iType].Add(res.index);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    passInfo.debugResourceWrites[iType].Add(m_Resources.GetRenderGraphResourceName(res));
                    passInfo.debugResourceReads[iType].Add(m_Resources.GetRenderGraphResourceName(res));
#endif
                }

                foreach (var res in pass.resourceWriteLists[iType])
                {
                    if (pass.transientResourceList[iType].Contains(res))
                        continue; // Prevent registering writes to transient texture twice

                    if (!m_Resources.IsGraphicsResourceCreated(res))
                    {
                        passInfo.resourceCreateList[iType].Add(res.index);
                        m_ImmediateModeResourceList[iType].Add(res.index);
                    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    passInfo.debugResourceWrites[iType].Add(m_Resources.GetRenderGraphResourceName(res));
#endif
                }

                foreach (var res in pass.resourceReadLists[iType])
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    passInfo.debugResourceReads[iType].Add(m_Resources.GetRenderGraphResourceName(res));
#endif
                }
            }

            // Create the necessary renderer lists
            foreach (var rl in pass.usedRendererListList)
            {
                if (!m_Resources.IsRendererListCreated(rl))
                    m_RendererLists.Add(rl);
            }
            // Anything related to renderer lists needs a real context to be able to use/test it
            Debug.Assert(m_RenderGraphContext.contextlessTesting == false);
            m_Resources.CreateRendererLists(m_RendererLists, m_RenderGraphContext.renderContext);
            m_RendererLists.Clear();

            return ref passInfo;
        }

        void ExecutePassImmediately(RenderGraphPass pass)
        {
            ExecuteCompiledPass(ref CompilePassImmediatly(pass));
        }

        void ExecuteCompiledPass(ref CompiledPassInfo passInfo)
        {
            if (passInfo.culled)
                return;

            var pass = m_RenderPasses[passInfo.index];

            if (!pass.HasRenderFunc())
                throw new InvalidOperationException($"RenderPass {pass.name} was not provided with an execute function.");

            try
            {
                using (new ProfilingScope(m_RenderGraphContext.cmd, pass.customSampler))
                {
                    LogRenderPassBegin(passInfo);
                    using (new RenderGraphLogIndent(m_FrameInformationLogger))
                    {
                        m_RenderGraphContext.executingPass = pass;
                        PreRenderPassExecute(passInfo, pass, m_RenderGraphContext);
                        pass.Execute(m_RenderGraphContext);
                        PostRenderPassExecute(ref passInfo, pass, m_RenderGraphContext);
                    }
                }
            }
            catch (Exception e)
            {
                // Dont log errors during tests
                if (m_RenderGraphContext.contextlessTesting == false)
                {
                    // Log exception from the pass that raised it to have improved error logging quality for users
                    m_ExecutionExceptionWasRaised = true;
                    Debug.LogError($"Render Graph execution error at pass '{pass.name}' ({passInfo.index})");
                    Debug.LogException(e);
                }
                throw;
            }
        }

        // Execute the compiled render graph
        void ExecuteRenderGraph()
        {
            using (new ProfilingScope(m_RenderGraphContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.ExecuteRenderGraph)))
            {
                var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
                for (int passIndex = 0; passIndex < compiledPassInfo.size; ++passIndex)
                {
                    ExecuteCompiledPass(ref compiledPassInfo[passIndex]);
                }
            }
        }

        void PreRenderPassSetRenderTargets(in CompiledPassInfo passInfo, RenderGraphPass pass, InternalRenderGraphContext rgContext)
        {
            if (passInfo.hasShadingRateImage)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (!pass.shadingRateAccess.textureHandle.IsValid())
                {
                    throw new InvalidOperationException("MRT setup with invalid variable rate shading images.");
                }
#endif

                CoreUtils.SetShadingRateImage(rgContext.cmd, m_Resources.GetTexture(pass.shadingRateAccess.textureHandle));
            }

            var depthBufferIsValid = pass.depthAccess.textureHandle.IsValid();
            if (depthBufferIsValid || pass.colorBufferMaxIndex != -1)
            {
                var colorBufferAccess = pass.colorBufferAccess;
                if (pass.colorBufferMaxIndex > 0)
                {
                    var mrtArray = m_TempMRTArrays[pass.colorBufferMaxIndex];

                    for (int i = 0; i <= pass.colorBufferMaxIndex; ++i)
                    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        if (!colorBufferAccess[i].textureHandle.IsValid())
                            throw new InvalidOperationException("MRT setup is invalid. Some indices are not used.");
#endif
                        mrtArray[i] = m_Resources.GetTexture(colorBufferAccess[i].textureHandle);
                    }

                    if (depthBufferIsValid)
                    {
                        CoreUtils.SetRenderTarget(rgContext.cmd, mrtArray, m_Resources.GetTexture(pass.depthAccess.textureHandle));
                    }
                    else
                    {
                        throw new InvalidOperationException("Setting MRTs without a depth buffer is not supported.");
                    }
                }
                else
                {
                    if (depthBufferIsValid)
                    {
                        if (pass.colorBufferMaxIndex > -1)
                        {
                            CoreUtils.SetRenderTarget(rgContext.cmd, m_Resources.GetTexture(pass.colorBufferAccess[0].textureHandle),
                                m_Resources.GetTexture(pass.depthAccess.textureHandle));
                        }
                        else
                        {
                            CoreUtils.SetRenderTarget(rgContext.cmd, m_Resources.GetTexture(pass.depthAccess.textureHandle));
                        }
                    }
                    else
                    {
                        if (pass.colorBufferAccess[0].textureHandle.IsValid())
                        {
                            CoreUtils.SetRenderTarget(rgContext.cmd, m_Resources.GetTexture(pass.colorBufferAccess[0].textureHandle));
                        }
                        else
                            throw new InvalidOperationException("Neither Depth nor color render targets are correctly setup at pass " + pass.name + ".");
                    }
                }
            }
        }

        void PreRenderPassExecute(in CompiledPassInfo passInfo, RenderGraphPass pass, InternalRenderGraphContext rgContext)
        {
            // Need to save the command buffer to restore it later as the one in the context can changed if running a pass async.
            m_PreviousCommandBuffer = rgContext.cmd;

            bool executedWorkDuringResourceCreation = false;
            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                foreach (int resource in passInfo.resourceCreateList[type])
                {
                    executedWorkDuringResourceCreation |= m_Resources.CreatePooledResource(rgContext, type, resource);
                }
            }

            if (passInfo.enableFoveatedRasterization)
                rgContext.cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);

            if (passInfo.hasShadingRateStates)
            {
                rgContext.cmd.SetShadingRateFragmentSize(pass.shadingRateFragmentSize);
                rgContext.cmd.SetShadingRateCombiner(ShadingRateCombinerStage.Primitive, pass.primitiveShadingRateCombiner);
                rgContext.cmd.SetShadingRateCombiner(ShadingRateCombinerStage.Fragment, pass.fragmentShadingRateCombiner);
            }

            PreRenderPassSetRenderTargets(passInfo, pass, rgContext);

            if (passInfo.enableAsyncCompute)
            {
                GraphicsFence previousFence = new GraphicsFence();
                if (executedWorkDuringResourceCreation)
                {
                    previousFence = rgContext.cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.AllGPUOperations);
                }

                // Flush current command buffer on the render context before enqueuing async commands.
                if (rgContext.contextlessTesting == false)
                    rgContext.renderContext.ExecuteCommandBuffer(rgContext.cmd);
                rgContext.cmd.Clear();

                CommandBuffer asyncCmd = CommandBufferPool.Get(pass.name);
                asyncCmd.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
                rgContext.cmd = asyncCmd;

                if (executedWorkDuringResourceCreation)
                {
                    rgContext.cmd.WaitOnAsyncGraphicsFence(previousFence);
                }
            }

            // Synchronize with graphics or compute pipe if needed.
            if (passInfo.syncToPassIndex != -1)
            {
                rgContext.cmd.WaitOnAsyncGraphicsFence(m_CurrentCompiledGraph.compiledPassInfos[passInfo.syncToPassIndex].fence);
            }
        }

        void PostRenderPassExecute(ref CompiledPassInfo passInfo, RenderGraphPass pass, InternalRenderGraphContext rgContext)
        {
            if (passInfo.hasShadingRateStates || passInfo.hasShadingRateImage)
                rgContext.cmd.ResetShadingRate();

            foreach (var tex in pass.setGlobalsList)
            {
                rgContext.cmd.SetGlobalTexture(tex.Item2, tex.Item1);
            }

            if (passInfo.needGraphicsFence)
                passInfo.fence = rgContext.cmd.CreateAsyncGraphicsFence();

            if (passInfo.enableAsyncCompute)
            {
                // Anything related to async command buffer execution needs a real context to be able to use/test it
                // As the async will likely be waited for but never finish if there is no real context
                Debug.Assert(m_RenderGraphContext.contextlessTesting == false);

                // The command buffer has been filled. We can kick the async task.
                rgContext.renderContext.ExecuteCommandBufferAsync(rgContext.cmd, ComputeQueueType.Background);
                CommandBufferPool.Release(rgContext.cmd);
                rgContext.cmd = m_PreviousCommandBuffer; // Restore the main command buffer.
            }

            if (passInfo.enableFoveatedRasterization)
                rgContext.cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);

            m_RenderGraphPool.ReleaseAllTempAlloc();

            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                foreach (var resource in passInfo.resourceReleaseList[type])
                {
                    m_Resources.ReleasePooledResource(rgContext, type, resource);
                }
            }
        }

        void ClearRenderPasses()
        {
            foreach (var pass in m_RenderPasses)
                pass.Release(m_RenderGraphPool);
            m_RenderPasses.Clear();
        }

        void ReleaseImmediateModeResources()
        {
            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                foreach (var resource in m_ImmediateModeResourceList[type])
                {
                    m_Resources.ReleasePooledResource(m_RenderGraphContext, type, resource);
                }
            }
        }

        void LogFrameInformation()
        {
            if (m_DebugParameters.enableLogging)
            {
                m_FrameInformationLogger.LogLine($"==== Staring render graph frame for: {m_CurrentExecutionName} ====");

                if (!m_DebugParameters.immediateMode)
                    m_FrameInformationLogger.LogLine("Number of passes declared: {0}\n", m_RenderPasses.Count);
            }
        }

        void LogRendererListsCreation()
        {
            if (m_DebugParameters.enableLogging)
            {
                m_FrameInformationLogger.LogLine("Number of renderer lists created: {0}\n", m_RendererLists.Count);
            }
        }

        void LogRenderPassBegin(in CompiledPassInfo passInfo)
        {
            if (m_DebugParameters.enableLogging)
            {
                RenderGraphPass pass = m_RenderPasses[passInfo.index];

                m_FrameInformationLogger.LogLine("[{0}][{1}] \"{2}\"", pass.index, pass.enableAsyncCompute ? "Compute" : "Graphics", pass.name);
                using (new RenderGraphLogIndent(m_FrameInformationLogger))
                {
                    if (passInfo.syncToPassIndex != -1)
                        m_FrameInformationLogger.LogLine("Synchronize with [{0}]", passInfo.syncToPassIndex);
                }
            }
        }

        void LogCulledPasses()
        {
            if (m_DebugParameters.enableLogging)
            {
                m_FrameInformationLogger.LogLine("Pass Culling Report:");
                using (new RenderGraphLogIndent(m_FrameInformationLogger))
                {
                    var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
                    for (int i = 0; i < compiledPassInfo.size; ++i)
                    {
                        if (compiledPassInfo[i].culled)
                        {
                            var pass = m_RenderPasses[i];
                            m_FrameInformationLogger.LogLine("[{0}] {1}", pass.index, pass.name);
                        }
                    }
                    m_FrameInformationLogger.LogLine("\n");
                }
            }
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

        void UpdateImportedResourceLifeTime(ref DebugData.ResourceData data, List<int> passList)
        {
            foreach (var pass in passList)
            {
                if (data.creationPassIndex == -1)
                    data.creationPassIndex = pass;
                else
                    data.creationPassIndex = Math.Min(data.creationPassIndex, pass);

                if (data.releasePassIndex == -1)
                    data.releasePassIndex = pass;
                else
                    data.releasePassIndex = Math.Max(data.releasePassIndex, pass);
            }
        }

        void GenerateDebugData()
        {
            if (m_ExecutionExceptionWasRaised)
                return;

            if (!isRenderGraphViewerActive)
            {
                CleanupDebugData();
                return;
            }

            if (!m_DebugData.TryGetValue(m_CurrentExecutionName, out var debugData))
            {
                onExecutionRegistered?.Invoke(this, m_CurrentExecutionName);
                debugData = new DebugData();
                m_DebugData.Add(m_CurrentExecutionName, debugData);
                return; // Generate the debug data on the next frame, because script metadata is collected during recording step
            }

            // Only generate debug data on request
            if (m_CaptureDebugDataForExecution == null || !m_CaptureDebugDataForExecution.Equals(m_CurrentExecutionName))
                return;

            debugData.Clear();

            if (nativeRenderPassesEnabled)
                nativeCompiler.GenerateNativeCompilerDebugData(ref debugData);
            else
                GenerateCompilerDebugData(ref debugData);

            onDebugDataCaptured?.Invoke();

            m_CaptureDebugDataForExecution = null;

            ClearPassDebugMetadata();
        }

        void GenerateCompilerDebugData(ref DebugData debugData)
        {
            var compiledPassInfo = m_CurrentCompiledGraph.compiledPassInfos;
            var compiledResourceInfo = m_CurrentCompiledGraph.compiledResourcesInfos;

            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                for (int i = 0; i < compiledResourceInfo[type].size; ++i)
                {
                    ref var resourceInfo = ref compiledResourceInfo[type][i];
                    DebugData.ResourceData newResource = new DebugData.ResourceData();
                    if (i != 0)
                    {
                        var resName = m_Resources.GetRenderGraphResourceName((RenderGraphResourceType)type, i);
                        newResource.name = !string.IsNullOrEmpty(resName) ? resName : "(unnamed)";
                        newResource.imported = m_Resources.IsRenderGraphResourceImported((RenderGraphResourceType)type, i);
                    }
                    else
                    {
                        // The above functions will throw exceptions when used with the null argument so just use a dummy instead
                        newResource.name = "<null>";
                        newResource.imported = true;
                    }

                    newResource.creationPassIndex = -1;
                    newResource.releasePassIndex = -1;

                    RenderGraphResourceType resourceType = (RenderGraphResourceType) type;
                    var handle = new ResourceHandle(i, resourceType, false);
                    if (i != 0 && handle.IsValid())
                    {
                        if (resourceType == RenderGraphResourceType.Texture)
                        {
                            m_Resources.GetRenderTargetInfo(handle, out var renderTargetInfo);

                            var textureData = new DebugData.TextureResourceData();
                            textureData.width = renderTargetInfo.width;
                            textureData.height = renderTargetInfo.height;
                            textureData.depth = renderTargetInfo.volumeDepth;
                            textureData.samples = renderTargetInfo.msaaSamples;
                            textureData.format = renderTargetInfo.format;
                            textureData.bindMS = renderTargetInfo.bindMS;

                            newResource.textureData = textureData;
                        }
                        else if (resourceType == RenderGraphResourceType.Buffer)
                        {
                            var bufferDesc = m_Resources.GetBufferResourceDesc(handle, true);

                            var bufferData = new DebugData.BufferResourceData();
                            bufferData.count = bufferDesc.count;
                            bufferData.stride = bufferDesc.stride;
                            bufferData.target = bufferDesc.target;
                            bufferData.usage = bufferDesc.usageFlags;

                            newResource.bufferData = bufferData;
                        }
                    }

                    newResource.consumerList = new List<int>(resourceInfo.consumers);
                    newResource.producerList = new List<int>(resourceInfo.producers);

                    if (newResource.imported)
                    {
                        UpdateImportedResourceLifeTime(ref newResource, newResource.consumerList);
                        UpdateImportedResourceLifeTime(ref newResource, newResource.producerList);
                    }

                    debugData.resourceLists[type].Add(newResource);
                }
            }

            for (int i = 0; i < compiledPassInfo.size; ++i)
            {
                ref CompiledPassInfo passInfo = ref compiledPassInfo[i];
                RenderGraphPass pass = m_RenderPasses[passInfo.index];

                DebugData.PassData newPass = new DebugData.PassData();
                newPass.name = pass.name;
                newPass.type = pass.type;
                newPass.culled = passInfo.culled;
                newPass.async = passInfo.enableAsyncCompute;
                newPass.generateDebugData = pass.generateDebugData;
                newPass.resourceReadLists = new List<int>[(int)RenderGraphResourceType.Count];
                newPass.resourceWriteLists = new List<int>[(int)RenderGraphResourceType.Count];
                newPass.syncFromPassIndex = passInfo.syncFromPassIndex;
                newPass.syncToPassIndex = passInfo.syncToPassIndex;

                DebugData.s_PassScriptMetadata.TryGetValue(pass, out newPass.scriptInfo);

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    newPass.resourceReadLists[type] = new List<int>();
                    newPass.resourceWriteLists[type] = new List<int>();

                    foreach (var resourceRead in pass.resourceReadLists[type])
                        newPass.resourceReadLists[type].Add(resourceRead.index);
                    foreach (var resourceWrite in pass.resourceWriteLists[type])
                        newPass.resourceWriteLists[type].Add(resourceWrite.index);

                    foreach (var resourceCreate in passInfo.resourceCreateList[type])
                    {
                        var res = debugData.resourceLists[type][resourceCreate];
                        if (res.imported)
                            continue;
                        res.creationPassIndex = i;
                        debugData.resourceLists[type][resourceCreate] = res;
                    }

                    foreach (var resourceRelease in passInfo.resourceReleaseList[type])
                    {
                        var res = debugData.resourceLists[type][resourceRelease];
                        if (res.imported)
                            continue;
                        res.releasePassIndex = i;
                        debugData.resourceLists[type][resourceRelease] = res;
                    }
                }

                debugData.passList.Add(newPass);
            }
        }

        void CleanupDebugData()
        {
            foreach (var kvp in m_DebugData)
            {
                onExecutionUnregistered?.Invoke(this, kvp.Key);
            }

            m_DebugData.Clear();
        }

        #endregion


        Dictionary<int, TextureHandle> registeredGlobals = new Dictionary<int, TextureHandle>();

        internal void SetGlobal(TextureHandle h, int globalPropertyId)
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
