using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Sets the read and write access for the depth buffer.
    /// </summary>
    [Flags]
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
    /// This class specifies the context given to every render pass.
    /// </summary>
    public class RenderGraphContext
    {
        ///<summary>Scriptable Render Context used for rendering.</summary>
        public ScriptableRenderContext      renderContext;
        ///<summary>Command Buffer used for rendering.</summary>
        public CommandBuffer                cmd;
        ///<summary>Render Graph pooll used for temporary data.</summary>
        public RenderGraphObjectPool        renderGraphPool;
        ///<summary>Render Graph default resources.</summary>
        public RenderGraphDefaultResources  defaultResources;
    }

    /// <summary>
    /// This struct contains properties which control the execution of the Render Graph.
    /// </summary>
    public struct RenderGraphParameters
    {
        ///<summary>Index of the current frame being rendered.</summary>
        public int currentFrameIndex;
        ///<summary>Scriptable Render Context used by the render pipeline.</summary>
        public ScriptableRenderContext scriptableRenderContext;
        ///<summary>Command Buffer used to execute graphic commands.</summary>
        public CommandBuffer commandBuffer;
    }

    class RenderGraphDebugParams
    {
        public bool clearRenderTargetsAtCreation;
        public bool clearRenderTargetsAtRelease;
        public bool disablePassCulling;
        public bool immediateMode;
        public bool logFrameInformation;
        public bool logResources;

        public void RegisterDebug(string name)
        {
            var list = new List<DebugUI.Widget>();
            list.Add(new DebugUI.BoolField { displayName = "Clear Render Targets at creation", getter = () => clearRenderTargetsAtCreation, setter = value => clearRenderTargetsAtCreation = value });
            // We cannot expose this option as it will change the active render target and the debug menu won't know where to render itself anymore.
        //    list.Add(new DebugUI.BoolField { displayName = "Clear Render Targets at release", getter = () => clearRenderTargetsAtRelease, setter = value => clearRenderTargetsAtRelease = value });
            list.Add(new DebugUI.BoolField { displayName = "Disable Pass Culling", getter = () => disablePassCulling, setter = value => disablePassCulling = value });
            list.Add(new DebugUI.BoolField { displayName = "Immediate Mode", getter = () => immediateMode, setter = value => immediateMode = value });
            list.Add(new DebugUI.Button { displayName = "Log Frame Information",
                action = () =>
                {
                    logFrameInformation = true;
                #if UNITY_EDITOR
                    UnityEditor.SceneView.RepaintAll();
                #endif
                }
            });
            list.Add(new DebugUI.Button { displayName = "Log Resources",
                action = () =>
                {
                    logResources = true;
                #if UNITY_EDITOR
                    UnityEditor.SceneView.RepaintAll();
                #endif
                }
            });

            var panel = DebugManager.instance.GetPanel(name.Length == 0 ? "Render Graph" : name, true);
            panel.children.Add(list.ToArray());
        }

        public void UnRegisterDebug(string name)
        {
            DebugManager.instance.RemovePanel(name.Length == 0 ? "Render Graph" : name);
        }
    }

    /// <summary>
    /// The Render Pass rendering delegate.
    /// </summary>
    /// <typeparam name="PassData">The type of the class used to provide data to the Render Pass.</typeparam>
    /// <param name="data">Render Pass specific data.</param>
    /// <param name="renderGraphContext">Global Render Graph context.</param>
    public delegate void RenderFunc<PassData>(PassData data, RenderGraphContext renderGraphContext) where PassData : class, new();

    internal class RenderGraphDebugData
    {
        [DebuggerDisplay("PassDebug: {name}")]
        public struct PassDebugData
        {
            public string name;
            public List<int>[] resourceReadLists;
            public List<int>[] resourceWriteLists;
            public bool culled;
            // We have this member instead of removing the pass altogether because we need the full list of passes in order to be able to remap them correctly when we remove them from display in the viewer.
            public bool generateDebugData;
        }

        [DebuggerDisplay("ResourceDebug: {name} [{creationPassIndex}:{releasePassIndex}]")]
        public struct ResourceDebugData
        {
            public string name;
            public bool imported;
            public int creationPassIndex;
            public int releasePassIndex;

            public List<int> consumerList;
            public List<int> producerList;
        }

        public List<PassDebugData> passList = new List<PassDebugData>();
        public List<ResourceDebugData>[] resourceLists = new List<ResourceDebugData>[(int)RenderGraphResourceType.Count];

        public void Clear()
        {
            passList.Clear();

            // Create if needed
            if (resourceLists[0] == null)
            {
                for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                    resourceLists[i] = new List<ResourceDebugData>();
            }

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                resourceLists[i].Clear();
        }
    }

    /// <summary>
    /// This class is the main entry point of the Render Graph system.
    /// </summary>
    public class RenderGraph
    {
        ///<summary>Maximum number of MRTs supported by Render Graph.</summary>
        public static readonly int kMaxMRTCount = 8;

        internal struct CompiledResourceInfo
        {
            public List<int>    producers;
            public List<int>    consumers;
            public bool         resourceCreated;
            public int          refCount;

            public void Reset()
            {
                if (producers == null)
                    producers = new List<int>();
                if (consumers == null)
                    consumers = new List<int>();

                producers.Clear();
                consumers.Clear();
                resourceCreated = false;
                refCount = 0;
            }
        }

        [DebuggerDisplay("RenderPass: {pass.name} (Index:{pass.index} Async:{enableAsyncCompute})")]
        internal struct CompiledPassInfo
        {
            public RenderGraphPass  pass;
            public List<int>[]      resourceCreateList;
            public List<int>[]      resourceReleaseList;
            public int              refCount;
            public bool             culled;
            public bool             hasSideEffect;
            public int              syncToPassIndex; // Index of the pass that needs to be waited for.
            public int              syncFromPassIndex; // Smaller pass index that waits for this pass.
            public bool             needGraphicsFence;
            public GraphicsFence    fence;

            public bool             enableAsyncCompute;
            public bool             allowPassCulling { get { return pass.allowPassCulling; } }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // This members are only here to ease debugging.
            public List<string>[]   debugResourceReads;
            public List<string>[]   debugResourceWrites;
#endif

            public void Reset(RenderGraphPass pass)
            {
                this.pass = pass;
                enableAsyncCompute = pass.enableAsyncCompute;

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

        RenderGraphResourceRegistry             m_Resources;
        RenderGraphObjectPool                   m_RenderGraphPool = new RenderGraphObjectPool();
        List<RenderGraphPass>                   m_RenderPasses = new List<RenderGraphPass>(64);
        List<RendererListHandle>                m_RendererLists = new List<RendererListHandle>(32);
        RenderGraphDebugParams                  m_DebugParameters = new RenderGraphDebugParams();
        RenderGraphLogger                       m_Logger = new RenderGraphLogger();
        RenderGraphDefaultResources             m_DefaultResources = new RenderGraphDefaultResources();
        Dictionary<int, ProfilingSampler>       m_DefaultProfilingSamplers = new Dictionary<int, ProfilingSampler>();
        bool                                    m_ExecutionExceptionWasRaised;
        RenderGraphContext                      m_RenderGraphContext = new RenderGraphContext();
        CommandBuffer                           m_PreviousCommandBuffer;
        int                                     m_CurrentImmediatePassIndex;
        List<int>[]                             m_ImmediateModeResourceList = new List<int>[(int)RenderGraphResourceType.Count];

        // Compiled Render Graph info.
        DynamicArray<CompiledResourceInfo>[]    m_CompiledResourcesInfos = new DynamicArray<CompiledResourceInfo>[(int)RenderGraphResourceType.Count];
        DynamicArray<CompiledPassInfo>          m_CompiledPassInfos = new DynamicArray<CompiledPassInfo>();
        Stack<int>                              m_CullingStack = new Stack<int>();

        int                                     m_ExecutionCount;
        RenderGraphDebugData                    m_RenderGraphDebugData = new RenderGraphDebugData();

        // Global list of living render graphs
        static List<RenderGraph>                s_RegisteredGraphs = new List<RenderGraph>();

        #region Public Interface
        /// <summary>Name of the Render Graph.</summary>
        public string name { get; private set; } = "RenderGraph";
        /// <summary>If true, the Render Graph will generate execution debug information.</summary>
        internal static bool requireDebugData { get; set; } = false;

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
            m_Resources = new RenderGraphResourceRegistry(m_DebugParameters, m_Logger);

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                m_CompiledResourcesInfos[i] = new DynamicArray<CompiledResourceInfo>();
            }

            m_DebugParameters.RegisterDebug(this.name);

            s_RegisteredGraphs.Add(this);
            onGraphRegistered?.Invoke(this);
        }

        /// <summary>
        /// Cleanup the Render Graph.
        /// </summary>
        public void Cleanup()
        {
            m_DebugParameters.UnRegisterDebug(this.name);
            m_Resources.Cleanup();
            m_DefaultResources.Cleanup();
            m_RenderGraphPool.Cleanup();

            s_RegisteredGraphs.Remove(this);
            onGraphUnregistered?.Invoke(this);
        }

        /// <summary>
        /// Returns the last rendered frame debug data. Can be null if requireDebugData is set to false.
        /// </summary>
        /// <returns>The last rendered frame debug data</returns>
        internal RenderGraphDebugData GetDebugData()
        {
            return m_RenderGraphDebugData;
        }

        /// <summary>
        /// End frame processing. Purge resources that have been used since last frame and resets internal states.
        /// This need to be called once per frame.
        /// </summary>
        public void EndFrame()
        {
            m_Resources.PurgeUnusedResources();
            m_DebugParameters.logFrameInformation = false;
            m_DebugParameters.logResources = false;
        }

        /// <summary>
        /// Import an external texture to the Render Graph.
        /// Any pass writing to an imported texture will be considered having side effects and can't be automatically culled.
        /// </summary>
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle ImportTexture(RTHandle rt)
        {
            return m_Resources.ImportTexture(rt);
        }

        /// <summary>
        /// Import the final backbuffer to render graph.
        /// </summary>
        /// <param name="rt">Backbuffer render target identifier.</param>
        /// <returns>A new TextureHandle for the backbuffer.</returns>
        public TextureHandle ImportBackbuffer(RenderTargetIdentifier rt)
        {
            return m_Resources.ImportBackbuffer(rt);
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
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <returns>A new TextureHandle.</returns>
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
        /// Creates a new Renderer List Render Graph resource.
        /// </summary>
        /// <param name="desc">Renderer List descriptor.</param>
        /// <returns>A new TextureHandle.</returns>
        public RendererListHandle CreateRendererList(in RendererListDesc desc)
        {
            return m_Resources.CreateRendererList(desc);
        }

        /// <summary>
        /// Import an external Compute Buffer to the Render Graph
        /// Any pass writing to an imported compute buffer will be considered having side effects and can't be automatically culled.
        /// </summary>
        /// <param name="computeBuffer">External Compute Buffer that needs to be imported.</param>
        /// <returns>A new ComputeBufferHandle.</returns>
        public ComputeBufferHandle ImportComputeBuffer(ComputeBuffer computeBuffer)
        {
            return m_Resources.ImportComputeBuffer(computeBuffer);
        }

        /// <summary>
        /// Create a new Render Graph Compute Buffer resource.
        /// </summary>
        /// <param name="desc">Compute Buffer descriptor.</param>
        /// <returns>A new ComputeBufferHandle.</returns>
        public ComputeBufferHandle CreateComputeBuffer(in ComputeBufferDesc desc)
        {
            return m_Resources.CreateComputeBuffer(desc);
        }

        /// <summary>
        /// Create a new Render Graph Compute Buffer resource using the descriptor from another compute buffer.
        /// </summary>
        /// <param name="computeBuffer">Compute Buffer from which the descriptor should be used.</param>
        /// <returns>A new ComputeBufferHandle.</returns>
        public ComputeBufferHandle CreateComputeBuffer(in ComputeBufferHandle computeBuffer)
        {
            return m_Resources.CreateComputeBuffer(m_Resources.GetComputeBufferResourceDesc(computeBuffer.handle));
        }

        /// <summary>
        /// Gets the descriptor of the specified Compute Buffer resource.
        /// </summary>
        /// <param name="computeBuffer">Compute Buffer resource from which the descriptor is requested.</param>
        /// <returns>The input compute buffer descriptor.</returns>
        public ComputeBufferDesc GetComputeBufferDesc(in ComputeBufferHandle computeBuffer)
        {
            return m_Resources.GetComputeBufferResourceDesc(computeBuffer.handle);
        }

        /// <summary>
        /// Add a new Render Pass to the Render Graph.
        /// </summary>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="sampler">Profiling sampler used around the pass.</param>
        /// <returns>A new instance of a RenderGraphBuilder used to setup the new Render Pass.</returns>
        public RenderGraphBuilder AddRenderPass<PassData>(string passName, out PassData passData, ProfilingSampler sampler) where PassData : class, new()
        {
            var renderPass = m_RenderGraphPool.Get<RenderGraphPass<PassData>>();
            renderPass.Initialize(m_RenderPasses.Count, m_RenderGraphPool.Get<PassData>(), passName, sampler);

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
        /// <returns>A new instance of a RenderGraphBuilder used to setup the new Render Pass.</returns>
        public RenderGraphBuilder AddRenderPass<PassData>(string passName, out PassData passData) where PassData : class, new()
        {
            return AddRenderPass(passName, out passData, GetDefaultProfilingSampler(passName));
        }

        /// <summary>
        /// Begin using the render graph.
        /// This must be called before adding any pass to the render graph.
        /// </summary>
        /// <param name="parameters">Parameters necessary for the render graph execution.</param>
        public void Begin(in RenderGraphParameters parameters)
        {
            m_ExecutionCount++;

            m_Logger.Initialize();

            m_Resources.BeginRender(parameters.currentFrameIndex, m_ExecutionCount);
            m_DefaultResources.InitializeForRendering(this);

            m_RenderGraphContext.cmd = parameters.commandBuffer;
            m_RenderGraphContext.renderContext = parameters.scriptableRenderContext;
            m_RenderGraphContext.renderGraphPool = m_RenderGraphPool;
            m_RenderGraphContext.defaultResources = m_DefaultResources;

            if (m_DebugParameters.immediateMode)
            {
                LogFrameInformation();

                // Prepare the list of compiled pass info for immediate mode.
                // Conservative resize because we don't know how many passes there will be.
                // We might still need to grow the array later on anyway if it's not enough.
                m_CompiledPassInfos.Resize(m_CompiledPassInfos.capacity);
                m_CurrentImmediatePassIndex = 0;

                for(int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                {
                    if (m_ImmediateModeResourceList[i] == null)
                        m_ImmediateModeResourceList[i] = new List<int>();

                    m_ImmediateModeResourceList[i].Clear();
                }
            }
        }

        /// <summary>
        /// Execute the Render Graph in its current state.
        /// </summary>
        public void Execute()
        {
            m_ExecutionExceptionWasRaised = false;

            try
            {
                if (m_RenderGraphContext.cmd == null)
                    throw new InvalidOperationException("RenderGraph.Begin was not called before executing the render graph.");

                if (!m_DebugParameters.immediateMode)
                {
                    LogFrameInformation();

                    CompileRenderGraph();
                    ExecuteRenderGraph();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Render Graph Execution error");
                if (!m_ExecutionExceptionWasRaised) // Already logged. TODO: There is probably a better way in C# to handle that.
                    Debug.LogException(e);
                m_ExecutionExceptionWasRaised = true;
            }
            finally
            {
                if (!m_ExecutionExceptionWasRaised && requireDebugData)
                    GenerateDebugData();

                if (m_DebugParameters.immediateMode)
                    ReleaseImmediateModeResources();

                ClearCompiledGraph();

                if (m_DebugParameters.logFrameInformation || m_DebugParameters.logResources)
                    Debug.Log(m_Logger.GetLog());

                m_Resources.EndRender();

                InvalidateContext();
            }
        }


        class ProfilingScopePassData
        {
            public ProfilingSampler sampler;
        }

        /// <summary>
        /// Begin a profiling scope.
        /// </summary>
        /// <param name="sampler">Sampler used for profiling.</param>
        public void BeginProfilingSampler(ProfilingSampler sampler)
        {
            using (var builder = AddRenderPass<ProfilingScopePassData>("BeginProfile", out var passData, null))
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
        public void EndProfilingSampler(ProfilingSampler sampler)
        {
            using (var builder = AddRenderPass<ProfilingScopePassData>("EndProfile", out var passData, null))
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
        internal static List<RenderGraph>  GetRegisteredRenderGraphs()
        {
            return s_RegisteredGraphs;
        }

        // Internal for testing purpose only
        internal DynamicArray<CompiledPassInfo> GetCompiledPassInfos() { return m_CompiledPassInfos; }

        // Internal for testing purpose only
        internal void ClearCompiledGraph()
        {
            ClearRenderPasses();
            m_Resources.Clear(m_ExecutionExceptionWasRaised);
            m_RendererLists.Clear();
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_CompiledResourcesInfos[i].Clear();
            m_CompiledPassInfos.Clear();
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
                ExecutePassImmediatly(pass);
            }
        }

        internal delegate void OnGraphRegisteredDelegate(RenderGraph graph);
        internal static event OnGraphRegisteredDelegate onGraphRegistered;
        internal static event OnGraphRegisteredDelegate onGraphUnregistered;

        #endregion

        #region Private Interface

        void InitResourceInfosData(DynamicArray<CompiledResourceInfo> resourceInfos, int count)
        {
            resourceInfos.Resize(count);
            for (int i = 0; i < resourceInfos.size; ++i)
                resourceInfos[i].Reset();
        }

        void InitializeCompilationData()
        {
            InitResourceInfosData(m_CompiledResourcesInfos[(int)RenderGraphResourceType.Texture], m_Resources.GetTextureResourceCount());
            InitResourceInfosData(m_CompiledResourcesInfos[(int)RenderGraphResourceType.ComputeBuffer], m_Resources.GetComputeBufferResourceCount());

            m_CompiledPassInfos.Resize(m_RenderPasses.Count);
            for (int i = 0; i < m_CompiledPassInfos.size; ++i)
                m_CompiledPassInfos[i].Reset(m_RenderPasses[i]);
        }

        void CountReferences()
        {
            for (int passIndex = 0; passIndex < m_CompiledPassInfos.size; ++passIndex)
            {
                ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[passIndex];

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    var resourceRead = passInfo.pass.resourceReadLists[type];
                    foreach (var resource in resourceRead)
                    {
                        ref CompiledResourceInfo info = ref m_CompiledResourcesInfos[type][resource];
                        info.consumers.Add(passIndex);
                        info.refCount++;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        passInfo.debugResourceReads[type].Add(m_Resources.GetResourceName(resource));
#endif
                    }

                    var resourceWrite = passInfo.pass.resourceWriteLists[type];
                    foreach (var resource in resourceWrite)
                    {
                        ref CompiledResourceInfo info = ref m_CompiledResourcesInfos[type][resource];
                        info.producers.Add(passIndex);
                        passInfo.refCount++;

                        // Writing to an imported texture is considered as a side effect because we don't know what users will do with it outside of render graph.
                        if (m_Resources.IsResourceImported(resource))
                            passInfo.hasSideEffect = true;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        passInfo.debugResourceWrites[type].Add(m_Resources.GetResourceName(resource));
#endif
                    }

                    foreach (int resourceIndex in passInfo.pass.transientResourceList[type])
                    {
                        ref CompiledResourceInfo info = ref m_CompiledResourcesInfos[type][resourceIndex];
                        info.refCount++;
                        info.consumers.Add(passIndex);
                        info.producers.Add(passIndex);
                    }
                }
            }
        }

        void CullOutputlessPasses()
        {
            // Gather passes that don't produce anything and cull them.
            m_CullingStack.Clear();
            for (int pass = 0; pass < m_CompiledPassInfos.size; ++pass)
            {
                ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[pass];

                if (passInfo.refCount == 0 && !passInfo.hasSideEffect && passInfo.allowPassCulling)
                {
                    // Producer is not necessary as it produces zero resources
                    // Cull it and decrement refCount of all the resources it reads.
                    // We don't need to go recursively here because we decrement ref count of read resources
                    // so the subsequent passes of culling will detect those and remove the related passes.
                    passInfo.culled = true;
                    for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                    {
                        foreach (var index in passInfo.pass.resourceReadLists[type])
                        {
                            m_CompiledResourcesInfos[type][index].refCount--;

                        }
                    }
                }
            }
        }

        void CullUnusedPasses()
        {
            if (m_DebugParameters.disablePassCulling)
            {
                if (m_DebugParameters.logFrameInformation)
                {
                    m_Logger.LogLine("- Pass Culling Disabled -\n");
                }
                return;
            }

            // TODO RENDERGRAPH: temporarily remove culling of passes without product.
            // Many passes are used just to set global variables so we don't want to force users to disallow culling on those explicitly every time.
            // This will cull passes with no outputs.
            //CullOutputlessPasses();

            // This will cull all passes that produce resource that are never read.
            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                DynamicArray<CompiledResourceInfo> resourceUsageList = m_CompiledResourcesInfos[type];

                // Gather resources that are never read.
                m_CullingStack.Clear();
                for (int i = 0; i < resourceUsageList.size; ++i)
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
                        ref var producerInfo = ref m_CompiledPassInfos[producerIndex];
                        producerInfo.refCount--;
                        if (producerInfo.refCount == 0 && !producerInfo.hasSideEffect && producerInfo.allowPassCulling)
                        {
                            // Producer is not necessary anymore as it produces zero resources
                            // Cull it and decrement refCount of all the textures it reads.
                            producerInfo.culled = true;

                            foreach (var resourceIndex in producerInfo.pass.resourceReadLists[type])
                            {
                                ref CompiledResourceInfo resourceInfo = ref resourceUsageList[resourceIndex];
                                resourceInfo.refCount--;
                                // If a resource is not used anymore, add it to the stack to be processed in subsequent iteration.
                                if (resourceInfo.refCount == 0)
                                    m_CullingStack.Push(resourceIndex);
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
                ref CompiledPassInfo currentPassInfo = ref m_CompiledPassInfos[currentPassIndex];

                //If the passes are on different pipes, we need synchronization.
                if (m_CompiledPassInfos[lastProducer].enableAsyncCompute != currentPassInfo.enableAsyncCompute)
                {
                    // Pass is on compute pipe, need sync with graphics pipe.
                    if (currentPassInfo.enableAsyncCompute)
                    {
                        if (lastProducer > lastGraphicsPipeSync)
                        {
                            UpdatePassSynchronization(ref currentPassInfo, ref m_CompiledPassInfos[lastProducer], currentPassIndex, lastProducer, ref lastGraphicsPipeSync);
                        }
                    }
                    else
                    {
                        if (lastProducer > lastComputePipeSync)
                        {
                            UpdatePassSynchronization(ref currentPassInfo, ref m_CompiledPassInfos[lastProducer], currentPassIndex, lastProducer, ref lastComputePipeSync);
                        }
                    }
                }
            }
        }

        int GetLatestProducerIndex(int passIndex, in CompiledResourceInfo info)
        {
            // We want to know the highest pass index below the current pass that writes to the resource.
            int result = -1;
            foreach (var producer in info.producers)
            {
                // producers are by construction in increasing order.
                if (producer < passIndex)
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

            var consumers = info.consumers;
            for (int i = consumers.Count - 1; i >= 0; --i)
            {
                if (!m_CompiledPassInfos[consumers[i]].culled)
                    return consumers[i];
            }

            return -1;
        }

        int GetFirstValidWriteIndex(in CompiledResourceInfo info)
        {
            if (info.producers.Count == 0)
                return -1;

            var producers = info.producers;
            for (int i = 0; i < producers.Count; i++)
            {
                if (!m_CompiledPassInfos[producers[i]].culled)
                    return producers[i];
            }

            return -1;
        }

        int GetLatestValidWriteIndex(in CompiledResourceInfo info)
        {
            if (info.producers.Count == 0)
                return -1;

            var producers = info.producers;
            for (int i = producers.Count - 1; i >= 0; --i)
            {
                if (!m_CompiledPassInfos[producers[i]].culled)
                    return producers[i];
            }

            return -1;
        }


        void UpdateResourceAllocationAndSynchronization()
        {
            int lastGraphicsPipeSync = -1;
            int lastComputePipeSync = -1;

            // First go through all passes.
            // - Update the last pass read index for each resource.
            // - Add texture to creation list for passes that first write to a texture.
            // - Update synchronization points for all resources between compute and graphics pipes.
            for (int passIndex = 0; passIndex < m_CompiledPassInfos.size; ++passIndex)
            {
                ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[passIndex];

                if (passInfo.culled)
                    continue;

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    var resourcesInfo = m_CompiledResourcesInfos[type];
                    foreach (int resource in passInfo.pass.resourceReadLists[type])
                    {
                        UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, resourcesInfo[resource]);
                    }

                    foreach (int resource in passInfo.pass.resourceWriteLists[type])
                    {
                        UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, resourcesInfo[resource]);
                    }

                }

                // Gather all renderer lists
                m_RendererLists.AddRange(passInfo.pass.usedRendererListList);
            }

            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                var resourceInfos = m_CompiledResourcesInfos[type];
                // Now push resources to the release list of the pass that reads it last.
                for (int i = 0; i < resourceInfos.size; ++i)
                {
                    CompiledResourceInfo resourceInfo = resourceInfos[i];

                    // Resource creation
                    int firstWriteIndex = GetFirstValidWriteIndex(resourceInfo);
                    // Index -1 can happen for imported resources (for example an imported dummy black texture will never be written to but does not need creation anyway)
                    if (firstWriteIndex != -1)
                        m_CompiledPassInfos[firstWriteIndex].resourceCreateList[type].Add(i);

                    // Texture release
                    // Sometimes, a texture can be written by a pass after the last pass that reads it.
                    // In this case, we need to extend its lifetime to this pass otherwise the pass would get an invalid texture.
                    int lastReadPassIndex = Math.Max(GetLatestValidReadIndex(resourceInfo), GetLatestValidWriteIndex(resourceInfo));

                    if (lastReadPassIndex != -1)
                    {
                        // In case of async passes, we need to extend lifetime of resource to the first pass on the graphics pipeline that wait for async passes to be over.
                        // Otherwise, if we freed the resource right away during an async pass, another non async pass could reuse the resource even though the async pipe is not done.
                        if (m_CompiledPassInfos[lastReadPassIndex].enableAsyncCompute)
                        {
                            int currentPassIndex = lastReadPassIndex;
                            int firstWaitingPassIndex = m_CompiledPassInfos[currentPassIndex].syncFromPassIndex;
                            // Find the first async pass that is synchronized by the graphics pipeline (ie: passInfo.syncFromPassIndex != -1)
                            while (firstWaitingPassIndex == -1 && currentPassIndex < m_CompiledPassInfos.size)
                            {
                                currentPassIndex++;
                                if (m_CompiledPassInfos[currentPassIndex].enableAsyncCompute)
                                    firstWaitingPassIndex = m_CompiledPassInfos[currentPassIndex].syncFromPassIndex;
                            }

                            // Finally add the release command to the pass before the first pass that waits for the compute pipe.
                            ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[Math.Max(0, firstWaitingPassIndex - 1)];
                            passInfo.resourceReleaseList[type].Add(i);

                            // Fail safe in case render graph is badly formed.
                            if (currentPassIndex == m_CompiledPassInfos.size)
                            {
                                RenderGraphPass invalidPass = m_RenderPasses[lastReadPassIndex];
                                throw new InvalidOperationException($"Asynchronous pass {invalidPass.name} was never synchronized on the graphics pipeline.");
                            }
                        }
                        else
                        {
                            ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[lastReadPassIndex];
                            passInfo.resourceReleaseList[type].Add(i);
                        }
                    }
                }
            }

            // Creates all renderer lists
            m_Resources.CreateRendererLists(m_RendererLists);
        }

        // Internal visibility for testing purpose only
        // Traverse the render graph:
        // - Determines when resources are created/released
        // - Determines async compute pass synchronization
        // - Cull unused render passes.
        internal void CompileRenderGraph()
        {
            InitializeCompilationData();
            CountReferences();
            CullUnusedPasses();
            UpdateResourceAllocationAndSynchronization();
            LogRendererListsCreation();
        }

        ref CompiledPassInfo CompilePassImmediatly(RenderGraphPass pass)
        {
            // If we don't have enough pre allocated elements we double the size.
            // It's pretty aggressive but the immediate mode is only for debug purpose so it should be fine.
            if (m_CurrentImmediatePassIndex >= m_CompiledPassInfos.size)
                m_CompiledPassInfos.Resize(m_CompiledPassInfos.size * 2);

            ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[m_CurrentImmediatePassIndex++];
            passInfo.Reset(pass);
            // In immediate mode we don't have proper information to generate synchronization so we disable async compute.
            passInfo.enableAsyncCompute = false;

            // In immediate mode, we don't have any resource usage information so we'll just create resources whenever they are written to if not already alive.
            // We will release all resources at the end of the render graph execution.
            for (int iType = 0; iType < (int)RenderGraphResourceType.Count; ++iType)
            {
                foreach (var res in pass.resourceWriteLists[iType])
                {
                    if (!m_Resources.IsResourceCreated(res))
                    {
                        passInfo.resourceCreateList[iType].Add(res);
                        m_ImmediateModeResourceList[iType].Add(res);
                    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    passInfo.debugResourceWrites[iType].Add(m_Resources.GetResourceName(res));
#endif
                }

                foreach (var res in pass.transientResourceList[iType])
                {
                    passInfo.resourceCreateList[iType].Add(res);
                    passInfo.resourceReleaseList[iType].Add(res);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    passInfo.debugResourceWrites[iType].Add(m_Resources.GetResourceName(res));
                    passInfo.debugResourceReads[iType].Add(m_Resources.GetResourceName(res));
#endif
                }

                foreach (var res in pass.resourceReadLists[iType])
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    passInfo.debugResourceReads[iType].Add(m_Resources.GetResourceName(res));
#endif
                }
            }

            // Create the necessary renderer lists
            foreach(var rl in pass.usedRendererListList)
            {
                if (!m_Resources.IsRendererListCreated(rl))
                    m_RendererLists.Add(rl);
            }
            m_Resources.CreateRendererLists(m_RendererLists);
            m_RendererLists.Clear();

            return ref passInfo;
        }

        void ExecutePassImmediatly(RenderGraphPass pass)
        {
            ExecuteCompiledPass(ref CompilePassImmediatly(pass), m_CurrentImmediatePassIndex - 1);
        }

        void ExecuteCompiledPass(ref CompiledPassInfo passInfo, int passIndex)
        {
            if (passInfo.culled)
                return;

            if (!passInfo.pass.HasRenderFunc())
            {
                throw new InvalidOperationException(string.Format("RenderPass {0} was not provided with an execute function.", passInfo.pass.name));
            }

            try
            {
                using (new ProfilingScope(m_RenderGraphContext.cmd, passInfo.pass.customSampler))
                {
                    LogRenderPassBegin(passInfo);
                    using (new RenderGraphLogIndent(m_Logger))
                    {
                        PreRenderPassExecute(passInfo, m_RenderGraphContext);
                        passInfo.pass.Execute(m_RenderGraphContext);
                        PostRenderPassExecute(ref passInfo, m_RenderGraphContext);
                    }
                }
            }
            catch (Exception e)
            {
                m_ExecutionExceptionWasRaised = true;
                Debug.LogError($"Render Graph Execution error at pass {passInfo.pass.name} ({passIndex})");
                Debug.LogException(e);
                throw;
            }
        }

        // Execute the compiled render graph
        void ExecuteRenderGraph()
        {
            for (int passIndex = 0; passIndex < m_CompiledPassInfos.size; ++passIndex)
            {
                ExecuteCompiledPass(ref m_CompiledPassInfos[passIndex], passIndex);
            }
        }

        void PreRenderPassSetRenderTargets(in CompiledPassInfo passInfo, RenderGraphContext rgContext)
        {
            var pass = passInfo.pass;
            if (pass.depthBuffer.IsValid() || pass.colorBufferMaxIndex != -1)
            {
                var mrtArray = rgContext.renderGraphPool.GetTempArray<RenderTargetIdentifier>(pass.colorBufferMaxIndex + 1);
                var colorBuffers = pass.colorBuffers;

                if (pass.colorBufferMaxIndex > 0)
                {
                    for (int i = 0; i <= pass.colorBufferMaxIndex; ++i)
                    {
                        if (!colorBuffers[i].IsValid())
                            throw new InvalidOperationException("MRT setup is invalid. Some indices are not used.");
                        mrtArray[i] = m_Resources.GetTexture(colorBuffers[i]);
                    }

                    if (pass.depthBuffer.IsValid())
                    {
                        CoreUtils.SetRenderTarget(rgContext.cmd, mrtArray, m_Resources.GetTexture(pass.depthBuffer));
                    }
                    else
                    {
                        throw new InvalidOperationException("Setting MRTs without a depth buffer is not supported.");
                    }
                }
                else
                {
                    if (pass.depthBuffer.IsValid())
                    {
                        if (pass.colorBufferMaxIndex > -1)
                            CoreUtils.SetRenderTarget(rgContext.cmd, m_Resources.GetTexture(pass.colorBuffers[0]), m_Resources.GetTexture(pass.depthBuffer));
                        else
                            CoreUtils.SetRenderTarget(rgContext.cmd, m_Resources.GetTexture(pass.depthBuffer));
                    }
                    else
                    {
                        CoreUtils.SetRenderTarget(rgContext.cmd, m_Resources.GetTexture(pass.colorBuffers[0]));
                    }

                }
            }
        }

        void PreRenderPassExecute(in CompiledPassInfo passInfo, RenderGraphContext rgContext)
        {
            // TODO RENDERGRAPH merge clear and setup here if possible
            RenderGraphPass pass = passInfo.pass;

            // Need to save the command buffer to restore it later as the one in the context can changed if running a pass async.
            m_PreviousCommandBuffer = rgContext.cmd;

            foreach (var texture in passInfo.resourceCreateList[(int)RenderGraphResourceType.Texture])
                m_Resources.CreateAndClearTexture(rgContext, texture);

            foreach (var buffer in passInfo.resourceCreateList[(int)RenderGraphResourceType.ComputeBuffer])
                m_Resources.CreateComputeBuffer(rgContext, buffer);

            PreRenderPassSetRenderTargets(passInfo, rgContext);

            // Flush first the current command buffer on the render context.
            rgContext.renderContext.ExecuteCommandBuffer(rgContext.cmd);
            rgContext.cmd.Clear();

            if (passInfo.enableAsyncCompute)
            {
                CommandBuffer asyncCmd = CommandBufferPool.Get(pass.name);
                asyncCmd.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
                rgContext.cmd = asyncCmd;
            }

            // Synchronize with graphics or compute pipe if needed.
            if (passInfo.syncToPassIndex != -1)
            {
                rgContext.cmd.WaitOnAsyncGraphicsFence(m_CompiledPassInfos[passInfo.syncToPassIndex].fence);
            }
        }

        void PostRenderPassExecute(ref CompiledPassInfo passInfo, RenderGraphContext rgContext)
        {
            RenderGraphPass pass = passInfo.pass;

            if (passInfo.needGraphicsFence)
                passInfo.fence = rgContext.cmd.CreateAsyncGraphicsFence();

            if (passInfo.enableAsyncCompute)
            {
                // The command buffer has been filled. We can kick the async task.
                rgContext.renderContext.ExecuteCommandBufferAsync(rgContext.cmd, ComputeQueueType.Background);
                CommandBufferPool.Release(rgContext.cmd);
                rgContext.cmd = m_PreviousCommandBuffer; // Restore the main command buffer.
            }

            m_RenderGraphPool.ReleaseAllTempAlloc();

            foreach (var texture in passInfo.resourceReleaseList[(int)RenderGraphResourceType.Texture])
                m_Resources.ReleaseTexture(rgContext, texture);
            foreach (var buffer in passInfo.resourceReleaseList[(int)RenderGraphResourceType.ComputeBuffer])
                m_Resources.ReleaseComputeBuffer(rgContext, buffer);
        }

        void ClearRenderPasses()
        {
            foreach (var pass in m_RenderPasses)
                pass.Release(m_RenderGraphPool);
            m_RenderPasses.Clear();
        }

        void ReleaseImmediateModeResources()
        {
            foreach (var texture in m_ImmediateModeResourceList[(int)RenderGraphResourceType.Texture])
                m_Resources.ReleaseTexture(m_RenderGraphContext, texture);
            foreach (var buffer in m_ImmediateModeResourceList[(int)RenderGraphResourceType.ComputeBuffer])
                m_Resources.ReleaseComputeBuffer(m_RenderGraphContext, buffer);
        }

        void LogFrameInformation()
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("==== Staring render graph frame ====");

                if (!m_DebugParameters.immediateMode)
                    m_Logger.LogLine("Number of passes declared: {0}\n", m_RenderPasses.Count);
            }
        }

        void LogRendererListsCreation()
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("Number of renderer lists created: {0}\n", m_RendererLists.Count);
            }
        }

        void LogRenderPassBegin(in CompiledPassInfo passInfo)
        {
            if (m_DebugParameters.logFrameInformation)
            {
                RenderGraphPass pass = passInfo.pass;

                m_Logger.LogLine("[{0}][{1}] \"{2}\"", pass.index, pass.enableAsyncCompute ? "Compute" : "Graphics", pass.name);
                using (new RenderGraphLogIndent(m_Logger))
                {
                    if (passInfo.syncToPassIndex != -1)
                        m_Logger.LogLine("Synchronize with [{0}]", passInfo.syncToPassIndex);
                }
            }
        }

        void LogCulledPasses()
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("Pass Culling Report:");
                using (new RenderGraphLogIndent(m_Logger))
                {
                    for (int i = 0; i < m_CompiledPassInfos.size; ++i)
                    {
                        if (m_CompiledPassInfos[i].culled)
                        {
                            var pass = m_RenderPasses[i];
                            m_Logger.LogLine("[{0}] {1}", pass.index, pass.name);
                        }
                    }
                    m_Logger.LogLine("\n");
                }
            }
        }

        ProfilingSampler GetDefaultProfilingSampler(string name)
        {
            int hash = name.GetHashCode();
            if (!m_DefaultProfilingSamplers.TryGetValue(hash, out var sampler))
            {
                sampler = new ProfilingSampler(name);
                m_DefaultProfilingSamplers.Add(hash, sampler);
            }

            return sampler;
        }

        void UpdateImportedResourceLifeTime(ref RenderGraphDebugData.ResourceDebugData data, List<int> passList)
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
            m_RenderGraphDebugData.Clear();

            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                for (int i = 0; i < m_CompiledResourcesInfos[type].size; ++i)
                {
                    ref var resourceInfo = ref m_CompiledResourcesInfos[type][i];
                    RenderGraphDebugData.ResourceDebugData newResource = new RenderGraphDebugData.ResourceDebugData();
                    newResource.name = m_Resources.GetResourceName((RenderGraphResourceType)type, i);
                    newResource.imported = m_Resources.IsResourceImported((RenderGraphResourceType)type, i);
                    newResource.creationPassIndex = -1;
                    newResource.releasePassIndex = -1;

                    newResource.consumerList = new List<int>(resourceInfo.consumers);
                    newResource.producerList = new List<int>(resourceInfo.producers);

                    if (newResource.imported)
                    {
                        UpdateImportedResourceLifeTime(ref newResource, newResource.consumerList);
                        UpdateImportedResourceLifeTime(ref newResource, newResource.producerList);
                    }

                    m_RenderGraphDebugData.resourceLists[type].Add(newResource);
                }
            }

            for (int i = 0; i < m_CompiledPassInfos.size; ++i)
            {
                ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[i];

                RenderGraphDebugData.PassDebugData newPass = new RenderGraphDebugData.PassDebugData();
                newPass.name = passInfo.pass.name;
                newPass.culled = passInfo.culled;
                newPass.generateDebugData = passInfo.pass.generateDebugData;
                newPass.resourceReadLists = new List<int>[(int)RenderGraphResourceType.Count];
                newPass.resourceWriteLists = new List<int>[(int)RenderGraphResourceType.Count];

                for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
                {
                    newPass.resourceReadLists[type] = new List<int>();
                    newPass.resourceWriteLists[type] = new List<int>();

                    foreach (var resourceRead in passInfo.pass.resourceReadLists[type])
                        newPass.resourceReadLists[type].Add(resourceRead);
                    foreach (var resourceWrite in passInfo.pass.resourceWriteLists[type])
                        newPass.resourceWriteLists[type].Add(resourceWrite);

                    foreach (var resourceCreate in passInfo.resourceCreateList[type])
                    {
                        var res = m_RenderGraphDebugData.resourceLists[type][resourceCreate];
                        if (res.imported)
                            continue;
                        res.creationPassIndex = i;
                        m_RenderGraphDebugData.resourceLists[type][resourceCreate] = res;
                    }

                    foreach (var resourceRelease in passInfo.resourceReleaseList[type])
                    {
                        var res = m_RenderGraphDebugData.resourceLists[type][resourceRelease];
                        if (res.imported)
                            continue;
                        res.releasePassIndex = i;
                        m_RenderGraphDebugData.resourceLists[type][resourceRelease] = res;
                    }
                }

                m_RenderGraphDebugData.passList.Add(newPass);
            }
        }

        #endregion
    }

    /// <summary>
    /// Render Graph Scoped Profiling markers
    /// </summary>
    public struct RenderGraphProfilingScope : IDisposable
    {
        bool m_Disposed;
        ProfilingSampler m_Sampler;
        RenderGraph m_RenderGraph;

        /// <summary>
        /// Profiling Scope constructor
        /// </summary>
        /// <param name="renderGraph">Render Graph used for this scope.</param>
        /// <param name="sampler">Profiling Sampler to be used for this scope.</param>
        public RenderGraphProfilingScope(RenderGraph renderGraph, ProfilingSampler sampler)
        {
            m_RenderGraph = renderGraph;
            m_Sampler = sampler;
            m_Disposed = false;
            renderGraph.BeginProfilingSampler(sampler);
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
        }
    }
}

