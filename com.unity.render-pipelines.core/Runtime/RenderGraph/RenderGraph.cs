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
    /// This struct specifies the context given to every render pass.
    /// </summary>
    public ref struct RenderGraphContext
    {
        ///<summary>Scriptable Render Context used for rendering.</summary>
        public ScriptableRenderContext      renderContext;
        ///<summary>Command Buffer used for rendering.</summary>
        public CommandBuffer                cmd;
        ///<summary>Render Graph pooll used for temporary data.</summary>
        public RenderGraphObjectPool        renderGraphPool;
        ///<summary>Render Graph Resource Registry used for accessing resources.</summary>
        public RenderGraphResourceRegistry  resources;
        ///<summary>Render Graph default resources.</summary>
        public RenderGraphDefaultResources  defaultResources;
    }

    /// <summary>
    /// This struct contains properties which control the execution of the Render Graph.
    /// </summary>
    public struct RenderGraphExecuteParams
    {
        ///<summary>Rendering width.</summary>
        public int          renderingWidth;
        ///<summary>Rendering height.</summary>
        public int          renderingHeight;
        ///<summary>Number of MSAA samples.</summary>
        public MSAASamples  msaaSamples;
        ///<summary>Index of the current frame being rendered.</summary>
        public int          currentFrameIndex;
    }

    class RenderGraphDebugParams
    {
        public bool tagResourceNamesWithRG;
        public bool clearRenderTargetsAtCreation;
        public bool clearRenderTargetsAtRelease;
        public bool unbindGlobalTextures;
        public bool logFrameInformation;
        public bool logResources;

        public void RegisterDebug()
        {
            var list = new List<DebugUI.Widget>();
            list.Add(new DebugUI.BoolField { displayName = "Tag Resources with RG", getter = () => tagResourceNamesWithRG, setter = value => tagResourceNamesWithRG = value });
            list.Add(new DebugUI.BoolField { displayName = "Clear Render Targets at creation", getter = () => clearRenderTargetsAtCreation, setter = value => clearRenderTargetsAtCreation = value });
            list.Add(new DebugUI.BoolField { displayName = "Clear Render Targets at release", getter = () => clearRenderTargetsAtRelease, setter = value => clearRenderTargetsAtRelease = value });
            list.Add(new DebugUI.BoolField { displayName = "Unbind Global Textures", getter = () => unbindGlobalTextures, setter = value => unbindGlobalTextures = value });
            list.Add(new DebugUI.Button { displayName = "Log Frame Information", action = () => logFrameInformation = true });
            list.Add(new DebugUI.Button { displayName = "Log Resources", action = () => logResources = true });

            var panel = DebugManager.instance.GetPanel("Render Graph", true);
            panel.children.Add(list.ToArray());
        }

        public void UnRegisterDebug()
        {
            DebugManager.instance.RemovePanel("Render Graph");
        }
    }

    /// <summary>
    /// The Render Pass rendering delegate.
    /// </summary>
    /// <typeparam name="PassData">The type of the class used to provide data to the Render Pass.</typeparam>
    /// <param name="data">Render Pass specific data.</param>
    /// <param name="renderGraphContext">Global Render Graph context.</param>
    public delegate void RenderFunc<PassData>(PassData data, RenderGraphContext renderGraphContext) where PassData : class, new();

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
            public RenderGraphPass      pass;
            public List<int>            textureCreateList;
            public List<int>            textureReleaseList;
            public int                  refCount;
            public bool                 pruned;
            public bool                 hasSideEffect;
            public int                  syncToPassIndex; // Index of the pass that needs to be waited for.
            public int                  syncFromPassIndex; // Smaller pass index that waits for this pass.
            public bool                 needGraphicsFence;
            public GraphicsFence        fence;

            public bool                 enableAsyncCompute { get { return pass.enableAsyncCompute; } }
            public bool                 allowPassPruning { get { return pass.allowPassPruning; } }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // This members are only here to ease debugging.
            public List<string>         debugTextureReads;
            public List<string>         debugTextureWrites;
#endif

            public void Reset(RenderGraphPass pass)
            {
                this.pass = pass;

                if (textureCreateList == null)
                {
                    textureCreateList = new List<int>();
                    textureReleaseList = new List<int>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    debugTextureReads = new List<string>();
                    debugTextureWrites = new List<string>();
#endif
                }

                textureCreateList.Clear();
                textureReleaseList.Clear();
                refCount = 0;
                pruned = false;
                hasSideEffect = false;
                syncToPassIndex = -1;
                syncFromPassIndex = -1;
                needGraphicsFence = false;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                debugTextureReads.Clear();
                debugTextureWrites.Clear();
#endif
            }
        }

        RenderGraphResourceRegistry         m_Resources;
        RenderGraphObjectPool               m_RenderGraphPool = new RenderGraphObjectPool();
        List<RenderGraphPass>               m_RenderPasses = new List<RenderGraphPass>(64);
        List<RendererListHandle>            m_RendererLists = new List<RendererListHandle>(32);
        RenderGraphDebugParams              m_DebugParameters = new RenderGraphDebugParams();
        RenderGraphLogger                   m_Logger = new RenderGraphLogger();
        RenderGraphDefaultResources         m_DefaultResources = new RenderGraphDefaultResources();
        Dictionary<int, ProfilingSampler>   m_DefaultProfilingSamplers = new Dictionary<int, ProfilingSampler>();
        bool                                m_ExecutionExceptionWasRaised;

        // Compiled Render Graph info.
        DynamicArray<CompiledResourceInfo>[] m_CompiledResourcesInfos = new DynamicArray<CompiledResourceInfo>[(int)RenderGraphResourceType.Count];

        //DynamicArray<CompiledResourceInfo>  m_CompiledTextureInfos = new DynamicArray<CompiledResourceInfo>();
        //DynamicArray<CompiledResourceInfo>  m_CompiledBufferInfos = new DynamicArray<CompiledResourceInfo>();
        DynamicArray<CompiledPassInfo>      m_CompiledPassInfos = new DynamicArray<CompiledPassInfo>();
        Stack<int>                          m_PruningStack = new Stack<int>();

        #region Public Interface

        // TODO RENDERGRAPH: Currently only needed by SSAO to sample correctly depth texture mips. Need to figure out a way to hide this behind a proper formalization.
        /// <summary>
        /// Gets the RTHandleProperties structure associated with the Render Graph's RTHandle System.
        /// </summary>
        public RTHandleProperties rtHandleProperties { get { return m_Resources.GetRTHandleProperties(); } }

        public RenderGraphDefaultResources defaultResources
        {
            get
            {
                m_DefaultResources.InitializeForRendering(this);
                return m_DefaultResources;
            }
        }

        /// <summary>
        /// Render Graph constructor.
        /// </summary>
        /// <param name="supportMSAA">Specify if this Render Graph should support MSAA.</param>
        /// <param name="initialSampleCount">Specify the initial sample count of MSAA render textures.</param>
        public RenderGraph(bool supportMSAA, MSAASamples initialSampleCount)
        {
            m_Resources = new RenderGraphResourceRegistry(supportMSAA, initialSampleCount, m_DebugParameters, m_Logger);

            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
            {
                m_CompiledResourcesInfos[i] = new DynamicArray<CompiledResourceInfo>();
            }
        }

        /// <summary>
        /// Cleanup the Render Graph.
        /// </summary>
        public void Cleanup()
        {
            m_Resources.Cleanup();
            m_DefaultResources.Cleanup();
        }

        /// <summary>
        /// Register this Render Graph to the debug window.
        /// </summary>
        public void RegisterDebug()
        {
            m_DebugParameters.RegisterDebug();
        }

        /// <summary>
        /// Unregister this Render Graph from the debug window.
        /// </summary>
        public void UnRegisterDebug()
        {
            m_DebugParameters.UnRegisterDebug();
        }

        /// <summary>
        /// Resets the reference size of the internal RTHandle System.
        /// This allows users to reduce the memory footprint of render textures after doing a super sampled rendering pass for example.
        /// </summary>
        /// <param name="width">New width of the internal RTHandle System.</param>
        /// <param name="height">New height of the internal RTHandle System.</param>
        public void ResetRTHandleReferenceSize(int width, int height)
        {
            m_Resources.ResetRTHandleReferenceSize(width, height);
        }

        /// <summary>
        /// Import an external texture to the Render Graph.
        /// Any pass writing to an imported texture will be considered having side effects and can't be automatically pruned.
        /// </summary>
        /// <param name="rt">External RTHandle that needs to be imported.</param>
        /// <param name="shaderProperty">Optional property that allows you to specify a Shader property name to use for automatic resource binding.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle ImportTexture(RTHandle rt, int shaderProperty = 0)
        {
            return m_Resources.ImportTexture(rt, shaderProperty);
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
        /// <param name="shaderProperty">Optional property that allows you to specify a Shader property name to use for automatic resource binding.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle CreateTexture(in TextureDesc desc, int shaderProperty = 0)
        {
            return m_Resources.CreateTexture(desc, shaderProperty);
        }

        /// <summary>
        /// Create a new Render Graph Texture resource using the descriptor from another texture.
        /// </summary>
        /// <param name="texture">Texture from which the descriptor should be used.</param>
        /// <param name="shaderProperty">Optional property that allows you to specify a Shader property name to use for automatic resource binding.</param>
        /// <returns>A new TextureHandle.</returns>
        public TextureHandle CreateTexture(TextureHandle texture, int shaderProperty = 0)
        {
            return m_Resources.CreateTexture(m_Resources.GetTextureResourceDesc(texture), shaderProperty);
        }

        /// <summary>
        /// Gets the descriptor of the specified Texture resource.
        /// </summary>
        /// <param name="texture"></param>
        /// <returns>The input texture descriptor.</returns>
        public TextureDesc GetTextureDesc(TextureHandle texture)
        {
            return m_Resources.GetTextureResourceDesc(texture);
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
        /// Any pass writing to an imported compute buffer will be considered having side effects and can't be automatically pruned.
        /// </summary>
        /// <param name="computeBuffer">External Compute Buffer that needs to be imported.</param>
        /// <returns>A new ComputeBufferHandle.</returns>
        public ComputeBufferHandle ImportComputeBuffer(ComputeBuffer computeBuffer)
        {
            return m_Resources.ImportComputeBuffer(computeBuffer);
        }

        public ComputeBufferHandle CreateComputeBuffer(in ComputeBufferDesc desc)
        {
            return m_Resources.CreateComputeBuffer(desc);
        }

        public ComputeBufferHandle CreateComputeBuffer(ComputeBufferHandle computeBuffer)
        {
            return m_Resources.CreateComputeBuffer(m_Resources.GetComputeBufferResourceDesc(computeBuffer));
        }

        public ComputeBufferDesc GetComputeBufferDesc(ComputeBufferHandle computeBuffer)
        {
            return m_Resources.GetComputeBufferResourceDesc(computeBuffer);
        }

        /// <summary>
        /// Add a new Render Pass to the current Render Graph.
        /// </summary>
        /// <typeparam name="PassData">Type of the class to use to provide data to the Render Pass.</typeparam>
        /// <param name="passName">Name of the new Render Pass (this is also be used to generate a GPU profiling marker).</param>
        /// <param name="passData">Instance of PassData that is passed to the render function and you must fill.</param>
        /// <param name="sampler">Optional profiling sampler.</param>
        /// <returns>A new instance of a RenderGraphBuilder used to setup the new Render Pass.</returns>
        public RenderGraphBuilder AddRenderPass<PassData>(string passName, out PassData passData, ProfilingSampler sampler = null) where PassData : class, new()
        {
            var renderPass = m_RenderGraphPool.Get<RenderGraphPass<PassData>>();
            renderPass.Initialize(m_RenderPasses.Count, m_RenderGraphPool.Get<PassData>(), passName, sampler != null ? sampler : GetDefaultProfilingSampler(passName));

            passData = renderPass.data;

            m_RenderPasses.Add(renderPass);

            return new RenderGraphBuilder(renderPass, m_Resources);
        }

        /// <summary>
        /// Execute the Render Graph in its current state.
        /// </summary>
        /// <param name="renderContext">ScriptableRenderContext used to execute Scriptable Render Pipeline.</param>
        /// <param name="cmd">Command Buffer used for Render Passes rendering.</param>
        /// <param name="parameters">Render Graph execution parameters.</param>
        public void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, in RenderGraphExecuteParams parameters)
        {
            m_ExecutionExceptionWasRaised = false;

            try
            {
                m_Logger.Initialize();

                m_Resources.BeginRender(parameters.renderingWidth, parameters.renderingHeight, parameters.msaaSamples, parameters.currentFrameIndex);

                LogFrameInformation(parameters.renderingWidth, parameters.renderingHeight);

                CompileRenderGraph();
                ExecuteRenderGraph(renderContext, cmd);
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
                ClearCompiledGraph();

                if (m_DebugParameters.logFrameInformation || m_DebugParameters.logResources)
                    Debug.Log(m_Logger.GetLog());

                m_DebugParameters.logFrameInformation = false;
                m_DebugParameters.logResources = false;
            }
        }
        #endregion

        #region Internal Interface
        private RenderGraph()
        {

        }

        internal void ClearCompiledGraph()
        {
            ClearRenderPasses();
            m_Resources.Clear(m_ExecutionExceptionWasRaised);
            m_DefaultResources.Clear();
            m_RendererLists.Clear();
            for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                m_CompiledResourcesInfos[i].Clear();
            m_CompiledPassInfos.Clear();
        }

        void InitializeCompilationData()
        {
            m_CompiledResourcesInfos[(int)RenderGraphResourceType.Texture]
            m_CompiledBufferInfos.Resize(m_Resources.GetComputeBufferResourceCount());
            for (int i = 0; i < m_CompiledBufferInfos.size; ++i)
                m_CompiledBufferInfos[i].Reset();
            m_CompiledTextureInfos.Resize(m_Resources.GetTextureResourceCount());
            for (int i = 0; i < m_CompiledTextureInfos.size; ++i)
                m_CompiledTextureInfos[i].Reset();
            m_CompiledPassInfos.Resize(m_RenderPasses.Count);
            for (int i = 0; i < m_CompiledPassInfos.size; ++i)
                m_CompiledPassInfos[i].Reset(m_RenderPasses[i]);
        }

        internal DynamicArray<CompiledPassInfo> GetCompiledPassInfos() { return m_CompiledPassInfos; }

        void CountReferences()
        {
            for (int passIndex = 0; passIndex < m_CompiledPassInfos.size; ++passIndex)
            {
                ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[passIndex];

                var textureRead = passInfo.pass.textureReadList;
                foreach (TextureHandle texture in textureRead)
                {
                    ref CompiledResourceInfo info = ref m_CompiledTextureInfos[texture];
                    info.consumers.Add(passIndex);
                    info.refCount++;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    passInfo.debugTextureReads.Add(m_Resources.GetTextureResourceDesc(texture).name);
#endif
                }

                var textureWrite = passInfo.pass.textureWriteList;
                foreach (TextureHandle texture in textureWrite)
                {
                    ref CompiledResourceInfo info = ref m_CompiledTextureInfos[texture];
                    info.producers.Add(passIndex);
                    passInfo.refCount++;

                    // Writing to an imported texture is considered as a side effect because we don't know what users will do with it outside of render graph.
                    if (m_Resources.IsTextureImported(texture))
                        passInfo.hasSideEffect = true;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    passInfo.debugTextureWrites.Add(m_Resources.GetTextureResourceDesc(texture).name);
#endif
                }

                foreach (TextureHandle texture in passInfo.pass.transientTextureList)
                {
                    ref CompiledResourceInfo info = ref m_CompiledTextureInfos[texture];
                    info.refCount++;
                    info.consumers.Add(passIndex);
                    info.producers.Add(passIndex);
                }

                // Can't share the code with a generic func as TextureHandle and ComputeBufferHandle are both struct and can't inherit from a common struct with a shared API (thanks C#)
                var bufferRead = passInfo.pass.bufferReadList;
                foreach (ComputeBufferHandle buffer in bufferRead)
                {
                    ref CompiledResourceInfo info = ref m_CompiledBufferInfos[buffer];
                    info.consumers.Add(passIndex);
                    info.refCount++;
                }

                var bufferWrite = passInfo.pass.bufferWriteList;
                foreach (ComputeBufferHandle buffer in bufferWrite)
                {
                    ref CompiledResourceInfo info = ref m_CompiledBufferInfos[buffer];
                    info.producers.Add(passIndex);
                    passInfo.refCount++;

                    // Writing to an imported compute buffer is considered as a side effect because we don't know what users will do with it outside of render graph.
                    if (m_Resources.IsComputeBufferImported(buffer))
                        passInfo.hasSideEffect = true;
                }
            }
        }

        void PruneOutputlessPasses()
        {
            // Gather passes that don't produce anything and prune them.
            m_PruningStack.Clear();
            for (int i = 0; i < m_CompiledPassInfos.size; ++i)
            {
                ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[i];

                if (passInfo.refCount == 0 && !passInfo.hasSideEffect && passInfo.allowPassPruning)
                {
                    // Producer is not necessary as it produces zero resources
                    // Prune it and decrement refCount of all the resources it reads.
                    // We don't need to go recursively here because we decrement ref count of read resources
                    // so the subsequent passes of pruning will detect those and remove the related passes.
                    passInfo.pruned = true;
                    foreach (var index in passInfo.pass.textureReadList)
                    {
                        m_CompiledTextureInfos[index].refCount--;
                    }

                    foreach (var index in passInfo.pass.bufferReadList)
                    {
                        m_CompiledBufferInfos[index].refCount--;
                    }
                }
            }
        }

        void PruneUnusedPasses(bool textureResources)
        {
            DynamicArray<CompiledResourceInfo> resourceUsageList = textureResources ? m_CompiledTextureInfos : m_CompiledBufferInfos;

            // Gather resources that are never read.
            m_PruningStack.Clear();
            for (int i = 0; i < resourceUsageList.size; ++i)
            {
                if (resourceUsageList[i].refCount == 0)
                {
                    m_PruningStack.Push(i);
                }
            }

            while (m_PruningStack.Count != 0)
            {
                var unusedResource = resourceUsageList[m_PruningStack.Pop()];
                foreach (var producerIndex in unusedResource.producers)
                {
                    ref var producerInfo = ref m_CompiledPassInfos[producerIndex];
                    producerInfo.refCount--;
                    if (producerInfo.refCount == 0 && !producerInfo.hasSideEffect && producerInfo.allowPassPruning)
                    {
                        // Producer is not necessary anymore as it produces zero resources
                        // Prune it and decrement refCount of all the textures it reads.
                        producerInfo.pruned = true;

                        // Once again, can't share code because C# (can't have struct inheritance)
                        // Making all those List <int> could help but we lose a lot of explicitness in the API...
                        if (textureResources)
                        {
                            foreach (var textureIndex in producerInfo.pass.textureReadList)
                            {
                                ref CompiledResourceInfo resourceInfo = ref resourceUsageList[textureIndex];
                                resourceInfo.refCount--;
                                // If a resource is not used anymore, add it to the stack to be processed in subsequent iteration.
                                if (resourceInfo.refCount == 0)
                                    m_PruningStack.Push(textureIndex);
                            }
                        }
                        else
                        {
                            foreach (var bufferIndex in producerInfo.pass.bufferReadList)
                            {
                                ref CompiledResourceInfo resourceInfo = ref resourceUsageList[bufferIndex];
                                resourceInfo.refCount--;
                                // If a resource is not used anymore, add it to the stack to be processed in subsequent iteration.
                                if (resourceInfo.refCount == 0)
                                    m_PruningStack.Push(bufferIndex);
                            }
                        }
                    }
                }
            }
        }

        void PruneUnusedPasses()
        {
            // TODO RENDERGRAPH: temporarily remove pruning of passes without product.
            // Many passes are used just to set global variables so we don't want to force users to disallow pruning on those explicitly every time.
            // This will prune passes with no outputs.
            //PruneOutputlessPasses();

            // This will prune all passes that produce resource that are never read.
            PruneUnusedPasses(textureResources: true);
            PruneUnusedPasses(textureResources: false);

            LogPrunedPasses();
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
                if (!m_CompiledPassInfos[consumers[i]].pruned)
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
                if (!m_CompiledPassInfos[producers[i]].pruned)
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
                if (!m_CompiledPassInfos[producers[i]].pruned)
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

                if (passInfo.pruned)
                    continue;

                foreach (TextureHandle texture in passInfo.pass.textureReadList)
                {
                    UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, m_CompiledTextureInfos[texture]);
                }

                foreach (TextureHandle texture in passInfo.pass.textureWriteList)
                {
                    UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, m_CompiledTextureInfos[texture]);
                }

                foreach (ComputeBufferHandle texture in passInfo.pass.bufferReadList)
                {
                    UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, m_CompiledBufferInfos[texture]);
                }
                foreach (ComputeBufferHandle texture in passInfo.pass.bufferWriteList)
                {
                    UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, m_CompiledBufferInfos[texture]);
                }

                // Gather all renderer lists
                m_RendererLists.AddRange(passInfo.pass.usedRendererListList);
            }

            // Now push textures to the release list of the pass that reads it last.
            for (int i = 0; i < m_CompiledTextureInfos.size; ++i)
            {
                CompiledResourceInfo textureInfo = m_CompiledTextureInfos[i];

                // Texture creation
                int firstWriteIndex = GetFirstValidWriteIndex(textureInfo);
                // This can happen for imported textures (for example an imported dummy black texture will never be written to but does not need creation anyway)
                if (firstWriteIndex != -1)
                    m_CompiledPassInfos[firstWriteIndex].textureCreateList.Add(i);

                // Texture release
                // Sometimes, a texture can be written by a pass after the last pass that reads it.
                // In this case, we need to extend its lifetime to this pass otherwise the pass would get an invalid texture.
                int lastReadPassIndex = Math.Max(GetLatestValidReadIndex(textureInfo), GetLatestValidWriteIndex(textureInfo));

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
                            if(m_CompiledPassInfos[currentPassIndex].enableAsyncCompute)
                                firstWaitingPassIndex = m_CompiledPassInfos[currentPassIndex].syncFromPassIndex;
                        }

                        // Finally add the release command to the pass before the first pass that waits for the compute pipe.
                        ref CompiledPassInfo passInfo = ref m_CompiledPassInfos[Math.Max(0, firstWaitingPassIndex - 1)];
                        passInfo.textureReleaseList.Add(i);

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
                        passInfo.textureReleaseList.Add(i);
                    }
                }
            }

            // Creates all renderer lists
            m_Resources.CreateRendererLists(m_RendererLists);
        }

        // Traverse the render graph:
        // - Determines when resources are created/released
        // - Determines async compute pass synchronization
        // - Prune unused render passes.
        internal void CompileRenderGraph()
        {
            InitializeCompilationData();
            CountReferences();
            PruneUnusedPasses();
            UpdateResourceAllocationAndSynchronization();
            LogRendererListsCreation();
        }

        // Execute the compiled render graph
        void ExecuteRenderGraph(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            RenderGraphContext rgContext = new RenderGraphContext();
            rgContext.cmd = cmd;
            rgContext.renderContext = renderContext;
            rgContext.renderGraphPool = m_RenderGraphPool;
            rgContext.resources = m_Resources;
            rgContext.defaultResources = m_DefaultResources;

            for (int passIndex = 0; passIndex < m_CompiledPassInfos.size; ++passIndex)
            {
                ref var passInfo = ref m_CompiledPassInfos[passIndex];
                if (passInfo.pruned)
                    continue;

                if (!passInfo.pass.HasRenderFunc())
                {
                    throw new InvalidOperationException(string.Format("RenderPass {0} was not provided with an execute function.", passInfo.pass.name));
                }

                try
                {
                    using (new ProfilingScope(rgContext.cmd, passInfo.pass.customSampler))
                    {
                        LogRenderPassBegin(passInfo);
                        using (new RenderGraphLogIndent(m_Logger))
                        {
                            PreRenderPassExecute(passInfo, ref rgContext);
                            passInfo.pass.Execute(rgContext);
                            PostRenderPassExecute(cmd, ref passInfo, ref rgContext);
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

        void PreRenderPassExecute(in CompiledPassInfo passInfo, ref RenderGraphContext rgContext)
        {
            // TODO RENDERGRAPH merge clear and setup here if possible
            RenderGraphPass pass = passInfo.pass;

            // TODO RENDERGRAPH remove this when we do away with auto global texture setup
            // (can't put it in the profiling scope otherwise it might be executed on compute queue which is not possible for global sets)
            m_Resources.PreRenderPassSetGlobalTextures(rgContext, pass.textureReadList);

            foreach (var texture in passInfo.textureCreateList)
                m_Resources.CreateAndClearTexture(rgContext, texture);

            PreRenderPassSetRenderTargets(passInfo, rgContext);

            // Flush first the current command buffer on the render context.
            rgContext.renderContext.ExecuteCommandBuffer(rgContext.cmd);
            rgContext.cmd.Clear();

            if (pass.enableAsyncCompute)
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

        void PostRenderPassExecute(CommandBuffer mainCmd, ref CompiledPassInfo passInfo, ref RenderGraphContext rgContext)
        {
            RenderGraphPass pass = passInfo.pass;

            if (passInfo.needGraphicsFence)
                passInfo.fence = rgContext.cmd.CreateAsyncGraphicsFence();

            if (pass.enableAsyncCompute)
            {
                // The command buffer has been filled. We can kick the async task.
                rgContext.renderContext.ExecuteCommandBufferAsync(rgContext.cmd, ComputeQueueType.Background);
                CommandBufferPool.Release(rgContext.cmd);
                rgContext.cmd = mainCmd; // Restore the main command buffer.
            }

            if (m_DebugParameters.unbindGlobalTextures)
                m_Resources.PostRenderPassUnbindGlobalTextures(rgContext, pass.textureReadList);

            m_RenderGraphPool.ReleaseAllTempAlloc();

            foreach (var texture in passInfo.textureReleaseList)
                m_Resources.ReleaseTexture(rgContext, texture);
        }

        void ClearRenderPasses()
        {
            foreach (var pass in m_RenderPasses)
                pass.Release(m_RenderGraphPool);
            m_RenderPasses.Clear();
        }

        void LogFrameInformation(int renderingWidth, int renderingHeight)
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("==== Staring frame at resolution ({0}x{1}) ====", renderingWidth, renderingHeight);
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

        void LogPrunedPasses()
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("Pass pruning report:");
                using (new RenderGraphLogIndent(m_Logger))
                {
                    for (int i = 0; i < m_CompiledPassInfos.size; ++i)
                    {
                        if (m_CompiledPassInfos[i].pruned)
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

        #endregion
    }
}

