using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

// Typedef for the in-engine RendererList API (to avoid conflicts with the experimental version)
using CoreRendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

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
        public ScriptableRenderContext renderContext;
        ///<summary>Command Buffer used for rendering.</summary>
        public CommandBuffer cmd;
        ///<summary>Render Graph pool used for temporary data.</summary>
        public RenderGraphObjectPool renderGraphPool;
        ///<summary>Render Graph default resources.</summary>
        public RenderGraphDefaultResources defaultResources;
    }

    /// <summary>
    /// This struct contains properties which control the execution of the Render Graph.
    /// </summary>
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
    }

    /// <summary>
    /// This struct is used to define the scope where the Render Graph is recorded before the execution.
    /// When this struct goes out of scope or is disposed, the Render Graph will be automatically executed.
    /// </summary>
    /// <seealso cref="RenderGraph.RecordAndExecute(in RenderGraphParameters)"/>
    public struct RenderGraphExecution : IDisposable
    {
        RenderGraph renderGraph;

        /// <summary>
        /// Internal constructor for RenderGraphExecution
        /// </summary>
        /// <param name="renderGraph">renderGraph</param>
        internal RenderGraphExecution(RenderGraph renderGraph)
            => this.renderGraph = renderGraph;

        /// <summary>
        /// This function triggers the Render Graph to be executed.
        /// </summary>
        public void Dispose() => renderGraph.Execute();
    }

    class RenderGraphDebugParams
    {
        DebugUI.Widget[] m_DebugItems;
        DebugUI.Panel m_DebugPanel;

        public bool clearRenderTargetsAtCreation;
        public bool clearRenderTargetsAtRelease;
        public bool disablePassCulling;
        public bool immediateMode;
        public bool enableLogging;
        public bool logFrameInformation;
        public bool logResources;

        private static class Strings
        {
            public static readonly NameAndTooltip ClearRenderTargetsAtCreation = new() { name = "Clear Render Targets At Creation", tooltip = "Enable to clear all render textures before any rendergraph passes to check if some clears are missing." };
            public static readonly NameAndTooltip DisablePassCulling = new() { name = "Disable Pass Culling", tooltip = "Enable to temporarily disable culling to asses if a pass is culled." };
            public static readonly NameAndTooltip ImmediateMode = new() { name = "Immediate Mode", tooltip = "Enable to force render graph to execute all passes in the order you registered them." };
            public static readonly NameAndTooltip EnableLogging = new() { name = "Enable Logging", tooltip = "Enable to allow HDRP to capture information in the log." };
            public static readonly NameAndTooltip LogFrameInformation = new() { name = "Log Frame Information", tooltip = "Enable to log information output from each frame." };
            public static readonly NameAndTooltip LogResources = new() { name = "Log Resources", tooltip = "Enable to log the current render graph's global resource usage." };
        }

        public void RegisterDebug(string name, DebugUI.Panel debugPanel = null)
        {
            var list = new List<DebugUI.Widget>();
            list.Add(new DebugUI.Container
            {
                displayName = $"{name} Render Graph",
                children =
                {
                    new DebugUI.BoolField { nameAndTooltip = Strings.ClearRenderTargetsAtCreation, getter = () => clearRenderTargetsAtCreation, setter = value => clearRenderTargetsAtCreation = value },
                    // We cannot expose this option as it will change the active render target and the debug menu won't know where to render itself anymore.
                    //    list.Add(new DebugUI.BoolField { displayName = "Clear Render Targets at release", getter = () => clearRenderTargetsAtRelease, setter = value => clearRenderTargetsAtRelease = value });
                    new DebugUI.BoolField { nameAndTooltip = Strings.DisablePassCulling, getter = () => disablePassCulling, setter = value => disablePassCulling = value },
                    new DebugUI.BoolField { nameAndTooltip = Strings.ImmediateMode, getter = () => immediateMode, setter = value => immediateMode = value },
                    new DebugUI.BoolField { nameAndTooltip = Strings.EnableLogging, getter = () => enableLogging, setter = value => enableLogging = value },
                    new DebugUI.Button
                    {
                        nameAndTooltip = Strings.LogFrameInformation,
                        action = () =>
                        {
                            if (!enableLogging)
                                Debug.Log("You must first enable logging before this logging frame information.");
                            logFrameInformation = true;
            #if UNITY_EDITOR
                            UnityEditor.SceneView.RepaintAll();
            #endif
                        }
                    },
                    new DebugUI.Button
                    {
                        nameAndTooltip = Strings.LogResources,
                        action = () =>
                        {
                            if (!enableLogging)
                                Debug.Log("You must first enable logging before this logging resources.");
                            logResources = true;
            #if UNITY_EDITOR
                            UnityEditor.SceneView.RepaintAll();
            #endif
                        }
                    }
                }
            });

            m_DebugItems = list.ToArray();
            m_DebugPanel = debugPanel != null ? debugPanel : DebugManager.instance.GetPanel(name.Length == 0 ? "Render Graph" : name, true);
            m_DebugPanel.children.Add(m_DebugItems);
        }

        public void UnRegisterDebug(string name)
        {
            //DebugManager.instance.RemovePanel(name.Length == 0 ? "Render Graph" : name);
            m_DebugPanel.children.Remove(m_DebugItems);
            m_DebugPanel = null;
            m_DebugItems = null;
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
            public bool async;
            public int syncToPassIndex; // Index of the pass that needs to be waited for.
            public int syncFromPassIndex; // Smaller pass index that waits for this pass.
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

        [DebuggerDisplay("RenderPass: {pass.name} (Index:{pass.index} Async:{enableAsyncCompute})")]
        internal struct CompiledPassInfo
        {
            public RenderGraphPass pass;
            public List<int>[] resourceCreateList;
            public List<int>[] resourceReleaseList;
            public int refCount;
            public bool culled;
            public bool culledByRendererList;
            public bool hasSideEffect;
            public int syncToPassIndex; // Index of the pass that needs to be waited for.
            public int syncFromPassIndex; // Smaller pass index that waits for this pass.
            public bool needGraphicsFence;
            public GraphicsFence fence;

            public bool enableAsyncCompute;
            public bool allowPassCulling { get { return pass.allowPassCulling; } }

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

        RenderGraphResourceRegistry m_Resources;
        RenderGraphObjectPool m_RenderGraphPool = new RenderGraphObjectPool();
        List<RenderGraphPass> m_RenderPasses = new List<RenderGraphPass>(64);
        List<RendererListHandle> m_RendererLists = new List<RendererListHandle>(32);
        RenderGraphDebugParams m_DebugParameters = new RenderGraphDebugParams();
        RenderGraphLogger m_FrameInformationLogger = new RenderGraphLogger();
        RenderGraphDefaultResources m_DefaultResources = new RenderGraphDefaultResources();
        Dictionary<int, ProfilingSampler> m_DefaultProfilingSamplers = new Dictionary<int, ProfilingSampler>();
        bool m_ExecutionExceptionWasRaised;
        RenderGraphContext m_RenderGraphContext = new RenderGraphContext();
        CommandBuffer m_PreviousCommandBuffer;
        int m_CurrentImmediatePassIndex;
        List<int>[] m_ImmediateModeResourceList = new List<int>[(int)RenderGraphResourceType.Count];

        // Compiled Render Graph info.
        DynamicArray<CompiledResourceInfo>[] m_CompiledResourcesInfos = new DynamicArray<CompiledResourceInfo>[(int)RenderGraphResourceType.Count];
        DynamicArray<CompiledPassInfo> m_CompiledPassInfos = new DynamicArray<CompiledPassInfo>();
        Stack<int> m_CullingStack = new Stack<int>();

        int m_ExecutionCount;
        int m_CurrentFrameIndex;
        bool m_HasRenderGraphBegun;
        string m_CurrentExecutionName;
        bool m_RendererListCulling;
        Dictionary<string, RenderGraphDebugData> m_DebugData = new Dictionary<string, RenderGraphDebugData>();

        // Global list of living render graphs
        static List<RenderGraph> s_RegisteredGraphs = new List<RenderGraph>();

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
            m_Resources = new RenderGraphResourceRegistry(m_DebugParameters, m_FrameInformationLogger);

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                m_CompiledResourcesInfos[i] = new DynamicArray<CompiledResourceInfo>();
            }

            s_RegisteredGraphs.Add(this);
            onGraphRegistered?.Invoke(this);
        }

        /// <summary>
        /// Cleanup the Render Graph.
        /// </summary>
        public void Cleanup()
        {
            m_Resources.Cleanup();
            m_DefaultResources.Cleanup();
            m_RenderGraphPool.Cleanup();

            s_RegisteredGraphs.Remove(this);
            onGraphUnregistered?.Invoke(this);
        }

        /// <summary>
        /// Register the render graph to the debug window.
        /// </summary>
        /// <param name="panel"></param>
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
        internal RenderGraphDebugData GetDebugData(string executionName)
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
        /// Creates a new Renderer List Render Graph resource.
        /// </summary>
        /// <param name="desc">Renderer List descriptor.</param>
        /// <returns>A new TextureHandle.</returns>
        public RendererListHandle CreateRendererList(in CoreRendererListDesc desc)
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
        /// Starts the recording of the the render graph and then automatically execute when the return value goes out of scope.
        /// This must be called before adding any pass to the render graph.
        /// </summary>
        /// <param name="parameters">Parameters necessary for the render graph execution.</param>
        /// <example>
        /// This shows how to increment an integer.
        /// <code>
        /// using (renderGraph.RecordAndExecute(parameters))
        /// {
        ///     // Add your render graph passes here.
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="RenderGraphExecution"/>
        /// <returns><see cref="RenderGraphExecution"/></returns>
        public RenderGraphExecution RecordAndExecute(in RenderGraphParameters parameters)
        {
            m_CurrentFrameIndex = parameters.currentFrameIndex;
            m_CurrentExecutionName = parameters.executionName != null ? parameters.executionName : "RenderGraphExecution";
            m_HasRenderGraphBegun = true;
            m_RendererListCulling = parameters.rendererListCulling;

            m_Resources.BeginRenderGraph(m_ExecutionCount++);

            if (m_DebugParameters.enableLogging)
            {
                m_FrameInformationLogger.Initialize(m_CurrentExecutionName);
            }

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

                for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                {
                    if (m_ImmediateModeResourceList[i] == null)
                        m_ImmediateModeResourceList[i] = new List<int>();

                    m_ImmediateModeResourceList[i].Clear();
                }

                m_Resources.BeginExecute(m_CurrentFrameIndex);
            }

            return new RenderGraphExecution(this);
        }

        /// <summary>
        /// Execute the Render Graph in its current state.
        /// </summary>
        internal void Execute()
        {
            m_ExecutionExceptionWasRaised = false;

            try
            {
                if (m_RenderGraphContext.cmd == null)
                    throw new InvalidOperationException("RenderGraph.RecordAndExecute was not called before executing the render graph.");


                if (!m_DebugParameters.immediateMode)
                {
                    LogFrameInformation();

                    CompileRenderGraph();

                    m_Resources.BeginExecute(m_CurrentFrameIndex);

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
                GenerateDebugData();

                if (m_DebugParameters.immediateMode)
                    ReleaseImmediateModeResources();

                ClearCompiledGraph();

                m_Resources.EndExecute();

                InvalidateContext();

                m_HasRenderGraphBegun = false;
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
        internal delegate void OnExecutionRegisteredDelegate(RenderGraph graph, string executionName);
        internal static event OnExecutionRegisteredDelegate onExecutionRegistered;
        internal static event OnExecutionRegisteredDelegate onExecutionUnregistered;

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
                        info.imported = m_Resources.IsRenderGraphResourceImported(resource);
                        info.consumers.Add(passIndex);
                        info.refCount++;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        passInfo.debugResourceReads[type].Add(m_Resources.GetRenderGraphResourceName(resource));
#endif
                    }

                    var resourceWrite = passInfo.pass.resourceWriteLists[type];
                    foreach (var resource in resourceWrite)
                    {
                        ref CompiledResourceInfo info = ref m_CompiledResourcesInfos[type][resource];
                        info.imported = m_Resources.IsRenderGraphResourceImported(resource);
                        info.producers.Add(passIndex);

                        // Writing to an imported texture is considered as a side effect because we don't know what users will do with it outside of render graph.
                        passInfo.hasSideEffect = info.imported;
                        passInfo.refCount++;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        passInfo.debugResourceWrites[type].Add(m_Resources.GetRenderGraphResourceName(resource));
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

        int GetFirstValidConsumerIndex(int passIndex, in CompiledResourceInfo info)
        {
            // We want to know the lowest pass index after the current pass that reads from the resource.
            foreach (int consumer in info.consumers)
            {
                // consumers are by construction in increasing order.
                if (consumer > passIndex && !m_CompiledPassInfos[consumer].culled)
                    return consumer;
            }

            return -1;
        }

        int FindTextureProducer(int consumerPass, in CompiledResourceInfo info, out int index)
        {
            // We check all producers before the consumerPass. The first one not culled will be the one allocating the resource
            // If they are all culled, we need to get the one right before the consumer, it will allocate or reuse the resource

            int previousPass = 0;
            for (index = 0; index < info.producers.Count; index++)
            {
                int currentPass = info.producers[index];
                // We found a valid producer - he will allocate the texture
                if (!m_CompiledPassInfos[currentPass].culled)
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
            foreach (var producer in info.producers)
            {
                var producerPassInfo = m_CompiledPassInfos[producer];
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

        void CreateRendererLists()
        {
            for (int passIndex = 0; passIndex < m_CompiledPassInfos.size; ++passIndex)
            {
                ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[passIndex];

                if (passInfo.culled)
                    continue;

                // Gather all renderer lists
                m_RendererLists.AddRange(passInfo.pass.usedRendererListList);
            }

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

        void AllocateCulledPassResources(ref CompiledPassInfo passInfo, int passIndex)
        {
            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                var resourcesInfo = m_CompiledResourcesInfos[type];
                foreach (var resourceHandle in passInfo.pass.resourceWriteLists[type])
                {
                    ref var compiledResource = ref resourcesInfo[resourceHandle];

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

            // First go through all passes.
            // - Update the last pass read index for each resource.
            // - Add texture to creation list for passes that first write to a texture.
            // - Update synchronization points for all resources between compute and graphics pipes.
            for (int passIndex = 0; passIndex < m_CompiledPassInfos.size; ++passIndex)
            {
                ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[passIndex];

                // If this pass is culled, we need to make sure that any texture read by a later pass is still allocated
                // We also try to find an imported fallback to save an allocation
                if (passInfo.culledByRendererList)
                    AllocateCulledPassResources(ref passInfo, passIndex);

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
            }

            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                var resourceInfos = m_CompiledResourcesInfos[type];
                // Now push resources to the release list of the pass that reads it last.
                for (int i = 0; i < resourceInfos.size; ++i)
                {
                    CompiledResourceInfo resourceInfo = resourceInfos[i];

                    bool sharedResource = m_Resources.IsRenderGraphResourceShared((RenderGraphResourceType)type, i);

                    // Imported resource needs neither creation nor release.
                    if (resourceInfo.imported && !sharedResource)
                        continue;

                    // Resource creation
                    int firstWriteIndex = GetFirstValidWriteIndex(resourceInfo);
                    // Index -1 can happen for imported resources (for example an imported dummy black texture will never be written to but does not need creation anyway)
                    // Or when the only pass that was writting to this resource was culled dynamically by renderer lists
                    if (firstWriteIndex != -1)
                        m_CompiledPassInfos[firstWriteIndex].resourceCreateList[type].Add(i);

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
                        if (m_CompiledPassInfos[lastReadPassIndex].enableAsyncCompute)
                        {
                            int currentPassIndex = lastReadPassIndex;
                            int firstWaitingPassIndex = m_CompiledPassInfos[currentPassIndex].syncFromPassIndex;
                            // Find the first async pass that is synchronized by the graphics pipeline (ie: passInfo.syncFromPassIndex != -1)
                            while (firstWaitingPassIndex == -1 && currentPassIndex++ < m_CompiledPassInfos.size - 1)
                            {
                                if (m_CompiledPassInfos[currentPassIndex].enableAsyncCompute)
                                    firstWaitingPassIndex = m_CompiledPassInfos[currentPassIndex].syncFromPassIndex;
                            }

                            // Fail safe in case render graph is badly formed.
                            if (currentPassIndex == m_CompiledPassInfos.size)
                            {
                                // This is not true with passes with side effect as they are writing to a resource that may not be read by the render graph this frame and to no other resource.
                                // In this case we extend the lifetime of resources to the end of the frame. It's not idea but it should not be the majority of cases.
                                if (m_CompiledPassInfos[lastReadPassIndex].hasSideEffect)
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
                            while (m_CompiledPassInfos[releasePassIndex].culled)
                                releasePassIndex = Math.Max(0, releasePassIndex - 1);

                            ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[releasePassIndex];
                            passInfo.resourceReleaseList[type].Add(i);
                        }
                        else
                        {
                            ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[lastReadPassIndex];
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

        bool AreRendererListsEmpty(List<RendererListHandle> rendererLists)
        {
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
            var pass = m_CompiledPassInfos[passIndex].pass;
            if (!m_CompiledPassInfos[passIndex].culled &&
                pass.allowPassCulling &&
                pass.allowRendererListCulling &&
                !m_CompiledPassInfos[passIndex].hasSideEffect)
            {
                if (AreRendererListsEmpty(pass.usedRendererListList))
                {
                    //Debug.Log($"Culling pass <color=red> {pass.name} </color>");
                    m_CompiledPassInfos[passIndex].culled = m_CompiledPassInfos[passIndex].culledByRendererList = true;
                }
            }
        }

        void CullRendererLists()
        {
            for (int passIndex = 0; passIndex < m_CompiledPassInfos.size; ++passIndex)
            {
                if (!m_CompiledPassInfos[passIndex].culled && !m_CompiledPassInfos[passIndex].hasSideEffect)
                {
                    var pass = m_CompiledPassInfos[passIndex].pass;
                    if (pass.usedRendererListList.Count > 0)
                    {
                        TryCullPassAtIndex(passIndex);
                    }
                }
            }
        }

        // Internal visibility for testing purpose only
        // Traverse the render graph:
        // - Determines when resources are created/released
        // - Determines async compute pass synchronization
        // - Cull unused render passes.
        internal void CompileRenderGraph()
        {
            using (new ProfilingScope(m_RenderGraphContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.CompileRenderGraph)))
            {
                InitializeCompilationData();
                CountReferences();

                // First cull all passes thet produce unused output
                CullUnusedPasses();

                // Create the renderer lists of the remaining passes
                CreateRendererLists();

                // Cull dynamically the graph passes based on the renderer list visibility
                if (m_RendererListCulling)
                    CullRendererLists();

                // After all culling passes, allocate the resources for this frame
                UpdateResourceAllocationAndSynchronization();

                LogRendererListsCreation();
            }
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
                foreach (var res in pass.transientResourceList[iType])
                {
                    passInfo.resourceCreateList[iType].Add(res);
                    passInfo.resourceReleaseList[iType].Add(res);

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
                        passInfo.resourceCreateList[iType].Add(res);
                        m_ImmediateModeResourceList[iType].Add(res);
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
            m_Resources.CreateRendererLists(m_RendererLists, m_RenderGraphContext.renderContext);
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
                    using (new RenderGraphLogIndent(m_FrameInformationLogger))
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
            using (new ProfilingScope(m_RenderGraphContext.cmd, ProfilingSampler.Get(RenderGraphProfileId.ExecuteRenderGraph)))
            {
                for (int passIndex = 0; passIndex < m_CompiledPassInfos.size; ++passIndex)
                {
                    ExecuteCompiledPass(ref m_CompiledPassInfos[passIndex], passIndex);
                }
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
            RenderGraphPass pass = passInfo.pass;

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

            PreRenderPassSetRenderTargets(passInfo, rgContext);

            if (passInfo.enableAsyncCompute)
            {
                GraphicsFence previousFence = new GraphicsFence();
                if (executedWorkDuringResourceCreation)
                {
                    previousFence = rgContext.cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.AllGPUOperations);
                }

                // Flush current command buffer on the render context before enqueuing async commands.
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
                RenderGraphPass pass = passInfo.pass;

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
                    for (int i = 0; i < m_CompiledPassInfos.size; ++i)
                    {
                        if (m_CompiledPassInfos[i].culled)
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
            if (m_ExecutionExceptionWasRaised)
                return;

            if (!requireDebugData)
            {
                CleanupDebugData();
                return;
            }

            if (!m_DebugData.TryGetValue(m_CurrentExecutionName, out var debugData))
            {
                onExecutionRegistered?.Invoke(this, m_CurrentExecutionName);
                debugData = new RenderGraphDebugData();
                m_DebugData.Add(m_CurrentExecutionName, debugData);
            }

            debugData.Clear();

            for (int type = 0; type < (int)RenderGraphResourceType.Count; ++type)
            {
                for (int i = 0; i < m_CompiledResourcesInfos[type].size; ++i)
                {
                    ref var resourceInfo = ref m_CompiledResourcesInfos[type][i];
                    RenderGraphDebugData.ResourceDebugData newResource = new RenderGraphDebugData.ResourceDebugData();
                    newResource.name = m_Resources.GetRenderGraphResourceName((RenderGraphResourceType)type, i);
                    newResource.imported = m_Resources.IsRenderGraphResourceImported((RenderGraphResourceType)type, i);
                    newResource.creationPassIndex = -1;
                    newResource.releasePassIndex = -1;

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

            for (int i = 0; i < m_CompiledPassInfos.size; ++i)
            {
                ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[i];

                RenderGraphDebugData.PassDebugData newPass = new RenderGraphDebugData.PassDebugData();
                newPass.name = passInfo.pass.name;
                newPass.culled = passInfo.culled;
                newPass.async = passInfo.enableAsyncCompute;
                newPass.generateDebugData = passInfo.pass.generateDebugData;
                newPass.resourceReadLists = new List<int>[(int)RenderGraphResourceType.Count];
                newPass.resourceWriteLists = new List<int>[(int)RenderGraphResourceType.Count];
                newPass.syncFromPassIndex = passInfo.syncFromPassIndex;
                newPass.syncToPassIndex = passInfo.syncToPassIndex;

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
