using System;
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
        public int         renderingWidth;
        ///<summary>Rendering height.</summary>
        public int         renderingHeight;
        ///<summary>Number of MSAA samples.</summary>
        public MSAASamples msaaSamples;
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

        internal struct ResourceUsageInfo
        {
            public List<int>    producers;
            public bool         resourceCreated;
            public int          lastReadPassIndex;
            public int          firstWritePassIndex;
            public int          refCount;

            public void Reset()
            {
                if (producers == null)
                    producers = new List<int>();

                producers.Clear();
                resourceCreated = false;
                lastReadPassIndex = -1;
                firstWritePassIndex = int.MaxValue;
                refCount = 0;
            }
        }

        internal struct PassExecutionInfo
        {
            public List<TextureHandle>  textureCreateList;
            public List<TextureHandle>  textureReleaseList;
            public int                  refCount;
            public bool                 pruned;
            public bool                 hasSideEffect;
            public int                  syncWithPassIndex;
            public bool                 needGraphicsFence;
            public GraphicsFence        fence;

            public void Reset()
            {
                if (textureCreateList == null)
                {
                    textureCreateList = new List<TextureHandle>();
                    textureReleaseList = new List<TextureHandle>();
                }

                textureCreateList.Clear();
                textureReleaseList.Clear();
                refCount = 0;
                pruned = false;
                hasSideEffect = false;
                syncWithPassIndex = -1;
                needGraphicsFence = false;
            }
        }

        RenderGraphResourceRegistry         m_Resources;
        RenderGraphObjectPool               m_RenderGraphPool = new RenderGraphObjectPool();
        List<RenderGraphPass>               m_RenderPasses = new List<RenderGraphPass>();
        List<RendererListHandle>            m_RendererLists = new List<RendererListHandle>();
        RenderGraphDebugParams              m_DebugParameters = new RenderGraphDebugParams();
        RenderGraphLogger                   m_Logger = new RenderGraphLogger();
        RenderGraphDefaultResources         m_DefaultResources = new RenderGraphDefaultResources();
        Dictionary<int, ProfilingSampler>   m_DefaultProfilingSamplers = new Dictionary<int, ProfilingSampler>();

        // Compiled Render Graph info.
        DynamicArray<ResourceUsageInfo>     m_TextureUsageInfo = new DynamicArray<ResourceUsageInfo>();
        DynamicArray<ResourceUsageInfo>     m_BufferUsageInfo = new DynamicArray<ResourceUsageInfo>();
        DynamicArray<PassExecutionInfo>     m_PassExecutionInfo = new DynamicArray<PassExecutionInfo>();
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
        /// Import an external texture to the Render Graph.
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
        public TextureHandle CreateTexture(TextureDesc desc, int shaderProperty = 0)
        {
            if (m_DebugParameters.tagResourceNamesWithRG)
                desc.name = string.Format("{0}_RenderGraph", desc.name);
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
            var desc = m_Resources.GetTextureResourceDesc(texture);
            if (m_DebugParameters.tagResourceNamesWithRG)
                desc.name = string.Format("{0}_RenderGraph", desc.name);
            return m_Resources.CreateTexture(desc, shaderProperty);
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
        /// </summary>
        /// <param name="computeBuffer">External Compute Buffer that needs to be imported.</param>
        /// <returns>A new ComputeBufferHandle.</returns>
        public ComputeBufferHandle ImportComputeBuffer(ComputeBuffer computeBuffer)
        {
            return m_Resources.ImportComputeBuffer(computeBuffer);
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
            try
            {
                m_Logger.Initialize();

                // Update RTHandleSystem with size for this rendering pass.
                m_Resources.SetRTHandleReferenceSize(parameters.renderingWidth, parameters.renderingHeight, parameters.msaaSamples);

                LogFrameInformation(parameters.renderingWidth, parameters.renderingHeight);

                CompileRenderGraph();
                ExecuteRenderGraph(renderContext, cmd);
            }
            catch (Exception e)
            {
                Debug.LogError("Render Graph Execution error");
                Debug.LogException(e);
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

        void ClearCompiledGraph()
        {
            ClearRenderPasses();
            m_Resources.Clear();
            m_DefaultResources.Clear();
            m_RendererLists.Clear();
            m_BufferUsageInfo.Clear();
            m_TextureUsageInfo.Clear();
            m_PassExecutionInfo.Clear();
        }

        void InitializeCompilationData()
        {
            m_BufferUsageInfo.Resize(m_Resources.GetComputeBufferResourceCount());
            for (int i = 0; i < m_BufferUsageInfo.size; ++i)
                m_BufferUsageInfo[i].Reset();
            m_TextureUsageInfo.Resize(m_Resources.GetTextureResourceCount());
            for (int i = 0; i < m_TextureUsageInfo.size; ++i)
                m_TextureUsageInfo[i].Reset();
            m_PassExecutionInfo.Resize(m_RenderPasses.Count);
            for (int i = 0; i < m_PassExecutionInfo.size; ++i)
                m_PassExecutionInfo[i].Reset();
        }

        void CountReferences()
        {
            for (int passIndex = 0; passIndex < m_RenderPasses.Count; ++passIndex)
            {
                RenderGraphPass pass = m_RenderPasses[passIndex];
                ref PassExecutionInfo passInfo = ref m_PassExecutionInfo[passIndex];

                var textureRead = pass.textureReadList;
                foreach (TextureHandle texture in textureRead)
                {
                    ref ResourceUsageInfo info = ref m_TextureUsageInfo[texture];
                    info.refCount++;
                }

                var textureWrite = pass.textureWriteList;
                foreach (TextureHandle texture in textureWrite)
                {
                    ref ResourceUsageInfo info = ref m_TextureUsageInfo[texture];
                    info.producers.Add(passIndex);
                    passInfo.refCount++;

                    // Writing to an imported texture is considered as a side effect because we don't know what users will do with it outside of render graph.
                    if (m_Resources.IsTextureImported(texture))
                        passInfo.hasSideEffect = true;
                }

                // Can't share the code with a generic func as TextureHandle and ComputeBufferHandle are both struct and can't inherit from a common struct with a shared API (thanks C#)
                var bufferRead = pass.bufferReadList;
                foreach (ComputeBufferHandle buffer in bufferRead)
                {
                    ref ResourceUsageInfo info = ref m_BufferUsageInfo[buffer];
                    info.refCount++;
                }

                var bufferWrite = pass.bufferWriteList;
                foreach (ComputeBufferHandle buffer in bufferWrite)
                {
                    ref ResourceUsageInfo info = ref m_BufferUsageInfo[buffer];
                    passInfo.refCount++;
                }
            }
        }

        void PruneUnusedPasses(DynamicArray<ResourceUsageInfo> resourceUsageList)
        {
            // First gather resources that are never read.
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
                    ref var producerInfo = ref m_PassExecutionInfo[producerIndex];
                    producerInfo.refCount--;
                    if (producerInfo.refCount == 0 && !producerInfo.hasSideEffect)
                    {
                        // Producer is not necessary anymore as it produces zero resources
                        // Prune it and decrement refCount of all the textures it reads.
                        producerInfo.pruned = true;
                        RenderGraphPass pass = m_RenderPasses[producerIndex];
                        foreach (var textureIndex in pass.textureReadList)
                        {
                            ref ResourceUsageInfo textureInfo = ref resourceUsageList[textureIndex];
                            textureInfo.refCount--;
                            // If a texture is not used anymore, add it to the stack to be processed in subsequent iteration.
                            if (textureInfo.refCount == 0)
                                m_PruningStack.Push(textureIndex);
                        }
                    }
                }
            }
        }

        void PruneUnusedPasses()
        {
            PruneUnusedPasses(m_TextureUsageInfo);
            PruneUnusedPasses(m_BufferUsageInfo);
            LogPrunedPasses();
        }

        int GetLatestProducerIndex(int passIndex, in ResourceUsageInfo info)
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

        void UpdateResourceSynchronization(ref int lastGraphicsPipeSync, ref int lastComputePipeSync, int currentPassIndex, in ResourceUsageInfo resource)
        {
            int lastProducer = GetLatestProducerIndex(currentPassIndex, resource);
            if (lastProducer != -1)
            {
                RenderGraphPass currentPass = m_RenderPasses[currentPassIndex];
                ref PassExecutionInfo currentPassInfo = ref m_PassExecutionInfo[currentPassIndex];

                RenderGraphPass producerPass = m_RenderPasses[lastProducer];
                //If the passes are on different pipes, we need synchronization.
                if (producerPass.enableAsyncCompute != currentPass.enableAsyncCompute)
                {
                    // Pass is on compute pipe, need sync with graphics pipe.
                    if (currentPass.enableAsyncCompute)
                    {
                        if (lastProducer > lastGraphicsPipeSync)
                        {
                            currentPassInfo.syncWithPassIndex = lastProducer;
                            lastGraphicsPipeSync = lastProducer;
                            m_PassExecutionInfo[lastProducer].needGraphicsFence = true;
                        }
                    }
                    else
                    {
                        if (lastProducer > lastComputePipeSync)
                        {
                            currentPassInfo.syncWithPassIndex = lastProducer;
                            lastComputePipeSync = lastProducer;
                            m_PassExecutionInfo[lastProducer].needGraphicsFence = true;
                        }
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
            for (int passIndex = 0; passIndex < m_RenderPasses.Count; ++passIndex)
            {
                ref PassExecutionInfo passInfo = ref m_PassExecutionInfo[passIndex];

                if (passInfo.pruned)
                    continue;

                RenderGraphPass pass = m_RenderPasses[passIndex];

                foreach (TextureHandle texture in pass.textureReadList)
                {
                    ref ResourceUsageInfo info = ref m_TextureUsageInfo[texture];
                    info.lastReadPassIndex = Math.Max(info.lastReadPassIndex, passIndex);
                    UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, info);
                }

                foreach (TextureHandle texture in pass.textureWriteList)
                {
                    ref ResourceUsageInfo info = ref m_TextureUsageInfo[texture];
                    if (info.firstWritePassIndex == int.MaxValue)
                    {
                        info.firstWritePassIndex = Math.Min(info.firstWritePassIndex, passIndex);
                        passInfo.textureCreateList.Add(texture);
                    }
                    passInfo.refCount++;
                    UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, info);
                }

                foreach (ComputeBufferHandle texture in pass.bufferReadList)
                {
                    UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, m_BufferUsageInfo[texture]);
                }
                foreach (ComputeBufferHandle texture in pass.bufferWriteList)
                {
                    UpdateResourceSynchronization(ref lastGraphicsPipeSync, ref lastComputePipeSync, passIndex, m_BufferUsageInfo[texture]);
                }

                // Add transient resources that are only used during this pass.
                foreach (TextureHandle texture in pass.transientTextureList)
                {
                    passInfo.textureCreateList.Add(texture);
                    passInfo.textureReleaseList.Add(texture);
                }

                // Gather all renderer lists
                m_RendererLists.AddRange(m_RenderPasses[passIndex].usedRendererListList);
            }

            // Now push textures to the release list of the pass that reads it last.
            for (int i = 0; i < m_TextureUsageInfo.size; ++i)
            {
                ResourceUsageInfo textureInfo = m_TextureUsageInfo[i];
                if (textureInfo.lastReadPassIndex != -1)
                {
                    ref PassExecutionInfo passInfo = ref m_PassExecutionInfo[textureInfo.lastReadPassIndex];
                    passInfo.textureReleaseList.Add(new TextureHandle(i));
                }
            }

            // Creates all renderer lists
            m_Resources.CreateRendererLists(m_RendererLists);
        }

        // Traverse the render graph:
        // - Determines when resources are created/released
        // - Determines async compute pass synchronization
        // - Prune unused render passes.
        void CompileRenderGraph()
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

            for (int passIndex = 0; passIndex < m_RenderPasses.Count; ++passIndex)
            {
                var passInfo = m_PassExecutionInfo[passIndex];
                if (passInfo.pruned)
                    continue;

                var pass = m_RenderPasses[passIndex];
                if (!pass.HasRenderFunc())
                {
                    throw new InvalidOperationException(string.Format("RenderPass {0} was not provided with an execute function.", pass.name));
                }

                LogRenderPassBegin(passIndex);
                using (new RenderGraphLogIndent(m_Logger))
                {
                    PreRenderPassExecute(passIndex, ref rgContext);
                    pass.Execute(rgContext);
                    PostRenderPassExecute(cmd, passIndex, ref rgContext);
                }
            }
        }

        void PreRenderPassSetRenderTargets(int passIndex, RenderGraphContext rgContext)
        {
            var pass = m_RenderPasses[passIndex];
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

        void PushKickAsyncJobMarker(in RenderGraphContext rgContext, RenderGraphPass pass)
        {
            //var sampler = GetDefaultProfilingSampler($"Kick Async Job: {pass.name}");
            //sampler.Begin(rgContext.cmd);
            //sampler.End(rgContext.cmd);
            var name = $"Kick Async Job: {pass.name}";
            rgContext.cmd.BeginSample(name);
            rgContext.cmd.EndSample(name);
        }

        void PreRenderPassExecute(int passIndex, ref RenderGraphContext rgContext)
        {
            // TODO RENDERGRAPH merge clear and setup here if possible
            RenderGraphPass pass = m_RenderPasses[passIndex];
            PassExecutionInfo passInfo = m_PassExecutionInfo[passIndex];

            // TODO RENDERGRAPH remove this when we do away with auto global texture setup
            // (can't put it in the profiling scope otherwise it might be executed on compute queue which is not possible for global sets)
            m_Resources.PreRenderPassSetGlobalTextures(rgContext, pass.textureReadList);

            //// Just mark the place where we kick the async job on graphics pipe.
            //if (pass.enableAsyncCompute)
            //    PushKickAsyncJobMarker(rgContext, pass);

            pass.customSampler?.Begin(rgContext.cmd);

            foreach (var texture in passInfo.textureCreateList)
                m_Resources.CreateAndClearTexture(rgContext, texture);

            PreRenderPassSetRenderTargets(passIndex, rgContext);

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
            if (passInfo.syncWithPassIndex != -1)
            {
                rgContext.cmd.WaitOnAsyncGraphicsFence(m_PassExecutionInfo[passInfo.syncWithPassIndex].fence);
            }
        }

        void PostRenderPassExecute(CommandBuffer mainCmd, int passIndex, ref RenderGraphContext rgContext)
        {
            RenderGraphPass pass = m_RenderPasses[passIndex];
            ref PassExecutionInfo passInfo = ref m_PassExecutionInfo[passIndex];

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
                m_Resources.PostRenderPassUnbindGlobalTextures(rgContext, m_RenderPasses[passIndex].textureReadList);

            pass.customSampler?.End(rgContext.cmd);

            m_RenderGraphPool.ReleaseAllTempAlloc();

            foreach (var texture in passInfo.textureReleaseList)
                m_Resources.ReleaseTexture(rgContext, texture);

            m_RenderPasses[passIndex].Release(rgContext);
        }

        void ClearRenderPasses()
        {
            m_RenderPasses.Clear();
        }

        void LogFrameInformation(int renderingWidth, int renderingHeight)
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("==== Staring frame at resolution ({0}x{1}) ====", renderingWidth, renderingHeight);
                m_Logger.LogLine("Number of passes declared: {0}", m_RenderPasses.Count);
            }
        }

        void LogRendererListsCreation()
        {
            if (m_DebugParameters.logFrameInformation)
            {
                m_Logger.LogLine("Number of renderer lists created: {0}", m_RendererLists.Count);
            }
        }

        void LogRenderPassBegin(int passIndex)
        {
            if (m_DebugParameters.logFrameInformation)
            {
                RenderGraphPass pass = m_RenderPasses[passIndex];
                PassExecutionInfo passInfo = m_PassExecutionInfo[passIndex];

                m_Logger.LogLine("Executing pass \"{0}\" (index: {1})", pass.name, pass.index);
                using (new RenderGraphLogIndent(m_Logger))
                {
                    m_Logger.LogLine("Queue: {0}", pass.enableAsyncCompute ? "Compute" : "Graphics");
                    if (passInfo.syncWithPassIndex != -1)
                        m_Logger.LogLine("Synchronize with pass index: {0}", passInfo.syncWithPassIndex);
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
                    for (int i = 0; i < m_PassExecutionInfo.size; ++i)
                    {
                        if (m_PassExecutionInfo[i].pruned)
                        {
                            var pass = m_RenderPasses[i];
                            m_Logger.LogLine("{0} (index: {1})", pass.name, pass.index);
                        }
                    }
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

